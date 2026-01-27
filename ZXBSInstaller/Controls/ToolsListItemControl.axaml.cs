using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.IO;
using System.Linq;
using System.Threading;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller;

public partial class ToolsListItemControl : UserControl
{
    public ExternalTool ExternalTool { get; set; }
    public ExternalTools_Version LocalVersion = null;

    private ExternalTools_Version AvailableVersion = null;
    private ExternalTools_Version LatestVersion = null;
    private ExternalTools_Version LatestStableVersion = null;


    public ToolsListItemControl()
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

            try
            {
                using var stream = File.OpenRead($"InstallerResources/{ExternalTool.Id}.png");
                imgIcon.Source = new Bitmap(stream);
            }
            catch { }
            txtName.Text = ExternalTool.Name;

            var versions = ExternalTool.Versions.OrderByDescending(d => d.VersionNumber);
            LatestVersion = versions.FirstOrDefault(d => d.OperatingSystem == ServiceLayer.CurrentOperatingSystem);
            LatestStableVersion = versions.FirstOrDefault(d => d.BetaNumber == 0 && d.OperatingSystem == ServiceLayer.CurrentOperatingSystem);
            if (ServiceLayer.GeneralConfig.OnlyStableVersions)
            {
                AvailableVersion = LatestStableVersion == null ?
                    LatestVersion :
                    LatestStableVersion;
            }
            else
            {
                AvailableVersion = LatestVersion;
            }

            if (AvailableVersion == null)
            {
                txtVersion.Text = "ERROR";
                txtVersion.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
            else
            {
                txtVersion.Text = AvailableVersion.Version;
                txtVersion.Foreground = new SolidColorBrush(Colors.Yellow);
            }
            new Thread(GetLocalVersion).Start();
        });
    }


    private void GetLocalVersion()
    {
        LocalVersion = ServiceLayer.GetToolVersion(ExternalTool.Id);

        Dispatcher.UIThread.Post(() =>
        {
            if (LocalVersion == null)
            {
                txtVersion.Text = $"v{AvailableVersion.Version} - Update";
                txtVersion.Foreground = new SolidColorBrush(Avalonia.Media.Colors.Red);
            }
            else if (AvailableVersion == null)
            {
                txtVersion.Text = "ERROR";
                txtVersion.Foreground = new SolidColorBrush(Avalonia.Media.Colors.Red);
            }
            else if (LocalVersion.VersionNumber >= LatestVersion.VersionNumber)
            {
                txtVersion.Text = $"v{LocalVersion.Version} - OK";
                txtVersion.Foreground = new SolidColorBrush(Avalonia.Media.Colors.LightGreen);
            }
            else
            {
                txtVersion.Text = $"v{LocalVersion.Version} - Update";
                txtVersion.Foreground = new SolidColorBrush(Avalonia.Media.Colors.Red);
            }
        });
    }

}