// 
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

        public static bool SetAllowUnsafeHeaderParsing20() {
            //Get the assembly that contains the internal class
            Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
            if(aNetAssembly != null) {
                //Use the assembly in order to get the internal type for the internal class
                Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if(aSettingsType != null) {
                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created allready the property will create it for us.
                    object anInstance = aSettingsType.InvokeMember("Section",
                    BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                    if(anInstance != null) {
                        //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                        FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if(aUseUnsafeHeaderParsing != null) {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, true);
                            return true;
                            }
                        }
                    }
                }
            return false;
            }

        private void AppStart(object sender, StartupEventArgs e) {

            ServicePointManager.Expect100Continue = false;
            SetAllowUnsafeHeaderParsing20();

            new MainWindow(e.Args).Show();
            }
        }
    }