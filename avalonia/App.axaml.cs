using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using RBWStack.Avalonia.ViewModels;
using RBWStack.Avalonia.Views;
using System;

namespace RBWStack.Avalonia;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;
    private TrayPopupWindow? _trayPopup;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainViewModel = new MainViewModel();
            _mainWindow = new MainWindow
            {
                DataContext = _mainViewModel
            };

            _mainViewModel.RequestExitAction = () =>
            {
                _mainWindow.AllowRealClose = true;
                _trayPopup?.Close();
                _mainViewModel.CleanupOnExit();
                _trayIcon?.Dispose();
                desktop.Shutdown();
                Environment.Exit(0);
            };

            desktop.Exit += (_, _) =>
            {
                _mainViewModel?.CleanupOnExit();
                _trayIcon?.Dispose();
            };

            desktop.MainWindow = _mainWindow;
            SetupTrayIcon(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var menu = new NativeMenu();

            var openItem = new NativeMenuItem("Mở RBWStack");
            openItem.Click += (_, _) => ShowMainWindow();
            menu.Add(openItem);

            menu.Add(new NativeMenuItemSeparator());

            var startAllItem = new NativeMenuItem("Start All Services");
            startAllItem.Click += (_, _) => _mainViewModel?.StartAllCommand.Execute(null);
            menu.Add(startAllItem);

            var stopAllItem = new NativeMenuItem("Stop All Services");
            stopAllItem.Click += (_, _) => _mainViewModel?.StopAllCommand.Execute(null);
            menu.Add(stopAllItem);

            menu.Add(new NativeMenuItemSeparator());

            var exitItem = new NativeMenuItem("Thoát ứng dụng");
            exitItem.Click += (_, _) => _mainViewModel?.RequestExitAction?.Invoke();
            menu.Add(exitItem);

            WindowIcon? icon = null;
            try
            {
                using var stream = AssetLoader.Open(new Uri("avares://RBWStack/Assets/app.ico"));
                icon = new WindowIcon(stream);
            }
            catch { }

            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "RBWStack v4.0.0",
                Menu = menu,
                IsVisible = true
            };
            _trayPopup = new TrayPopupWindow
            {
                DataContext = _mainViewModel,
                RequestShowMain = ShowMainWindow
            };
            _trayIcon.Clicked += (_, _) =>
            {
                if (_trayPopup.IsVisible) _trayPopup.Hide();
                else _trayPopup.ShowAtBottomRight();
            };
        }
        catch { }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }
}

