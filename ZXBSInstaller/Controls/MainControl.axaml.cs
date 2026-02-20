using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using ZXBSInstaller.Controls;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

public partial class MainControl : UserControl
{
    private List<ToolItemControl> toolItemControls = new List<ToolItemControl>();
    private static Brush Yellow = new SolidColorBrush(Colors.Yellow);

    public MainControl()
    {
        InitializeComponent();

        this.Loaded += MainControl_Loaded;
        txtBasePath.TextChanged += TxtBasePath_TextChanged;
        chkOnlyStableVersions.IsCheckedChanged += ChkOnlyStableVersions_IsCheckedChanged;
        chkSetZXBSOptions.IsCheckedChanged += ChkSetZXBSOptions_IsCheckedChanged;
    }

    private void MainControl_Loaded(object? sender, RoutedEventArgs e)
    {
        new Thread(Initialize).Start();
    }


    private void Initialize()
    {
        ServiceLayer.Initialize(ShowStatusPanel, UpdateStatus, HideStatusPanel, GetExternalTools, ShowMessage, ExitApp);

        Dispatcher.UIThread.Post(() =>
        {
            txtBasePath.Text = ServiceLayer.GeneralConfig.BasePath;
            chkOnlyStableVersions.IsChecked = ServiceLayer.GeneralConfig.OnlyStableVersions;
            chkSetZXBSOptions.IsChecked = ServiceLayer.GeneralConfig.SetZXBSConfig;
        });

        GetExternalTools();
    }


    private void ShowMessage(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            HideStatusPanel();
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "ZX Basic Studio Installer",
                ContentMessage = message,
                Icon = MsBox.Avalonia.Enums.Icon.Info,
                WindowIcon = ((Window)this.VisualRoot).Icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });
            box.ShowAsPopupAsync(this);
        });
    }


    private void GetExternalTools()
    {
        Dispatcher.UIThread.Post(() =>
        {
            mainVersions.IsVisible = false;
            mainTools.IsVisible = true;

            ShowStatusPanel("Working...");
        });

        var tools = ServiceLayer.GetExternalTools();

        Dispatcher.UIThread.Post(() =>
        {
            HideStatusPanel();
            if (tools == null)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "ERROR",
                    ContentMessage = "Error retrieving the list of tools, please check your Internet connection.\r\nIt may be a temporary server error, report the error to duefectucorp@gmail.com and try again later.",
                    Icon = MsBox.Avalonia.Enums.Icon.Error,
                    WindowIcon = ((Window)this.VisualRoot).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });
                box.ShowAsPopupAsync(this);

            }
            else
            {
                ShowData();
            }
        });
    }


    private void ShowData()
    {
        toolItemControls.Clear();
        var tools = ServiceLayer.ExternalTools;

        pnlTools.Children.Clear();
        foreach (var tool in tools)
        {
            var control = new ToolItemControl(tool, Command_Received);
            toolItemControls.Add(control);
            pnlTools.Children.Add(control);
        }
        UpdateSummary();
    }


    private void Command_Received(string id, string command)
    {
        switch (command)
        {
            case "REFRESH":
                GetExternalTools();
                break;
            case "CHECKED":
                UpdateSummary();
                break;
            case "VERSIONS":
                ShowVersions(id);
                break;
        }
    }


    private void UpdateSummary()
    {
        Dispatcher.UIThread.Post(() =>
        {
            pnlSummary.Children.Clear();
            bool allUpToDate = true;
            // Check for recommended updates/installs
            foreach (var tool in toolItemControls)
            {
                if (tool.IsSelected)
                {
                    var tb = new TextBlock();
                    tb.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                    if (tool.ExternalTool.InstalledVersion == null)
                    {
                        tb.Text = $"- Install:\r\n\t{tool.ExternalTool.Name}\r\n\tPath: {tool.ExternalTool.LocalPath}\r\n";
                    }
                    else
                    {
                        tb.Text = $"- Update:\r\n\t{tool.ExternalTool.Name}\r\n\tPath: {tool.ExternalTool.LocalPath}\r\n";
                    }
                    pnlSummary.Children.Add(tb);
                    allUpToDate = false;
                }
            }
            if (allUpToDate)
            {
                var tb = new TextBlock();
                tb.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                tb.Text = "All tools are up to date.";
                pnlSummary.Children.Add(tb);
            }

            // Separator
            {
                var separator = new Separator()
                {
                    Margin = new Thickness(0, 10, 0, 10)
                };
                pnlSummary.Children.Add(separator);
            }

            // Show tools tree
            {
                pnlSummary.Children.Add(new TextBlock()
                {
                    Text = "Base path:",
                    Foreground = Yellow
                });
                pnlSummary.Children.Add(new TextBlock()
                {
                    Text = ServiceLayer.GeneralConfig.BasePath,
                    Margin = new Thickness(10, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }
            foreach (var tool in toolItemControls)
            {
                var tb = new TextBlock();
                tb.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                if (tool.ExternalTool.InstalledVersion != null)
                {
                    pnlSummary.Children.Add(new TextBlock()
                    {
                        Text = tool.ExternalTool.Name + ":",
                        Margin = new Thickness(0, 8, 0, 0),
                        Foreground = Yellow,
                        TextWrapping = TextWrapping.Wrap
                    });
                    pnlSummary.Children.Add(new TextBlock()
                    {
                        Text = System.IO.Path.Combine(ServiceLayer.GeneralConfig.BasePath, tool.ExternalTool.Id),
                        Margin = new Thickness(10, 4, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }
        });
    }


    private void ShowStatusPanel(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ServiceLayer.Cancel = false;
            txtStatus.Text = message;
            progressBar.Value = 0;
            pnlStatus.IsVisible = true;

            btnInstall.IsEnabled = false;
            btnPlayZXBS.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            btnSelectPath.IsEnabled = false;
        });
    }


    private void HideStatusPanel()
    {
        Dispatcher.UIThread.Post(() =>
        {
            pnlStatus.IsVisible = false;

            btnInstall.IsEnabled = true;
            btnPlayZXBS.IsEnabled = true;
            btnRefresh.IsEnabled = true;
            btnSelectPath.IsEnabled = true;
        });
    }


    /// <summary>
    /// Update status panel
    /// </summary>
    /// <param name="message">Message to display or empty if you don't want to change it</param>
    /// <param name="progress">Progress value (0-100)</param>
    private void UpdateStatus(string message, int progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!string.IsNullOrEmpty(message))
            {
                txtStatus.Text = message;
            }
            if (progress >= 0 && progress <= 100)
            {
                progressBar.Value = progress;
            }
        });
    }

    private void btnSelectPath_Click(object? sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog()
        {
            Title = "Select tools base path"
        };
        dlg.Directory = txtBasePath.Text;
        var wnd = this.GetVisualRoot() as Window;
        dlg.ShowAsync(wnd).ContinueWith((t) =>
        {
            if (!string.IsNullOrEmpty(t.Result))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    txtBasePath.Text = t.Result;
                });
            }
        });
    }


    private void btnInstall_Click(object? sender, RoutedEventArgs e)
    {
        new Thread(ServiceLayer.DownloadAndInstallTools).Start();
    }


    private void ChkSetZXBSOptions_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.GeneralConfig.SetZXBSConfig = chkSetZXBSOptions.IsChecked == true;
        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
    }


    private void ChkOnlyStableVersions_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.GeneralConfig.OnlyStableVersions = chkOnlyStableVersions.IsChecked == true;
        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
    }


    private void TxtBasePath_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ServiceLayer.GeneralConfig.BasePath = txtBasePath.Text;
        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
    }


    private void ShowVersions(string id)
    {
        Dispatcher.UIThread.Post(() =>
        {
            mainTools.IsVisible = false;
            mainVersions.IsVisible = true;
            pnlVersions.Children.Clear();
            var tool = ServiceLayer.ExternalTools.FirstOrDefault(d => d.Id == id);

            {
                var btn = new Button()
                {
                    Content = "Close",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };
                btn.Click += Versions_Close;
                pnlVersions.Children.Add(btn);
            }

            var versionControlHeader = new VersionControl(null, null, Command_Received);
            pnlVersions.Children.Add(versionControlHeader);
            foreach (var version in tool.Versions)
            {
                if (ServiceLayer.GeneralConfig.OnlyStableVersions && version.BetaNumber > 0)
                {
                    continue;
                }
                var versionControl = new VersionControl(tool, version, Command_Received);
                pnlVersions.Children.Add(versionControl);
            }

            {
                var btn = new Button()
                {
                    Content = "Close",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };
                btn.Click += Versions_Close;
                pnlVersions.Children.Add(btn);
            }

        });
    }


    private void Versions_Close(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            mainTools.IsVisible = true;
            mainVersions.IsVisible = false;
        });
    }

    private void btnPlayZXBS_Click(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.RunZXBasicStudio();
        ExitApp();
    }


    private void ExitApp()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (App.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        });
    }


    private void btnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        new Thread(GetExternalTools).Start();
    }

    private void btnCancel_Click(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.Cancel = true;
    }
}