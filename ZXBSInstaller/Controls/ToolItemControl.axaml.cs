using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using ZXBSInstaller.Log;
using ZXBSInstaller.Log.Neg;

namespace ZXBSInstaller.Controls;


/// <summary>
/// Box with the tool information
/// </summary>
public partial class ToolItemControl : UserControl
{
    /// <summary>
    /// External tool for the box
    /// </summary>
    public ExternalTool ExternalTool = null;
    /// <summary>
    /// Tool selected for installation?
    /// </summary>
    public bool IsSelected
    {
        get
        {
            return _IsSelected;
        }
        set
        {
            _IsSelected = value;
            SetSelected(value);
        }
    }

    private bool _IsSelected = false;

    // Colors
    public static SolidColorBrush colorRed = new SolidColorBrush(Colors.Red);
    public static SolidColorBrush colorGreen = new SolidColorBrush(Colors.LightGreen);

    /// <summary>
    /// Command callBack
    /// </summary>
    private static Action<string, string> Command = null;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tool">External tool to display</param>
    /// <param name="callBackCommand">Command delagate</param>
    public ToolItemControl(ExternalTool tool, Action<string, string> callBackCommand)
    {
        InitializeComponent();

        // Set fileds
        ExternalTool = tool;
        Command = callBackCommand;

        // Show tool image and data
        UITools.ShowImage($"{ExternalTool.Id}.png", imgIcon);
        txtName.Text = tool.Name;
        txtDescription.Text = tool.Description;
        txtPath.Text = "Path: " + tool.LocalPath;
        txtLicense.Text = "Licence: " + tool.LicenseType;
        txtAuthor.Text = "Author(s): " + tool.Author;
        
        // Set chkSelect status
        IsSelected = tool.UpdateNeeded;
        tool.IsSelected = IsSelected;
        chkSelect.IsChecked = IsSelected;

        // Display installed version
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

        // Display latest version
        if (tool.LatestVersion == null)
        {
            txtLatest.Text = $"Latest version: Unknow";
        }
        else
        {
            txtLatest.Text = $"Latest version: {tool.LatestVersion.Version}";
        }
    }


    /// <summary>
    /// ChkSelected changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void chkSelect_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSelected(chkSelect.IsChecked == true);
    }


    /// <summary>
    /// Versions button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnAllVersions_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Command(ExternalTool.Id, "VERSIONS");
    }

    private void btnViewSite_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ServiceLayer.OpenUrlInBrowser(ExternalTool.SiteUrl);
    }


    private void SetSelected(bool selected)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _IsSelected = selected;
            ExternalTool.IsSelected = _IsSelected;
            if (chkSelect.IsChecked != _IsSelected)
            {
                chkSelect.IsChecked = _IsSelected;
            }
            Command(ExternalTool.Id, "CHECKED");
        });
    }
}