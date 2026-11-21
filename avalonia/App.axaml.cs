using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RBWStack.Avalonia.Services;
using RBWStack.Avalonia.ViewModels;
using RBWStack.Avalonia.Views;
using System;
using System.IO;

namespace RBWStack.Avalonia;

public partial class App : Application
{
    private Win32TrayService? _trayService;
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
                _trayService?.Dispose();
                desktop.Shutdown();
                Environment.Exit(0);
            };

            desktop.Exit += (_, _) =>
            {
                _mainViewModel?.CleanupOnExit();
                _trayService?.Dispose();
            };

            desktop.MainWindow = _mainWindow;
            SetupTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon()
    {
        try
        {
            _trayPopup = new TrayPopupWindow
            {
                DataContext = _mainViewModel,
                RequestShowMain = ShowMainWindow
            };

            string icoPath = Path.Combine(PathHelper.RootDir, "app.ico");
            if (!File.Exists(icoPath))
                icoPath = Path.Combine(AppContext.BaseDirectory, "app.ico");

            _trayService = new Win32TrayService("RBWStack v4.0.0", icoPath);
            _trayService.LeftClicked += () => Dispatcher.UIThread.Post(ShowMainWindow);
            _trayService.RightClicked += () => Dispatcher.UIThread.Post(() =>
            {
                if (_trayPopup.IsVisible) _trayPopup.Hide();
                else _trayPopup.ShowAtBottomRight();
            });
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
