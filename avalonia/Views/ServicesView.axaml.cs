using Avalonia.Controls;
using Avalonia.Interactivity;
using RBWStack.Avalonia.ViewModels;

namespace RBWStack.Avalonia.Views;

public partial class ServicesView : UserControl
{
    public ServicesView() => InitializeComponent();

    private async void OnWebToggleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ToggleSwitch sw)
            await vm.ToggleServiceAsync(vm.WebService, sw.IsChecked == true);
    }

    private async void OnPhpToggleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ToggleSwitch sw)
            await vm.ToggleServiceAsync(vm.PhpService, sw.IsChecked == true);
    }

    private async void OnMySqlToggleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ToggleSwitch sw)
            await vm.ToggleServiceAsync(vm.MySqlService, sw.IsChecked == true);
    }
}

