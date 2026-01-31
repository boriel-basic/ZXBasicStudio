using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

public partial class ToolItemControl : UserControl
{
    public ExternalTool ExternalTool { get; set; }
    public ExternalTools_Version LocalVersion = null;

    private ExternalTools_Version AvailableVersion = null;
    private ExternalTools_Version LatestVersion = null;
    private ExternalTools_Version LatestStableVersion = null;

    public ToolItemControl()
    {
        InitializeComponent();
    }


    public void Refresh()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ExternalTool == null)
            {
                return;
            }

            ShowImage($"InstallerResources/{ExternalTool.Id}.png", imgIcon);

            txtName.Text = ExternalTool.Name;
            txtDescription.Text = ExternalTool.Description;
            txtAuthor.Text = $"Author: {ExternalTool.Author}";
            txtLicense.Text = $"License: {ExternalTool.LicenseType}";

            string localPath = "";
            if (ServiceLayer.GeneralConfig.ExternalTools_Paths != null)
            {
                var tp = ServiceLayer.GeneralConfig.ExternalTools_Paths.FirstOrDefault(d => d.Id == ExternalTool.Id);
                if (tp != null)
                {
                    localPath = tp.LocalPath;
                }
            }
            if (string.IsNullOrEmpty(localPath))
            {
                localPath = Path.Combine(ServiceLayer.GeneralConfig.BasePath, ExternalTool.Id);
                ServiceLayer.GeneralConfig.ExternalTools_Paths.Add(
                    new ExternalTools_Path()
                    {
                        Id = ExternalTool.Id,
                        LocalPath = localPath
                    });
                ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
            }
            txtPath.Text = $"Path: {localPath}";

            var versions = ExternalTool.Versions.OrderByDescending(d => d.VersionNumber);
            LatestVersion = versions.FirstOrDefault(d => d.OperatingSystem == ServiceLayer.CurrentOperatingSystem);
            LatestStableVersion = versions.FirstOrDefault(d => d.BetaNumber == 0 && d.OperatingSystem == ServiceLayer.CurrentOperatingSystem);
            if (LatestVersion == null)
            {
                txtVersion.Text = "ERROR";
                txtLatestVersion.Text = "ERROR";
                txtLatestStableVersion.Text = "ERROR";
            }
            else
            {
                if (ServiceLayer.GeneralConfig.OnlyStableVersions)
                {
                    txtVersion.Text = LatestStableVersion == null ?
                        $"Version: {LatestVersion.Version}" :
                        $"Version: {LatestStableVersion.Version}";
                    AvailableVersion = LatestStableVersion ?? LatestVersion;
                }
                else
                {
                    AvailableVersion = LatestVersion ?? LatestStableVersion;
                }

                txtLatestVersion.Text = $"Latest available version: {LatestVersion.Version}";
                txtLatestStableVersion.Text = LatestStableVersion == null ?
                    "Latest stable version: No stable versions found" :
                    $"Latest stable version: {LatestStableVersion.Version}";
            }

            ShowScreenShots();
            ShowLocalVersion();
            ShowVersions();

            btnLicense.IsEnabled = !string.IsNullOrEmpty(ExternalTool.LicenceUrl);
        });
    }


    private void ShowScreenShots()
    {
        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "InstallerResources");
            var sc = $"{ExternalTool.Id}.scr.*";
            var files = Directory.GetFiles(path, sc).OrderBy(d => d);
            foreach (var file in files)
            {
                var ctrl = new ScreenShotControl(file);
                pnlScreenShhots.Children.Add(ctrl);
            }
        }
        catch (Exception ex)
        {
        }
    }


    private void ShowImage(string fileName, Image imgControl)
    {
        try
        {
            using var stream = File.OpenRead(fileName);
            imgControl.Source = new Bitmap(stream);
        }
        catch (Exception ex)
        {
        }
    }


    private void ShowLocalVersion()
    {
        if (LocalVersion == null)
        {
            LocalVersion = ServiceLayer.GetToolVersion(ExternalTool.Id);
        }

        txtChecking.IsVisible = false;
        txtUpToDate.IsVisible = false;
        txtRecomended.IsVisible = false;
        btnUpdate.IsEnabled = false;

        if (LocalVersion == null)
        {
            txtVersion.Text = "ERROR";
            txtVersion.Foreground = new SolidColorBrush(Colors.Red);
            txtLocalVersion.Text = "Local installed version: Not detected";
        }
        else
        {
            txtVersion.Text = LocalVersion.Version;
            txtLocalVersion.Text = $"Local installed version: {LocalVersion.Version}";
        }

        if (LocalVersion == null)
        {
            txtLocalVersion.Foreground = new SolidColorBrush(Colors.Red);
            txtRecomended.Text = "Download recomended";
            txtRecomended.IsVisible = true;
            btnUpdate.IsEnabled = true;
            btnUpdate.Background = new SolidColorBrush(Colors.LightGreen);
            btnUpdate.Foreground = new SolidColorBrush(Colors.Black);
        }
        else if (AvailableVersion == null)
        {
            txtLatestVersion.Foreground = new SolidColorBrush(Colors.Red);
            txtLatestStableVersion.Foreground = new SolidColorBrush(Colors.Red);
            txtRecomended.Text = "No download available";
            txtRecomended.IsVisible = true;
        }
        else if (LocalVersion.VersionNumber >= AvailableVersion.VersionNumber)
        {
            txtLocalVersion.Foreground = new SolidColorBrush(Colors.LightGreen);
            txtUpToDate.Text = $"Up to date";
            txtUpToDate.IsVisible = true;
        }
        else
        {
            txtLocalVersion.Foreground = new SolidColorBrush(Colors.Red);
            txtRecomended.Text = "Download recomended";
            txtRecomended.IsVisible = true;
            btnUpdate.IsEnabled = true;
            btnUpdate.Background = new SolidColorBrush(Colors.LightGreen);
            btnUpdate.Foreground = new SolidColorBrush(Colors.Black);
        }
    }


    private void ShowVersions()
    {
        pnlVersions.Children.Clear();
        var header = new VersionControl(null,null);
        pnlVersions.Children.Add(header);
        var versions = ExternalTool.Versions.OrderByDescending(d => d.VersionNumber).ThenBy(d => d.OperatingSystem);
        foreach (var version in versions)
        {
            var ctrl = new VersionControl(ExternalTool, version);
            pnlVersions.Children.Add(ctrl);
        }
    }


    private void btnLicense_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ServiceLayer.OpenUrlInBrowser(ExternalTool.LicenceUrl);
    }


    private void btnVisit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ServiceLayer.OpenUrlInBrowser(ExternalTool.SiteUrl);
    }

    private void btnUpdate_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new Thread(() =>
        {
            ServiceLayer.DownloadAndInstallTool(ExternalTool, AvailableVersion);
        }).Start();
    }

    private void btnViewVersions_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        pnlVersions.IsVisible = !pnlVersions.IsVisible;
        if (pnlVersions.IsVisible)
        {
            btnViewVersions.Content = "Hide all versions";
        }
        else
        {
            btnViewVersions.Content = "Show all versions";
        }
    }

    private void btnSetPath_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                    txtPath.Text = $"Path: {t.Result}";
                    var tool = ServiceLayer.GeneralConfig.ExternalTools_Paths.FirstOrDefault(d => d.Id == ExternalTool.Id);
                    if (tool != null)
                    {
                        tool.LocalPath = t.Result;
                    }
                    else
                    {
                        ServiceLayer.GeneralConfig.ExternalTools_Paths.Add(new ExternalTools_Path()
                        {
                            Id = ExternalTool.Id,
                            LocalPath = t.Result
                        });
                        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
                    }
                });
            }
        });
    }
}