using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Reflection;

namespace ZXBasicStudio;

public partial class ZXAboutDialog : Window
{
    public ZXAboutDialog()
    {
        InitializeComponent();

        txtBuild.Text = Program.Version;
        txtDate.Text = Program.VersionDate;

        btnClose.Click += BtnClose_Click;
    }


    private DateTime GetBuildDate()
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        const int peHeaderOffset = 60;
        const int linkerTimestampOffset = 8;

        byte[] buffer = new byte[2048];

        using (FileStream fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
        {
            fs.Read(buffer, 0, buffer.Length);
        }

        int offset = BitConverter.ToInt32(buffer, peHeaderOffset);
        int secondsSince1970 = BitConverter.ToInt32(buffer, offset + linkerTimestampOffset);
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime linkTimeUtc = epoch.AddSeconds(secondsSince1970);
        return linkTimeUtc.ToLocalTime();
    }


    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}