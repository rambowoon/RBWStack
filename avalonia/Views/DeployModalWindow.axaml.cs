using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RBWStack.Avalonia.Models;
using RBWStack.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RBWStack.Avalonia.Views;

public partial class DeployModalWindow : Window
{
    private readonly SiteItemModel? _site;
    private readonly List<DemoServerModel> _servers = new();
    private readonly DeployService _deployService = new();
    private string _lastDemoUrl = "";

    public DeployModalWindow() { InitializeComponent(); }

    public DeployModalWindow(SiteItemModel site, IEnumerable<DemoServerModel> servers)
    {
        InitializeComponent();
        _site = site;
        _servers = servers.ToList();
        TxtProject.Text = $"Dự án: {site.Name} — {site.FullPath}";
        CbServer.ItemsSource = _servers.Select(s => string.IsNullOrWhiteSpace(s.Domain) ? s.Name : $"{s.Name} ({s.Domain})").ToList();
        int defIdx = _servers.FindIndex(s => s.IsDefault);
        CbServer.SelectedIndex = defIdx >= 0 ? defIdx : (_servers.Count > 0 ? 0 : -1);
        UpdatePreviewUrl();
    }

    private void OnServerSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdatePreviewUrl();
    private void OnSslChanged(object? sender, RoutedEventArgs e) => UpdatePreviewUrl();

    private string GetSelectedDomain()
    {
        if (CbServer.SelectedIndex >= 0 && CbServer.SelectedIndex < _servers.Count)
            return (_servers[CbServer.SelectedIndex].Domain ?? "").Trim();
        return "";
    }

    private void UpdatePreviewUrl()
    {
        if (_site == null || TxtPreviewUrl == null) return;
        string dom = GetSelectedDomain();
        string scheme = SwSsl?.IsChecked == true ? "https://" : "http://";
        string rel = _site.RelativePath.Replace('\\', '/').Trim('/');
        TxtPreviewUrl.Text = string.IsNullOrEmpty(dom)
            ? "⚠ Server này chưa nhập Tên miền (mặc định) trong Cài đặt chung"
            : $"URL dự kiến: {scheme}{dom}/{rel}";
    }

    private async void OnDeployClick(object? sender, RoutedEventArgs e)
    {
        if (_site == null || CbServer.SelectedIndex < 0 || CbServer.SelectedIndex >= _servers.Count)
        {
            TxtStatus.Text = "Chưa chọn Server Demo.";
            return;
        }

        var server = _servers[CbServer.SelectedIndex];
        BtnDeploy.IsEnabled = false;
        BtnCleanup.IsEnabled = false;
        BtnOpenDemo.IsVisible = false;
        TxtLog.Text = "";
        TxtStatus.Text = "Đang triển khai...";

        var (ok, demoUrl) = await _deployService.DeployProjectAsync(
            _site,
            server,
            server.Domain ?? "",
            TxtDbSuffix.Text ?? "",
            SwSsl.IsChecked == true,
            SwOnlyDb.IsChecked == true,
            Sw7Zip.IsChecked == true,
            msg => Dispatcher.UIThread.Post(() =>
            {
                TxtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {msg}\r\n";
                TxtLog.CaretIndex = TxtLog.Text.Length;
            }));

        _lastDemoUrl = demoUrl;
        if (ok && !string.IsNullOrEmpty(_lastDemoUrl))
            BtnOpenDemo.IsVisible = true;

        TxtStatus.Text = ok ? "✅ Deploy thành công!" : "❌ Có lỗi xảy ra, xem log.";
        BtnDeploy.IsEnabled = true;
        BtnCleanup.IsEnabled = true;
    }

    private async void OnCleanupClick(object? sender, RoutedEventArgs e)
    {
        if (_site == null || CbServer.SelectedIndex < 0 || CbServer.SelectedIndex >= _servers.Count) return;
        var server = _servers[CbServer.SelectedIndex];
        BtnCleanup.IsEnabled = false;
        bool ok = await _deployService.CleanupRemoteAsync(
            _site, server, server.Domain ?? "", SwSsl.IsChecked == true,
            msg => Dispatcher.UIThread.Post(() =>
            {
                TxtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {msg}\r\n";
                TxtLog.CaretIndex = TxtLog.Text.Length;
            }));
        TxtStatus.Text = ok ? "✅ Đã dọn dẹp file tạm!" : "⚠️ Dọn dẹp hoàn tất (xem log).";
        BtnCleanup.IsEnabled = true;
    }

    private void OnOpenDemoClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastDemoUrl))
        {
            try { Process.Start(new ProcessStartInfo(_lastDemoUrl) { UseShellExecute = true }); } catch { }
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
