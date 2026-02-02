using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            var version = ServiceLayer.GetZXBSInstallerVersion("zxbsinstaller");
            if (version != null)
            {
                Title = $"ZX Basic Studio Installer - v{version.Version}";
            }

            var ctrl = new Controls.MainControl();
            pnlMain.Children.Add(ctrl);
        }


        private void btnExit_Click(object? sender, RoutedEventArgs e)
        {
            if (App.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}