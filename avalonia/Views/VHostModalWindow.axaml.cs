using Avalonia.Controls;
using Avalonia.Interactivity;
using RBWStack.Avalonia.Models;

namespace RBWStack.Avalonia.Views;

public partial class VHostModalWindow : Window
{
    public bool ResultEnabled { get; private set; }
    public string ResultDomain { get; private set; } = "";

    public VHostModalWindow() { InitializeComponent(); }

    public VHostModalWindow(SiteItemModel site)
    {
        InitializeComponent();
        TxtSiteInfo.Text = $"Dự án: {site.Name} ({site.RelativePath})";
        TxtDomain.Text = site.VHostDomain;
        SwEnabled.IsChecked = site.IsVHostEnabled;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        ResultEnabled = SwEnabled.IsChecked == true;
        ResultDomain = (TxtDomain.Text ?? "").Trim();
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}

