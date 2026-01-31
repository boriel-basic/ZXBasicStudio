using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls2;

public partial class ToolItemControl : UserControl
{
    public ExternalTool ExternalTool = null;
    public bool IsSelected = false;

    public static SolidColorBrush colorRed = new SolidColorBrush(Colors.Red);
    public static SolidColorBrush colorGreen = new SolidColorBrush(Colors.LightGreen);

    private static Action Checked_Changed = null;


    public ToolItemControl(ExternalTool tool, Action callBackChecked_Changed)
    {
        InitializeComponent();

        ExternalTool = tool;
        Checked_Changed = callBackChecked_Changed;

        UITools.ShowImage($"InstallerResources/{ExternalTool.Id}.png", imgIcon);
        txtName.Text = tool.Name;
        txtDescription.Text = tool.Description;
        txtPath.Text = "Path: " + tool.LocalPath;

        IsSelected = tool.UpdateNeeded;
        tool.IsSelected = IsSelected;
        chkSelect.IsChecked = IsSelected;

        if (tool.InstalledVersion == null)
        {
            txtActual.Text = $"Installed version: None";
            txtActual.Foreground = colorRed;
        }
        else
        {
            txtActual.Text = $"Installed version: {tool.InstalledVersion.Version}";
            if (tool.UpdateNeeded)
            {
                txtActual.Foreground = colorRed;
            }
            else
            {
                txtActual.Foreground = colorGreen;
            }
        }

        if (tool.LatestVersion == null)
        {
            txtLatest.Text = $"Latest version: Unknow";
        }
        else
        {
            txtLatest.Text = $"Latest version: {tool.LatestVersion.Version}";
        }
    }

    private void chkSelect_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsSelected = chkSelect.IsChecked == true;
        ExternalTool.IsSelected = IsSelected;
        Checked_Changed?.Invoke();
    }
}