using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System.IO;

namespace ZXBSInstaller.Controls;

public partial class ScreenShotControl : UserControl
{
    public ScreenShotControl(string fileName)
    {
        InitializeComponent();

        using var stream = File.OpenRead(fileName);
        imgScreenShot.Source = new Bitmap(stream);
    }
}