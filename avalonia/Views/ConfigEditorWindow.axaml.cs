using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;

namespace RBWStack.Avalonia.Views;

public partial class ConfigEditorWindow : Window
{
    private readonly string _filePath;

    public ConfigEditorWindow() : this("Cấu hình", "") { }

    public ConfigEditorWindow(string title, string filePath)
    {
        InitializeComponent();
        _filePath = filePath;
        Title = title;
        TxtTitle.Text = title;
        TxtPath.Text = filePath;
        if (File.Exists(filePath))
            TxtContent.Text = File.ReadAllText(filePath);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_filePath))
                File.WriteAllText(_filePath, TxtContent.Text ?? "");
            Close(true);
        }
        catch { }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);
}

