using Avalonia.Controls;
using Avalonia.Media.Imaging;
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
                using var stream = File.OpenRead(fileName);
                imgControl.Source = new Bitmap(stream);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
