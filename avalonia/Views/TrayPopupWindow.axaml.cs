using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
        var screen = Screens.Primary ?? Screens.All[0];
        if (screen != null)
        {
            var wa = screen.WorkingArea;
            int w = (int)(Width * screen.Scaling);
            int h = (int)((Bounds.Height > 0 ? Bounds.Height : 330) * screen.Scaling);
            Position = new PixelPoint(wa.Right - w - 12, wa.Bottom - h - 12);
        }
        Activate();
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

