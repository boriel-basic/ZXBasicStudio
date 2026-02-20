using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

public partial class VersionControl : UserControl
{
    /// <summary>
    /// External tool of the item
    /// </summary>
    public ExternalTool Tool { get; set; }
    /// <summary>
    /// Version info of the item
    /// </summary>
    public ExternalTools_Version ToolVersion = null;

    /// <summary>
    /// Pair or odd row
    /// </summary>
    private bool IsPair = false;
    private static bool IsPairGlobal = false;
    /// <summary>
    /// Colours
    /// </summary>
    private static SolidColorBrush ColorPair= new SolidColorBrush(Color.FromRgb(20, 20, 20));
    private static SolidColorBrush ColorNormal= new SolidColorBrush(Colors.Black);
    private static SolidColorBrush ColorHover = new SolidColorBrush(Colors.DarkBlue);
    private static SolidColorBrush ColorGreen = new SolidColorBrush(Colors.LightGreen);
    /// <summary>
    /// CallBack Command
    /// </summary>
    private static Action<string, string> Command = null;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tool">ExternalTool of the item</param>
    /// <param name="toolVersion">Version info of the tool</param>
    /// <param name="callBackCommand">CallBack command</param>
    public VersionControl(ExternalTool tool, ExternalTools_Version toolVersion, Action<string,string> callBackCommand)
    {
        InitializeComponent();

        // Set fields
        Tool= tool;
        ToolVersion = toolVersion;
        Command = callBackCommand;

        if (tool == null)
        {
            // tool = null for a header
            pnlHeader.IsVisible = true;
            pnlRow.IsVisible = false;
            IsPair = true;
        }
        else
        {
            // Display version data
            pnlHeader.IsVisible = false;
            pnlRow.IsVisible = true;
            txtPlatform.Text = ToolVersion.OperatingSystem.ToString();
            txtStatus.Text = ToolVersion.BetaNumber == 0 ? "Stable" : "Beta";
            txtVersion.Text = ToolVersion.Version;
            IsPair = IsPairGlobal;
            if (IsPairGlobal)
            {
                pnlRow.Background = ColorPair;
            }
            IsPairGlobal = !IsPairGlobal;

            // Colour based on version operating system
            if (toolVersion.OperatingSystem == ServiceLayer.CurrentOperatingSystem)
            {
                btnDownload.Foreground = ColorGreen;
                txtPlatform.Foreground = ColorGreen;
                txtStatus.Foreground = ColorGreen;
                txtVersion.Foreground = ColorGreen;
            }
            if (ServiceLayer.CurrentOperatingSystem == OperatingSystems.MacOS_arm64 ||
                ServiceLayer.CurrentOperatingSystem == OperatingSystems.MacOS_x64)
            {
                if(toolVersion.OperatingSystem== OperatingSystems.MacOS_arm64 ||
                   toolVersion.OperatingSystem == OperatingSystems.MacOS_x64 ||
                   toolVersion.OperatingSystem == OperatingSystems.MacOS)
                {
                    btnDownload.Foreground = ColorGreen;
                    txtPlatform.Foreground = ColorGreen;
                    txtStatus.Foreground = ColorGreen;
                    txtVersion.Foreground = ColorGreen;
                }
            }
        }
    }



    /// <summary>
    /// Download button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnDownload_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new Thread(() =>
        {
            ServiceLayer.DownloadAndInstallTool(Tool, ToolVersion);            
            Command(Tool.Id, "REFRESH");
        }).Start();
    }


    /// <summary>
    /// Change color when mouse is over
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void pnlRow_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        pnlRow.Background = ColorHover;
    }


    /// <summary>
    /// Restore color when mouse is out
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void pnlRow_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (IsPair)
        {
            pnlRow.Background = ColorPair;
        }
        else
        {
            pnlRow.Background = ColorNormal;
        }
    }
}