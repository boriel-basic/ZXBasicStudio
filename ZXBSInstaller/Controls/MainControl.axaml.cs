using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

public partial class MainControl : UserControl
{
    private ExternalTool[] tools = null;
    private List<ToolsListItemControl> toolsControls = null;

    public MainControl()
    {
        InitializeComponent();

        this.Loaded += MainControl_Loaded;
        this.lstTools.SelectionChanged += LstTools_SelectionChanged;
    }

    private void MainControl_Loaded(object? sender, RoutedEventArgs e)
    {
        new Thread(Initialize).Start();
    }


    private void Initialize()
    {
        ServiceLayer.Initialize(ShowStatusPanel, UpdateStatus, HideStatusPanel, GetExternalTools, ShowMessage);
        if (ServiceLayer.GeneralConfig == null)
        {
            ShowConfig();
        }
        else
        {
            GetExternalTools();
        }
    }


    private void ShowMessage(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            pnlStatus.IsVisible = false;
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
            txtStatus.Text = "Working...";
            progressBar.Value = 0;
            pnlStatus.IsVisible = true;
        });

        tools = ServiceLayer.GetExternalTools();

        Dispatcher.UIThread.Post(() =>
        {
            pnlStatus.IsVisible = false;
            if (tools == null)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "ERROR",
                    ContentMessage = "Error retrieving the list of tools. Please check your internet connection.",
                    Icon = MsBox.Avalonia.Enums.Icon.Error,
                    WindowIcon = ((Window)this.VisualRoot).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });
                box.ShowAsPopupAsync(this);
            }
            else
            {
                ShowData(tools);
            }
        });
    }


    private void ShowData(ExternalTool[] tools)
    {
        toolsControls = new List<ToolsListItemControl>();

        lstTools.Items.Clear();
        foreach (var tool in tools)
        {
            var control = new ToolsListItemControl();
            control.ExternalTool = tool;
            control.Refresh();
            toolsControls.Add(control);
            lstTools.Items.Add(control);
        }
        lstTools.SelectedIndex = 0;
    }


    private void ShowStatusPanel(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            txtStatus.Text = message;
            progressBar.Value = 0;
            pnlStatus.IsVisible = true;
        });
    }


    private void HideStatusPanel()
    {
        Dispatcher.UIThread.Post(() =>
        {
            pnlStatus.IsVisible = false;
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


    private void LstTools_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (lstTools.SelectedItems == null || lstTools.SelectedItems.Count == 0)
        {
            return;
        }

        var ctrl = lstTools.SelectedItems[0] as ToolsListItemControl;
        if (ctrl == null)
        {
            return;
        }

        var tool = new ToolItemControl();
        tool.ExternalTool = ctrl.ExternalTool;
        tool.LocalVersion = ctrl.LocalVersion;
        tool.Refresh();

        pnlWorking.Children.Clear();
        pnlWorking.Children.Add(tool);
    }

    private void btnConfig_Click(object? sender, RoutedEventArgs e)
    {
        ShowConfig();
    }


    private void ShowConfig()
    {
        Dispatcher.UIThread.Post(() =>
        {
            pnlModalContainer.Children.Clear();
            pnlModalOverlay.IsVisible = true;
            var dlgConfig = new ConfigControl();
            dlgConfig.Show(ShowConfig_Closed);
            pnlModalContainer.Children.Add(dlgConfig);
        });
    }


    private void ShowConfig_Closed(bool saved)
    {
        pnlModalOverlay.IsVisible = false;
        pnlModalContainer.Children.Clear();

        if (saved)
        {
            new Thread(GetExternalTools).Start();
        }
    }
}