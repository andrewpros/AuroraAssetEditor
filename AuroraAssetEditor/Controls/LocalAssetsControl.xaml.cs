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
    using Properties;
    using System.Collections.Generic;
    using Ts = System.Threading.Tasks;
    using System.Collections.Concurrent;
    using System.Diagnostics;

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
        private string GameDataDir;

        private readonly XboxAssetDownloader _xboxAssetDownloader = new XboxAssetDownloader();
        private readonly BackgroundWorker _xboxWorker = new BackgroundWorker();


        public LocalAssetsControl(MainWindow main, BoxartControl boxart, BackgroundControl background, IconBannerControl iconBanner, ScreenshotsControl screenshots) {
            InitializeComponent();
            _main = main;
            _boxart = boxart;
            _background = background;
            _iconBanner = iconBanner;
            _screenshots = screenshots;

            FtpAssetsBox.ItemsSource = _assetsList;

            onlyNewSync.IsChecked = Settings.Default.OnlyNew;
            sliderScreens.Value = Settings.Default.NumScreens;

            #region Xbox.com Worker

            _xboxWorker.WorkerReportsProgress = true;
            _xboxWorker.WorkerSupportsCancellation = true;

            var xboxMarketDataResult = new ConcurrentBag<XboxTitleInfo>();
            var omitedTitles = new ConcurrentBag<AuroraDbManager.ContentItem>();

            _xboxWorker.DoWork += (sender, args) => {

                var bgWork = sender as BackgroundWorker;

                try {

                    if(_assetsList.Count ==0) {
                        Dispatcher.Invoke(new Action(() => Status.Text = "No games..."));
                        return;
                        }

                    xboxMarketDataResult = new ConcurrentBag<XboxTitleInfo>();
                    omitedTitles = new ConcurrentBag<AuroraDbManager.ContentItem>();
                    XboxLocale loc = args.Argument as XboxLocale;

                    var _assetsToGo = _assetsList.Where(p => p.TitleIdNumber > 0);


                    Dispatcher.Invoke(new Action(() => _main.busyProgress.Visibility = Visibility.Visible));
                    Dispatcher.Invoke(new Action(() => _main.busyProgress.Text= "0 / " + _assetsToGo.Count()));


                    var maxScreens = Settings.Default.NumScreens;

                    int progressCount = 0;
                    Object progressLock = new Object();


                    Ts.Parallel.ForEach(_assetsToGo, (ast) => {

                        if(bgWork.CancellationPending) {
                            args.Cancel = true;
                            }

                        XboxTitleInfo _titleResult = _xboxAssetDownloader.GetTitleInfoSingle(ast.TitleIdNumber, loc);
                        if(_titleResult.AssetsInfo!= null) {
                            xboxMarketDataResult.Add(_titleResult);
                            }
                        else {
                            omitedTitles.Add(ast);
                            }


                        lock (progressLock) {
                            bgWork.ReportProgress(1, (++progressCount) + " / " + _assetsToGo.Count());
                            }
                    });

                    Ts.Parallel.ForEach(xboxMarketDataResult, (titleItem) => {

                    });

                    Dispatcher.Invoke(new Action(() => Status.Text = "Finished downloading asset information..."));

                    }
                catch(Exception ex) {
                    MainWindow.SaveError(ex);
                    Dispatcher.Invoke(new Action(() => Status.Text = "An error has occured, check error.log for more information..."));
                    }
            };
            _xboxWorker.RunWorkerCompleted += (sender, args) => {

                _main.busyProgress.Visibility = Visibility.Collapsed;
                _main.BusyIndicator.Visibility = Visibility.Collapsed;
                syncDb.IsEnabled = true;

                if(omitedTitles.Count >0) {

                    var logPath = Path.GetTempPath() + Path.DirectorySeparatorChar + "omittedTitlesLog.log";

                    string logcontent = "";
                    foreach(var item in omitedTitles) {
                        logcontent += "Title name: " + item.TitleName + @"

TitleID: " + item.TitleId + @"

Title int: " + item.TitleIdNumber + @"

MediaID: " + item.MediaId + @"
-----------------------------------------------

";
                        }

                    File.WriteAllText(logPath, logcontent);

                    var msgresult = MessageBox.Show("Omitted titles " + omitedTitles.Count + @"

Show log?", "Action", MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.Cancel);

                    if(msgresult == MessageBoxResult.OK) {
                        try {
                            Process.Start(logPath);
                            }
                        catch {


                            }
                        }

                    }
                // var disp = new List<XboxTitleInfo.XboxAssetInfo>();

            };

            _xboxWorker.ProgressChanged+=_xboxWorker_ProgressChanged;

            #endregion

            }

        private void _xboxWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            _main.busyProgress.Text= e.UserState.ToString();
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
                        File.Copy(ContentDbPath, path, true);

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
                GameDataDir = contentdbFile.Directory.Parent.FullName + Path.DirectorySeparatorChar + "GameData";

                textBoxDir.Text = ContentDbPath;

                Properties.Settings.Default.LastDir = contentdbFile.DirectoryName;
                Properties.Settings.Default.Save();

                App.LocalOperations.GameDataDir = GameDataDir;

                GetAssetsClick();

                syncDb.IsEnabled = true;

                }


            }

        private void syncDb_Click(object sender, RoutedEventArgs e) {
            var loc = Settings.Default.LocaleMarketName;

            _main.BusyIndicator.Visibility = Visibility.Visible;
            syncDb.IsEnabled = false;
            _xboxWorker.RunWorkerAsync(new XboxLocale(loc, loc));
            }

        private void sliderScreens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Settings.Default.NumScreens = Convert.ToInt32(sliderScreens.Value);
            Settings.Default.Save();
            }

        private void onlyNewSync_Checked(object sender, RoutedEventArgs e) {
            Settings.Default.OnlyNew = true;
            Settings.Default.Save();
            }

        private void onlyNewSync_Unchecked(object sender, RoutedEventArgs e) {
            Settings.Default.OnlyNew = false;
            Settings.Default.Save();
            }



        private void GetAssetsOnline(object sender, RoutedEventArgs e) {

            var context = Ts.TaskScheduler.FromCurrentSynchronizationContext();

            var titleItem = FtpAssetsBox.SelectedItem  as AuroraDbManager.ContentItem;

            var loc_str = Settings.Default.LocaleMarketName;

            var screenNum = Settings.Default.NumScreens;

            int screenCount = 0;

            var loc = new XboxLocale(loc_str, loc_str);

            if(titleItem!= null) {
                _main.BusyIndicator.Visibility= Visibility.Visible;

                Ts.Task.Factory.StartNew(() => {


                    XboxTitleInfo _titleResult = _xboxAssetDownloader.GetTitleInfoSingle(titleItem.TitleIdNumber, loc);

                    var unityCovers = XboxUnity.GetUnityCoverInfo(titleItem.TitleId);

                    if(unityCovers.Count() > 0) {
                        var cover = unityCovers.FirstOrDefault(p => p._unityResponse.Official == true);
                        if(cover!= null) {
                            _boxart.Load(cover.GetCover());
                            }
                        }

                    Ts.Parallel.ForEach(_titleResult.AssetsInfo, (ast) => {

                        if(!ast.HaveAsset) {

                            if(ast.AssetType == XboxTitleInfo.XboxAssetType.Screenshot && screenCount >= screenNum) {
                                return;
                                }
                            var XboxAsset = ast.GetAsset();
                            //   var assetFile = new AuroraAsset.AssetFile();
                            switch(ast.AssetType) {
                                case XboxTitleInfo.XboxAssetType.Icon:
                                _iconBanner.Load(XboxAsset.Image, true);
                                break;
                                case XboxTitleInfo.XboxAssetType.Banner:
                                _iconBanner.Load(XboxAsset.Image, false);
                                break;
                                case XboxTitleInfo.XboxAssetType.Background:
                                _background.Load(XboxAsset.Image);
                                break;
                                case XboxTitleInfo.XboxAssetType.Screenshot:
                                _screenshots.Load(XboxAsset.Image, false);
                                screenCount++;
                                break;
                                default:
                                break;
                                }
                            }

                    });

                }).ContinueWith((t) => {
                    _main.BusyIndicator.Visibility= Visibility.Collapsed;
                }, context);


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