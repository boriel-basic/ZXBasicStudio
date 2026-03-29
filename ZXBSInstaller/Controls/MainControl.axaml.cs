using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using ZXBSInstaller.Controls;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

/// <summary>
/// Main window
/// </summary>
public partial class MainControl : UserControl
{
    /// <summary>
    /// List of ToolItemControl created items
    /// </summary>
    private List<ToolItemControl> toolItemControls = new List<ToolItemControl>();
    /// <summary>
    /// Yellow color for labels
    /// </summary>
    private static Brush Yellow = new SolidColorBrush(Colors.Yellow);

    private CheckBox chkBorielGroup = null;
    private CheckBox chkNextGroup = null;


    /// <summary>
    /// Main constructor
    /// </summary>
    public MainControl()
    {
        InitializeComponent();

        // Set events
        this.Loaded += MainControl_Loaded;
        txtBasePath.TextChanged += TxtBasePath_TextChanged;
        chkOnlyStableVersions.IsCheckedChanged += ChkOnlyStableVersions_IsCheckedChanged;
        chkSetZXBSOptions.IsCheckedChanged += ChkSetZXBSOptions_IsCheckedChanged;
    }


    /// <summary>
    /// Initialize in a new Thread when loaded
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainControl_Loaded(object? sender, RoutedEventArgs e)
    {
        new Thread(Initialize).Start();
    }


    /// <summary>
    /// Initialize
    /// </summary>
    private void Initialize()
    {
        // Initialize ServiceLayer
        ServiceLayer.Initialize(ShowStatusPanel, UpdateStatus, HideStatusPanel, GetExternalTools, ShowMessage, ExitApp);

        // Set config fields in UIThread
        Dispatcher.UIThread.Post(() =>
        {
            txtBasePath.Text = ServiceLayer.GeneralConfig.BasePath;
            chkOnlyStableVersions.IsChecked = ServiceLayer.GeneralConfig.OnlyStableVersions;
            chkSetZXBSOptions.IsChecked = ServiceLayer.GeneralConfig.SetZXBSConfig;
        });

        // Get external tools list
        GetExternalTools();
    }


    /// <summary>
    /// Show a message into a dialog box
    /// </summary>
    /// <param name="message">Message to display</param>
    private void ShowMessage(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            HideStatusPanel();
            txtModalMessage.Text = message;
            pnlModal.IsVisible = true;
        });
    }


    /// <summary>
    /// Get list of external tools and local versions
    /// </summary>
    private void GetExternalTools()
    {
        // Set UI...
        Dispatcher.UIThread.Post(() =>
        {
            mainVersions.IsVisible = false;
            mainTools.IsVisible = true;
            ShowStatusPanel("Working...");
        });

        // Get tools
        var tools = ServiceLayer.GetExternalTools();

        HideStatusPanel();
        if (tools == null)
        {
            // No tools, no way!
            ExitApp();
        }
        else
        {
            // Show tools
            ShowData();
        }
    }


    /// <summary>
    /// Show tools info
    /// </summary>
    private void ShowData()
    {
        Dispatcher.UIThread.Post(() =>
        {
            toolItemControls.Clear();
            var tools = ServiceLayer.ExternalTools;

            mainTools.Children.Clear();
            // ZX Basic Studio
            {
                var groupTools = tools.Where(t => t.Group == "ZX Basic Studio");
                {
                    if(chkBorielGroup == null)
                    {
                        chkBorielGroup = new CheckBox()
                        {
                            Content = "ZX Basic Studio",
                            FontSize = 16,
                            IsChecked = true,
                            Margin = new Thickness(10, 10, 0, 0),
                            Foreground = Yellow
                        };
                        chkBorielGroup.IsCheckedChanged += ZXBorielGropu_Changed;
                    }
                    mainTools.Children.Add(chkBorielGroup);
                    mainTools.Children.Add(new Separator()
                    {
                        Margin = new Thickness(0),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                    });
                }
                var pnlTools = new WrapPanel();
                mainTools.Children.Add(pnlTools);
                ShowData_AddTools(pnlTools, groupTools);
            }

            // Next
            {
                var groupTools = tools.Where(t => t.Group == "ZX Spectrum Next");
                {
                    if (chkNextGroup == null)
                    {
                        chkNextGroup = new CheckBox()
                        {
                            Content = "ZX Spectrum Next",
                            FontSize = 16,
                            IsChecked = true,
                            Margin = new Thickness(10, 10, 0, 0),
                            Foreground = Yellow
                        };
                        chkNextGroup.IsCheckedChanged += ZXNextGropu_Changed;
                    }
                    mainTools.Children.Add(chkNextGroup);
                    mainTools.Children.Add(new Separator()
                    {
                        Margin = new Thickness(0),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                    });
                }
                var pnlTools = new WrapPanel();
                mainTools.Children.Add(pnlTools);
                ShowData_AddTools(pnlTools, groupTools);
            }

            // Update summary area
            UpdateSummary();
        });
    }


    private void ZXBorielGropu_Changed(object? sender, RoutedEventArgs e)
    {
        foreach (var ctrl in toolItemControls)
        {
            if (ctrl.ExternalTool.Group == "ZX Basic Studio")
            {
                ctrl.IsSelected = chkBorielGroup.IsChecked == true;
            }
        }
    }


    private void ZXNextGropu_Changed(object? sender, RoutedEventArgs e)
    {
        foreach(var ctrl in toolItemControls)
        {
            if (ctrl.ExternalTool.Group == "ZX Spectrum Next")
            {
                ctrl.IsSelected = chkNextGroup.IsChecked == true;
            }
        }
    }


    private void ShowData_AddTools(WrapPanel panel, IEnumerable<ExternalTool> tools)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var tool in tools)
            {
                // Create on ToolItemControl foreach tool
                var control = new ToolItemControl(tool, Command_Received);
                toolItemControls.Add(control);
                panel.Children.Add(control);
            }
        });
    }


    /// <summary>
    /// Command received from sub-controls
    /// </summary>
    /// <param name="id">Tool id</param>
    /// <param name="command">Command</param>
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


    /// <summary>
    /// Show Summary panel
    /// </summary>
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
                    // Data for selected tools
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
                // Nothing to update
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

            // Show base path
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
            // Show tools installation paths
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


    /// <summary>
    /// Show status panel
    /// </summary>
    /// <param name="message">Message to display</param>
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


    /// <summary>
    /// Hide status panel
    /// </summary>
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


    /// <summary>
    /// Button select path
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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



    /// <summary>
    /// Button install components
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnInstall_Click(object? sender, RoutedEventArgs e)
    {
        new Thread(ServiceLayer.DownloadAndInstallTools).Start();
    }


    /// <summary>
    /// chkSetZXBSOptions checked changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ChkSetZXBSOptions_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.GeneralConfig.SetZXBSConfig = chkSetZXBSOptions.IsChecked == true;
        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
    }


    /// <summary>
    /// chkOnlyStableVersions checked changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ChkOnlyStableVersions_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.GeneralConfig.OnlyStableVersions = chkOnlyStableVersions.IsChecked == true;
        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
    }


    /// <summary>
    /// Base path changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TxtBasePath_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ServiceLayer.GeneralConfig.BasePath = txtBasePath.Text;
        ServiceLayer.SaveConfig(ServiceLayer.GeneralConfig);
    }


    /// <summary>
    /// Show version info for a tool
    /// </summary>
    /// <param name="id">Tool id</param>
    private void ShowVersions(string id)
    {
        Dispatcher.UIThread.Post(() =>
        {
            mainTools.IsVisible = false;
            mainVersions.IsVisible = true;
            pnlVersions.Children.Clear();

            var tool = ServiceLayer.ExternalTools.FirstOrDefault(d => d.Id == id);

            // Close button
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

            // Header
            var versionControlHeader = new VersionControl(null, null, Command_Received);
            pnlVersions.Children.Add(versionControlHeader);
            foreach (var version in tool.Versions)
            {
                // Version line
                if (ServiceLayer.GeneralConfig.OnlyStableVersions && version.BetaNumber > 0)
                {
                    continue;
                }
                var versionControl = new VersionControl(tool, version, Command_Received);
                pnlVersions.Children.Add(versionControl);
            }

            // Anothe Close button at the bottom of the list
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


    /// <summary>
    /// Versions button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Versions_Close(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            mainTools.IsVisible = true;
            mainVersions.IsVisible = false;
        });
    }


    /// <summary>
    /// Run ZXBS button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnPlayZXBS_Click(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.RunZXBasicStudio();
        ExitApp();
    }


    /// <summary>
    /// Exit application
    /// </summary>
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


    /// <summary>
    /// Refresh button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        new Thread(GetExternalTools).Start();
    }


    /// <summary>
    /// Cacel button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnCancel_Click(object? sender, RoutedEventArgs e)
    {
        ServiceLayer.Cancel = true;
    }

    private void btnModalClose_Click(object? sender, RoutedEventArgs e)
    {
        pnlModal.IsVisible = false;
    }
}