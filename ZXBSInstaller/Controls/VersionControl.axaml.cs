using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Runtime.InteropServices;
using System.Threading;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;

public partial class VersionControl : UserControl
{
    public ExternalTool Tool { get; set; }
    public ExternalTools_Version ToolVersion = null;

    private bool IsPair = false;

    public static bool IsPairGlobal = false;
    public static SolidColorBrush ColorPair= new SolidColorBrush(Color.FromRgb(20, 20, 20));
    public static SolidColorBrush ColorNormal= new SolidColorBrush(Colors.Black);
    public static SolidColorBrush ColorHover = new SolidColorBrush(Colors.DarkBlue);
    public static SolidColorBrush ColorGreen = new SolidColorBrush(Colors.LightGreen);

    public VersionControl(ExternalTool tool, ExternalTools_Version toolVersion)
    {
        InitializeComponent();

        Tool= tool;
        ToolVersion = toolVersion;
        if (tool == null)
        {
            pnlHeader.IsVisible = true;
            pnlRow.IsVisible = false;
            IsPair = true;
        }
        else
        {
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

            if (toolVersion.OperatingSystem == ServiceLayer.CurrentOperatingSystem)
            {
                btnDownload.Foreground = ColorGreen;
                txtPlatform.Foreground = ColorGreen;
                txtStatus.Foreground = ColorGreen;
                txtVersion.Foreground = ColorGreen;
            }            
        }
    }

    private void btnDownload_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new Thread(() =>
        {
            ServiceLayer.DownloadAndInstallTool(Tool, ToolVersion);
        }).Start();
    }

    private void pnlRow_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        pnlRow.Background = ColorHover;
    }

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