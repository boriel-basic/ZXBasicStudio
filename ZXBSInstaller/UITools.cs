using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBSInstaller
{
    internal static class UITools
    {
        public static void ShowImage(string fileName, Image imgControl)
        {
            try
            {
                var uri= new Uri($"avares://ZXBSInstaller/Assets/{fileName}");
                var asset=AssetLoader.Open(uri);
                var bitmap=new Bitmap(asset);
                imgControl.Source = bitmap;
            }
            catch (Exception ex)
            {
            }
        }
    }
}
