using Avalonia.Controls;
using Avalonia.Interactivity;
using RBWStack.Avalonia.Services;
using RBWStack.Avalonia.ViewModels;
using System;
using System.Diagnostics;
using System.IO;

namespace RBWStack.Avalonia.Views;

public partial class MailSandboxView : UserControl
{
    private bool _showRaw;

    public MailSandboxView() => InitializeComponent();

    private void OnToggleViewModeClick(object? sender, RoutedEventArgs e)
    {
        _showRaw = !_showRaw;
        PnlHtmlPreview.IsVisible = !_showRaw;
        TxtRawPreview.IsVisible = _showRaw;
        TxtModeLabel.Text = _showRaw ? "Xem HTML" : "Xem mã nguồn";
    }

    private void OnOpenInBrowserClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedEmail == null) return;
        try
        {
            string tmpHtml = Path.Combine(PathHelper.TmpDir, $"mail_{vm.SelectedEmail.Id}.html");
            File.WriteAllText(tmpHtml, vm.SelectedEmail.HtmlBody);
            Process.Start(new ProcessStartInfo(tmpHtml) { UseShellExecute = true });
        }
        catch { }
    }
}
