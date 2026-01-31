using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.IO;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

public partial class ConfigControl : UserControl
{
    private Action<bool> callBack = null;


    public ConfigControl()
    {
        InitializeComponent();
    }


    public void Show(Action<bool> callBack)
    {
        this.callBack = callBack;

        Dispatcher.UIThread.Post(() =>
        {
            this.IsVisible = true;
            var cfg = ServiceLayer.GetConfig();
            if (cfg == null)
            {
                cfg = new Log.Neg.Config()
                {
                    BasePath = "c:\\ZXSpectrum",
                    OnlyStableVersions = true
                };
            }

            txtToolsBasePath.Text = cfg.BasePath;
            chkOnlyStableVersions.IsChecked = cfg.OnlyStableVersions;
        });
    }


    private void btnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
        callBack?.Invoke(false);
    }

    private void btnSave_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var cfg = ServiceLayer.CreateConfig();
        cfg.BasePath = txtToolsBasePath.Text;
        cfg.OnlyStableVersions = chkOnlyStableVersions.IsChecked ?? true;

        ServiceLayer.SaveConfig(cfg);
        Close();
        callBack?.Invoke(true);
    }


    private void Close()
    {
        this.IsVisible = false;
    }

    private void btnBrowseToolsBasePath_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog()
        {
            Title = "Select tools base path"
        };
        var wnd = this.GetVisualRoot() as Window;
        dlg.ShowAsync(wnd).ContinueWith((t) =>
        {
            if (!string.IsNullOrEmpty(t.Result))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    txtToolsBasePath.Text = t.Result;
                });
            }
        });
    }
}