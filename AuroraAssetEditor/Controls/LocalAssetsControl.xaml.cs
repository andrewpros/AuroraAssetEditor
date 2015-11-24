// 
// 	FtpAssetsControl.xaml.cs
// 	AuroraAssetEditor
// 
// 	Created by Swizzy on 13/05/2015
// 	Copyright (c) 2015 Swizzy. All rights reserved.

namespace AuroraAssetEditor.Controls {
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using AuroraAssetEditor.Classes;
    using Microsoft.Win32;

    /// <summary>
    ///     Interaction logic for FtpAssetsControl.xaml
    /// </summary>
    public partial class LocalAssetsControl {
        private readonly ThreadSafeObservableCollection<AuroraDbManager.ContentItem> _assetsList = new ThreadSafeObservableCollection<AuroraDbManager.ContentItem>();
        private readonly BackgroundControl _background;
        private readonly BoxartControl _boxart;
        private readonly IconBannerControl _iconBanner;
        private readonly MainWindow _main;
        private readonly ScreenshotsControl _screenshots;
        private byte[] _buffer;
        private bool _isBusy, _isError;
        private string ContentDbPath;

        public LocalAssetsControl(MainWindow main, BoxartControl boxart, BackgroundControl background, IconBannerControl iconBanner, ScreenshotsControl screenshots) {
            InitializeComponent();
            _main = main;
            _boxart = boxart;
            _background = background;
            _iconBanner = iconBanner;
            _screenshots = screenshots;

            FtpAssetsBox.ItemsSource = _assetsList;

            }


        private void GetAssetsClick() {
            _assetsList.Clear();

            if(ContentDbPath == null) {
                return;
                }

            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                try {
                    var path = Path.Combine(Path.GetTempPath(), "AuroraAssetEditor.db");

                    if(File.Exists(ContentDbPath)) {
                        File.Copy(ContentDbPath, path);

                        }
                    else {
                        return;
                        }


                    foreach(var title in AuroraDbManager.GetDbTitles(path))
                        _assetsList.Add(title);
                    args.Result = true;
                    }
                catch(Exception ex) {
                    MainWindow.SaveError(ex);
                    args.Result = false;
                    }
            };
            bw.RunWorkerCompleted += (o, args) => {
                _main.BusyIndicator.Visibility = Visibility.Collapsed;
                if((bool)args.Result)
                    Status.Text = "Finished grabbing Assets information successfully...";
                else
                    Status.Text = "There was an error, check error.log for more information...";
            };
            _main.BusyIndicator.Visibility = Visibility.Visible;
            Status.Text = "Grabbing Assets information...";
            bw.RunWorkerAsync();
            }

        private void ProcessAsset(Task task, bool shouldHideWhenDone = true) {
            _isError = false;
            AuroraDbManager.ContentItem asset = null;
            Dispatcher.InvokeIfRequired(() => asset = FtpAssetsBox.SelectedItem as AuroraDbManager.ContentItem, DispatcherPriority.Normal);
            if(asset == null)
                return;


            ContentItemLocal assetWrapper = new ContentItemLocal(asset);

            var bw = new BackgroundWorker();
            bw.DoWork += (sender, args) => {
                try {
                    switch(task) {
                        case Task.GetBoxart:
                        _buffer = assetWrapper.GetBoxart();
                        break;
                        case Task.GetBackground:
                        _buffer = assetWrapper.GetBackground();
                        break;
                        case Task.GetIconBanner:
                        _buffer = assetWrapper.GetIconBanner();
                        break;
                        case Task.GetScreenshots:
                        _buffer = assetWrapper.GetScreenshots();
                        break;
                        case Task.SetBoxart:
                        assetWrapper.SaveAsBoxart(_buffer);
                        break;
                        case Task.SetBackground:
                        assetWrapper.SaveAsBackground(_buffer);
                        break;
                        case Task.SetIconBanner:
                        assetWrapper.SaveAsIconBanner(_buffer);
                        break;
                        case Task.SetScreenshots:
                        assetWrapper.SaveAsScreenshots(_buffer);
                        break;
                        }
                    args.Result = true;
                    }
                catch(Exception ex) {
                    MainWindow.SaveError(ex);
                    args.Result = false;
                    }
            };
            bw.RunWorkerCompleted += (sender, args) => {
                if(shouldHideWhenDone)
                    Dispatcher.InvokeIfRequired(() => _main.BusyIndicator.Visibility = Visibility.Collapsed, DispatcherPriority.Normal);
                var isGet = true;
                if((bool)args.Result) {
                    if(_buffer.Length > 0) {
                        var aurora = new AuroraAsset.AssetFile(_buffer);
                        switch(task) {
                            case Task.GetBoxart:
                            _boxart.Load(aurora);
                            Dispatcher.InvokeIfRequired(() => _main.BoxartTab.IsSelected = true, DispatcherPriority.Normal);
                            break;
                            case Task.GetBackground:
                            _background.Load(aurora);
                            Dispatcher.InvokeIfRequired(() => _main.BackgroundTab.IsSelected = true, DispatcherPriority.Normal);
                            break;
                            case Task.GetIconBanner:
                            _iconBanner.Load(aurora);
                            Dispatcher.InvokeIfRequired(() => _main.IconBannerTab.IsSelected = true, DispatcherPriority.Normal);
                            break;
                            case Task.GetScreenshots:
                            _screenshots.Load(aurora);
                            Dispatcher.InvokeIfRequired(() => _main.ScreenshotsTab.IsSelected = true, DispatcherPriority.Normal);
                            break;
                            default:
                            isGet = false;
                            break;
                            }
                        }
                    if(shouldHideWhenDone && isGet)
                        Dispatcher.InvokeIfRequired(() => Status.Text = "Finished grabbing assets from FTP", DispatcherPriority.Normal);
                    else if(shouldHideWhenDone)
                        Dispatcher.InvokeIfRequired(() => Status.Text = "Finished saving assets to FTP", DispatcherPriority.Normal);
                    }
                else {
                    switch(task) {
                        case Task.GetBoxart:
                        case Task.GetBackground:
                        case Task.GetIconBanner:
                        case Task.GetScreenshots:
                        break;
                        default:
                        isGet = false;
                        break;
                        }
                    if(isGet)
                        Dispatcher.InvokeIfRequired(() => Status.Text = "Failed getting asset data... See error.log for more information...", DispatcherPriority.Normal);
                    else
                        Dispatcher.InvokeIfRequired(() => Status.Text = "Failed saving asset data... See error.log for more information...", DispatcherPriority.Normal);
                    _isError = true;
                    }
                _isBusy = false;
            };
            Dispatcher.InvokeIfRequired(() => _main.BusyIndicator.Visibility = Visibility.Visible, DispatcherPriority.Normal);
            _isBusy = true;
            bw.RunWorkerAsync();
            }

        private void GetBoxartClick(object sender, RoutedEventArgs e) { ProcessAsset(Task.GetBoxart); }

        private void GetBackgroundClick(object sender, RoutedEventArgs e) { ProcessAsset(Task.GetBackground); }

        private void GetIconBannerClick(object sender, RoutedEventArgs e) { ProcessAsset(Task.GetIconBanner); }

        private void GetScreenshotsClick(object sender, RoutedEventArgs e) { ProcessAsset(Task.GetScreenshots); }

        private void GetFtpAssetsClick(object sender, RoutedEventArgs e) {
            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                ProcessAsset(Task.GetBoxart, false);
                while(_isBusy)
                    Thread.Sleep(100);
                if(_isError)
                    return;
                ProcessAsset(Task.GetBackground, false);
                while(_isBusy)
                    Thread.Sleep(100);
                if(_isError)
                    return;
                ProcessAsset(Task.GetIconBanner, false);
                while(_isBusy)
                    Thread.Sleep(100);
                if(_isError)
                    return;
                ProcessAsset(Task.GetScreenshots);
            };
            bw.RunWorkerCompleted += (o, args) => _main.BusyIndicator.Visibility = Visibility.Collapsed;
            bw.RunWorkerAsync();
            }

        private void SaveFtpAssetsClick(object sender, RoutedEventArgs e) {
            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                Dispatcher.InvokeIfRequired(() => _buffer = _boxart.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetBoxart, false);
                while(_isBusy)
                    Thread.Sleep(100);
                if(_isError)
                    return;
                Dispatcher.InvokeIfRequired(() => _buffer = _background.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetBackground, false);
                while(_isBusy)
                    Thread.Sleep(100);
                if(_isError)
                    return;
                Dispatcher.InvokeIfRequired(() => _buffer = _iconBanner.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetIconBanner, false);
                while(_isBusy)
                    Thread.Sleep(100);
                if(_isError)
                    return;
                Dispatcher.InvokeIfRequired(() => _buffer = _screenshots.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetScreenshots);
            };
            bw.RunWorkerCompleted += (o, args) => _main.BusyIndicator.Visibility = Visibility.Collapsed;
            _main.BusyIndicator.Visibility = Visibility.Visible;
            bw.RunWorkerAsync();
            }

        private void SaveBoxartClick(object sender, RoutedEventArgs e) {
            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                Dispatcher.InvokeIfRequired(() => _buffer = _boxart.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetBoxart);
            };
            _main.BusyIndicator.Visibility = Visibility.Visible;
            bw.RunWorkerAsync();
            }

        private void SaveBackgroundClick(object sender, RoutedEventArgs e) {
            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                Dispatcher.InvokeIfRequired(() => _buffer = _background.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetBackground);
            };
            _main.BusyIndicator.Visibility = Visibility.Visible;
            bw.RunWorkerAsync();
            }

        private void SaveIconBannerClick(object sender, RoutedEventArgs e) {
            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                Dispatcher.InvokeIfRequired(() => _buffer = _iconBanner.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetIconBanner);
            };
            _main.BusyIndicator.Visibility = Visibility.Visible;
            bw.RunWorkerAsync();
            }

        private void SaveScreenshotsClick(object sender, RoutedEventArgs e) {
            var bw = new BackgroundWorker();
            bw.DoWork += (o, args) => {
                Dispatcher.InvokeIfRequired(() => _buffer = _screenshots.GetData(), DispatcherPriority.Normal);
                ProcessAsset(Task.SetScreenshots);
            };
            _main.BusyIndicator.Visibility = Visibility.Visible;
            bw.RunWorkerAsync();
            }

        private void OnDragEnter(object sender, DragEventArgs e) { _main.OnDragEnter(sender, e); }

        private void OnDrop(object sender, DragEventArgs e) { _main.DragDrop(this, e); }

        private void FtpAssetsBoxContextOpening(object sender, ContextMenuEventArgs e) {
            if(FtpAssetsBox.SelectedItem == null)
                e.Handled = true;
            }

        private void RemoveFtpAssetsClick(object sender, RoutedEventArgs e) {
            _boxart.Reset();
            _iconBanner.Reset();
            _background.Reset();
            _screenshots.Reset();
            SaveFtpAssetsClick(sender, e);
            }

        private void RemoveBoxartClick(object sender, RoutedEventArgs e) {
            _boxart.Reset();
            SaveBoxartClick(sender, e);
            }

        private void RemoveBackgroundClick(object sender, RoutedEventArgs e) {
            _background.Reset();
            SaveBackgroundClick(sender, e);
            }

        private void RemoveIconBannerClick(object sender, RoutedEventArgs e) {
            _iconBanner.Reset();
            SaveIconBannerClick(sender, e);
            }

        private void RemoveScreenshotsClick(object sender, RoutedEventArgs e) {
            _screenshots.Reset();
            SaveScreenshotsClick(sender, e);
            }

        private void Button_Click(object sender, RoutedEventArgs e) {

            }

        private void SetLocalDB_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.ReadOnlyChecked = true;

            openFileDialog.Filter = "Aurora Content.db file (Content.db)|Content.db";

            var lastDir = Properties.Settings.Default.LastDir;

            if(!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir)) {
                openFileDialog.InitialDirectory = lastDir;
                }
            else {
                openFileDialog.InitialDirectory ="::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                }



            if(openFileDialog.ShowDialog().Value) {

                var contentdbFile = new FileInfo(openFileDialog.FileName);

                ContentDbPath = contentdbFile.FullName;

                Properties.Settings.Default.LastDir = contentdbFile.DirectoryName;
                Properties.Settings.Default.Save();

                GetAssetsClick();

                }


            }

        private enum Task {
            GetBoxart,
            GetBackground,
            GetIconBanner,
            GetScreenshots,
            SetBoxart,
            SetBackground,
            SetIconBanner,
            SetScreenshots,
            }
        }
    }