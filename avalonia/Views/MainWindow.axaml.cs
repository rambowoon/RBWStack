using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RBWStack.Avalonia.Models;
using RBWStack.Avalonia.ViewModels;
using System;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.Views;

public partial class MainWindow : Window
{
    public bool AllowRealClose { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closing += OnWindowClosing;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        vm.RequestPickFolderAsync = async initialPath =>
        {
            var opts = new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục gốc chứa dự án",
                AllowMultiple = false
            };
            var folders = await StorageProvider.OpenFolderPickerAsync(opts);
            return folders.Count > 0 ? folders[0].Path.LocalPath : null;
        };

        vm.RequestReadClipboardAsync = async () =>
        {
            var cb = TopLevel.GetTopLevel(this)?.Clipboard;
            return cb != null ? await cb.GetTextAsync() : null;
        };

        vm.RequestOpenConfigEditor = (title, filePath) =>
        {
            var win = new ConfigEditorWindow(title, filePath);
            win.ShowDialog(this);
        };

        vm.RequestOpenDeployModal = site =>
        {
            var win = new DeployModalWindow(site, vm.DemoServers, vm.Settings.DbNamingRule);
            win.ShowDialog(this);
        };

        vm.RequestOpenVHostModal = async site =>
        {
            var win = new VHostModalWindow(site);
            var ok = await win.ShowDialog<bool>(this);
            if (ok) vm.SaveSiteVHost(site, win.ResultEnabled, win.ResultDomain);
        };

        vm.RequestShowAlert = async (title, msg) =>
        {
            var dlg = new Window
            {
                Title = title,
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new global::Avalonia.Thickness(20),
                    Spacing = 14,
                    Children =
                    {
                        new TextBlock { Text = title, FontSize = 15, FontWeight = global::Avalonia.Media.FontWeight.Bold },
                        new TextBlock { Text = msg, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap, FontSize = 12.5 }
                    }
                }
            };
            await dlg.ShowDialog(this);
        };
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (e.ClickCount == 2) OnMaximizeClick(sender, e);
            else BeginMoveDrag(e);
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.Settings.MinimizeToTray && !AllowRealClose)
        {
            Hide();
            return;
        }
        Close();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!AllowRealClose && DataContext is MainViewModel vm && vm.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }
}

