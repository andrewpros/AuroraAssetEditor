﻿// 
// 	App.xaml.cs
// 	AuroraAssetEditor
// 
// 	Created by Swizzy on 08/05/2015
// 	Copyright (c) 2015 Swizzy. All rights reserved.

namespace AuroraAssetEditor {
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using AuroraAssetEditor.Classes;
    using System.Net;
    using System;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        internal static readonly FtpOperations FtpOperations = new FtpOperations();
        internal static readonly LocalOperations LocalOperations = new LocalOperations();

        internal static readonly string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";

        private static readonly Icon Icon =
             Icon.ExtractAssociatedIcon(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(App)).Location), Path.GetFileName(Assembly.GetAssembly(typeof(App)).Location)));

        internal static readonly ImageSource WpfIcon = Imaging.CreateBitmapSourceFromHIcon(Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        private void AppStart(object sender, StartupEventArgs e) {

#if DEBUG
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
#endif

            ServicePointManager.Expect100Continue = false;
            new MainWindow(e.Args).Show();
            }
        }
    }