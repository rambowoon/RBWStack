using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RBWStack.Avalonia.ViewModels;
using System;

namespace RBWStack.Avalonia.Views;

public partial class TrayPopupWindow : Window
{
    public Action? RequestShowMain { get; set; }

    public TrayPopupWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => Hide();
    }

    public void ShowAtBottomRight()
    {
        Show();
        RepositionBottomRight();
        Dispatcher.UIThread.Post(RepositionBottomRight, DispatcherPriority.Loaded);
        Activate();
    }

    private void RepositionBottomRight()
    {
        var screen = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (screen == null) return;
        var wa = screen.WorkingArea;
        int w = (int)(Width * screen.Scaling);
        double logicalH = Bounds.Height > 0 ? Bounds.Height : 340;
        int h = (int)(logicalH * screen.Scaling);
        Position = new PixelPoint(wa.Right - w - 12, wa.Bottom - h - 12);
    }

    private async void OnToggleWebClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.ToggleServiceAsync(vm.WebService, !vm.WebService.IsRunning);
    }

    private async void OnTogglePhpClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.ToggleServiceAsync(vm.PhpService, !vm.PhpService.IsRunning);
    }

    private async void OnToggleMySqlClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.ToggleServiceAsync(vm.MySqlService, !vm.MySqlService.IsRunning);
    }

    private void OnOpenMainClick(object? sender, RoutedEventArgs e)
    {
        Hide();
        RequestShowMain?.Invoke();
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Hide();
        if (DataContext is MainViewModel vm)
            vm.RequestExitAction?.Invoke();
    }
}
