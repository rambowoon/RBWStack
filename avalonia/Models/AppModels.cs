using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace RBWStack.Avalonia.Models;

public partial class PhpPoolVersionModel : ObservableObject
{
    public string Version { get; set; } = "";
    [ObservableProperty] private int _port = 9000;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isRunning;
    public string DisplayLabel => $"{Version} (:{Port})";
    public string PortLabel => $"Port {Port}";
    partial void OnPortChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayLabel));
        OnPropertyChanged(nameof(PortLabel));
    }
}

public partial class ServiceCardModel : ObservableObject
{
    public string Key { get; set; } = "";
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _iconText = "";
    [ObservableProperty] private string _iconBgHex = "#EEF2FF";
    [ObservableProperty] private string _iconFgHex = "#4F46E5";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _portText = "Port 80";
    [ObservableProperty] private string _selectedVersion = "";
    [ObservableProperty] private ObservableCollection<string> _versions = new();
    public string ConfigPath { get; set; } = "";

    public string StatusText => IsRunning ? "Đang chạy" : "Đã dừng";
    partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(StatusText));
}

public partial class SiteItemModel : ObservableObject
{
    public string RelativePath { get; set; } = "";
    public string FullPath { get; set; } = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _initials = "PR";
    [ObservableProperty] private string _avatarBgHex = "#EEF2FF";
    [ObservableProperty] private string _avatarFgHex = "#4F46E5";
    [ObservableProperty] private string _displayUrl = "";
    [ObservableProperty] private string _selectedPhpVersion = "";
    [ObservableProperty] private ObservableCollection<string> _availablePhpVersions = new();
    [ObservableProperty] private bool _isSslEnabled;
    [ObservableProperty] private bool _isVHostEnabled;
    [ObservableProperty] private string _vHostDomain = "";
    [ObservableProperty] private bool _isNodeProject;
}

public partial class DownloadCategoryTabModel : ObservableObject
{
    public string Name { get; set; } = "";
    [ObservableProperty] private bool _isSelected;
}

public partial class DownloadItemModel : ObservableObject
{
    public string Category { get; set; } = "PHP";
    public string DisplayName { get; set; } = "";
    public string Url { get; set; } = "";
    public string BaseDestFolder { get; set; } = "";
    public string FolderName { get; set; } = "";
    public string SavePathText => string.IsNullOrEmpty(FolderName)
        ? $"Lưu tại: .\\{BaseDestFolder}"
        : $"Lưu tại: .\\{BaseDestFolder}\\{FolderName}";

    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusLabel = "Sẵn sàng tải";

    public string ActionButtonText => IsDownloading ? $"{Progress:0}%" : (IsInstalled ? "Cài lại" : "Tải & Cài đặt");

    partial void OnIsInstalledChanged(bool value)
    {
        StatusLabel = value ? "Đã cài đặt" : "Sẵn sàng tải";
        OnPropertyChanged(nameof(ActionButtonText));
    }
    partial void OnIsDownloadingChanged(bool value) => OnPropertyChanged(nameof(ActionButtonText));
    partial void OnProgressChanged(double value) => OnPropertyChanged(nameof(ActionButtonText));
}

public partial class DemoServerModel : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    [ObservableProperty] private string _name = "Server Demo";
    [ObservableProperty] private bool _isDefault;
    [ObservableProperty] private string _host = "";
    [ObservableProperty] private string _domain = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _daPort = "1111";
    [ObservableProperty] private string _ftpRoot = "/domains/{domain}/public_html";
    [ObservableProperty] private bool _showPassword;
    [ObservableProperty] private string _testStatus = "";

    [JsonIgnore]
    public char PasswordChar => ShowPassword ? '\0' : '•';

    partial void OnShowPasswordChanged(bool value) => OnPropertyChanged(nameof(PasswordChar));
}

public class CaughtEmailModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Subject { get; set; } = "(Không có tiêu đề)";
    public string Body { get; set; } = "";
    public string Raw { get; set; } = "";

    [JsonIgnore]
    public string HtmlBody
    {
        get
        {
            string src = string.IsNullOrWhiteSpace(Body) ? Raw : Body;
            if (string.IsNullOrWhiteSpace(src))
                return "<html><body style='font-family:Segoe UI,sans-serif;color:#64748b;padding:16px;'>Không có nội dung.</body></html>";

            bool looksLikeHtml = src.Contains("<html", StringComparison.OrdinalIgnoreCase)
                              || src.Contains("<body", StringComparison.OrdinalIgnoreCase)
                              || src.Contains("<div", StringComparison.OrdinalIgnoreCase)
                              || src.Contains("<table", StringComparison.OrdinalIgnoreCase)
                              || src.Contains("<p>", StringComparison.OrdinalIgnoreCase)
                              || src.Contains("<br", StringComparison.OrdinalIgnoreCase);

            string responsiveCss = "<style>html,body{margin:0;padding:12px;background:#ffffff;font-family:'Segoe UI',Arial,sans-serif;box-sizing:border-box;overflow-x:hidden;}table{max-width:100%!important;height:auto!important;}td,th{word-break:break-word;}img{max-width:100%!important;height:auto!important;}</style>";

            if (looksLikeHtml)
            {
                if (src.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                    return System.Text.RegularExpressions.Regex.Replace(src, "</head>", responsiveCss + "</head>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (src.Contains("<body", StringComparison.OrdinalIgnoreCase))
                    return $"<html><head>{responsiveCss}</head>{src}</html>";
                return $"<html><head>{responsiveCss}</head><body>{src}</body></html>";
            }

            string escaped = System.Net.WebUtility.HtmlEncode(src).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
            return $"<html><head>{responsiveCss}<style>body{{font-size:13px;color:#0f172a;line-height:1.6;}}</style></head><body>{escaped}</body></html>";
        }
    }
}

public class AppSettingsModel
{
    public string DefaultWebServer { get; set; } = "Apache HTTP Server";
    public string VHostSuffix { get; set; } = ".local";
    public string WebPort { get; set; } = "80";
    public string MySqlPort { get; set; } = "3306";
    public string PhpFastCgiPort { get; set; } = "9000";
    public string ColorPickerHotkey { get; set; } = "Ctrl+Alt+C";
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool AutoOptimizePhpIni { get; set; } = true;
    public bool AutoHostsPermission { get; set; } = true;
    public string DbNamingRule { get; set; } = "Standard ({user}_{dbname})";
    public bool IsDarkTheme { get; set; } = false;
    public string ActiveApacheVersion { get; set; } = "";
    public string ActiveNginxVersion { get; set; } = "";
    public string ActivePhpVersion { get; set; } = "";
    public List<string> SelectedPhpPools { get; set; } = new();
    public string ActiveMySqlVersion { get; set; } = "";
    public string DocumentRoot { get; set; } = "";
}

public class GithubReleaseModel
{
    public string TagName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Body { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string PublishedAt { get; set; } = "";
}

