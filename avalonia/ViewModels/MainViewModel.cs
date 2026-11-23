using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RBWStack.Avalonia.Models;
using RBWStack.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public readonly SettingsService SettingsService = new();
    public readonly ProcessManagerService ProcessService = new();
    public readonly ScraperDownloadService DownloadService;
    public readonly MailSandboxService MailService = new();
    public readonly DeployService DeployService = new();
    public readonly UpdateService UpdateService = new();

    private Dictionary<string, string> _sitePhpMap = new();
    private Dictionary<string, string> _siteSslMap = new();
    private Dictionary<string, string> _siteVHostMap = new();
    private List<SiteItemModel> _allSites = new();
    private List<DownloadItemModel> _allDownloads = new();

    public Action? RequestExitAction { get; set; }
    public Func<string, Task<string?>>? RequestPickFolderAsync { get; set; }
    public Func<Task<string?>>? RequestReadClipboardAsync { get; set; }
    public Action<string, string>? RequestOpenConfigEditor { get; set; }
    public Action<SiteItemModel>? RequestOpenDeployModal { get; set; }
    public Action<SiteItemModel>? RequestOpenVHostModal { get; set; }
    public Action<string, string>? RequestShowAlert { get; set; }

    [ObservableProperty] private string _activeTab = "services";
    [ObservableProperty] private string _topStatusText = "Sẵn sàng";
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private AppSettingsModel _settings = new();

    // Services
    [ObservableProperty] private ServiceCardModel _webService = new();
    [ObservableProperty] private ServiceCardModel _phpService = new();
    [ObservableProperty] private ServiceCardModel _mySqlService = new();
    [ObservableProperty] private ObservableCollection<PhpPoolVersionModel> _phpPools = new();
    [ObservableProperty] private int _runningServicesCount;

    // Sites
    [ObservableProperty] private string _siteSearchText = "";
    [ObservableProperty] private ObservableCollection<SiteItemModel> _filteredSites = new();
    [ObservableProperty] private int _sitesCount;

    // Mail Sandbox
    [ObservableProperty] private ObservableCollection<CaughtEmailModel> _emails = new();
    [ObservableProperty] private CaughtEmailModel? _selectedEmail;
    [ObservableProperty] private bool _isMailRunning;

    // Downloads
    [ObservableProperty] private string _selectedDownloadCategory = "PHP";
    [ObservableProperty] private ObservableCollection<DownloadItemModel> _filteredDownloads = new();
    [ObservableProperty] private bool _isSyncingDownloads;
    [ObservableProperty] private string _downloadSubtitle = "Đang đồng bộ danh sách phiên bản mới nhất từ trang chủ dịch vụ...";

    // Settings & Demo Servers
    [ObservableProperty] private ObservableCollection<DemoServerModel> _demoServers = new();
    public ObservableCollection<string> WebServerOptions { get; } = new() { "Apache HTTP Server", "Nginx Web Server" };
    public ObservableCollection<string> DbNamingRuleOptions { get; } = new()
    {
        "Standard ({user}_{dbname})",
        "Compact ({user}_{short})",
        "Custom (Giữ nguyên tên nhập)"
    };
    public ObservableCollection<DownloadCategoryTabModel> DownloadCategories { get; } = new()
    {
        new() { Name = "PHP", IsSelected = true },
        new() { Name = "Node.js" },
        new() { Name = "Apache" },
        new() { Name = "Nginx" },
        new() { Name = "Database" },
        new() { Name = "phpMyAdmin" },
        new() { Name = "Tiện ích & Runtime" }
    };

    // Update
    [ObservableProperty] private string _currentVersion = "v" + UpdateService.CurrentVersion;
    [ObservableProperty] private string _latestVersionText = "Chưa kiểm tra";
    [ObservableProperty] private string _releaseNotesText = "Nhấn 'Kiểm tra cập nhật' để đồng bộ thông tin phiên bản mới nhất từ GitHub.";
    [ObservableProperty] private GithubReleaseModel? _latestRelease;
    [ObservableProperty] private bool _hasUpdateAvailable;
    [ObservableProperty] private bool _isCheckingUpdate;

    // Computed tab booleans
    public bool IsServicesTab => ActiveTab == "services";
    public bool IsSitesTab => ActiveTab == "sites";
    public bool IsMailTab => ActiveTab == "mail";
    public bool IsDownloadsTab => ActiveTab == "downloads";
    public bool IsSettingsTab => ActiveTab == "settings";
    public bool IsProTab => ActiveTab == "pro";
    public bool IsUpdateTab => ActiveTab == "update";
    public string RunningSummaryText => $"{RunningServicesCount}/3";
    public bool HasAnyRunningService => RunningServicesCount > 0;
    public string TrayPhpLabel
    {
        get
        {
            var active = PhpPools.Where(p => p.IsSelected).Select(p => p.Version.Replace("php-", "", StringComparison.OrdinalIgnoreCase)).ToList();
            if (active.Count > 0) return "PHP " + string.Join(", ", active);
            string v = (PhpService.SelectedVersion ?? "").Replace("php-", "", StringComparison.OrdinalIgnoreCase);
            return string.IsNullOrEmpty(v) ? "PHP FastCGI" : $"PHP {v}";
        }
    }

    private static readonly (string Bg, string Fg)[] AvatarPalette =
    {
        ("#EEF2FF", "#4F46E5"), ("#F3E8FF", "#7C3AED"), ("#E0F2FE", "#0284C7"),
        ("#DCFCE7", "#15803D"), ("#FEF3C7", "#B45309"), ("#FFE4E6", "#BE123C")
    };

    public MainViewModel()
    {
        DownloadService = new ScraperDownloadService(ProcessService);
        InitializeData();
    }

    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsServicesTab));
        OnPropertyChanged(nameof(IsSitesTab));
        OnPropertyChanged(nameof(IsMailTab));
        OnPropertyChanged(nameof(IsDownloadsTab));
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsProTab));
        OnPropertyChanged(nameof(IsUpdateTab));
    }

    partial void OnRunningServicesCountChanged(int value)
    {
        OnPropertyChanged(nameof(RunningSummaryText));
        OnPropertyChanged(nameof(HasAnyRunningService));
        OnPropertyChanged(nameof(TrayPhpLabel));
    }
    partial void OnSiteSearchTextChanged(string value) => ApplySitesFilter();
    partial void OnSelectedDownloadCategoryChanged(string value) => ApplyDownloadsFilter();

    private void InitializeData()
    {
        Settings = SettingsService.LoadSettings();
        IsDarkTheme = Settings.IsDarkTheme;
        ApplyTheme(IsDarkTheme);

        _sitePhpMap = SettingsService.LoadJsonDict("sites.json");
        _siteSslMap = SettingsService.LoadJsonDict("ssl.json");
        _siteVHostMap = SettingsService.LoadJsonDict("sites_vhosts.json");

        DemoServers = new ObservableCollection<DemoServerModel>(SettingsService.LoadDemoServers());

        SetupServiceCards();
        RefreshServiceStates();
        LoadSites();

        // Mail Sandbox
        Emails = new ObservableCollection<CaughtEmailModel>(MailService.LoadEmails());
        SelectedEmail = Emails.FirstOrDefault();
        MailService.EmailReceived += mail =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Emails.Insert(0, mail);
                SelectedEmail ??= mail;
                MailService.SaveEmails(Emails.ToList());
            });
        };
        MailService.Start(2525);
        IsMailRunning = MailService.IsRunning;

        _allDownloads = DownloadService.GetLocalInstalledCatalog();
        ApplyDownloadsFilter();

        // Background sync catalog
        _ = SyncDownloadsAsync();
    }

    private void SetupServiceCards()
    {
        bool isNginx = Settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        var webVers = ProcessService.GetInstalledVersions(isNginx ? "nginx" : "apache");
        var phpVers = ProcessService.GetInstalledVersions("php");
        var dbVers = ProcessService.GetInstalledVersions("mysql");

        string activeWeb = !string.IsNullOrEmpty(isNginx ? Settings.ActiveNginxVersion : Settings.ActiveApacheVersion)
            ? (isNginx ? Settings.ActiveNginxVersion : Settings.ActiveApacheVersion)
            : (webVers.FirstOrDefault() ?? "Chưa cài đặt");
        if (webVers.Count > 0 && !webVers.Contains(activeWeb)) activeWeb = webVers[0];

        string activePhp = !string.IsNullOrEmpty(Settings.ActivePhpVersion) && phpVers.Contains(Settings.ActivePhpVersion)
            ? Settings.ActivePhpVersion
            : (phpVers.FirstOrDefault() ?? "Chưa cài đặt");

        string activeDb = !string.IsNullOrEmpty(Settings.ActiveMySqlVersion) && dbVers.Contains(Settings.ActiveMySqlVersion)
            ? Settings.ActiveMySqlVersion
            : (dbVers.FirstOrDefault() ?? "Chưa cài đặt");

        WebService = new ServiceCardModel
        {
            Key = "web",
            Title = isNginx ? "Nginx Server" : "Apache Server",
            IconText = "🌐",
            IconBgHex = "#EEF2FF",
            IconFgHex = "#4F46E5",
            PortText = $"Port {Settings.WebPort}",
            Versions = new ObservableCollection<string>(webVers.Count > 0 ? webVers : new List<string> { "Chưa cài đặt" }),
            SelectedVersion = activeWeb,
            ConfigPath = ProcessService.GetConfigPath("web", activeWeb, isNginx)
        };

        PhpService = new ServiceCardModel
        {
            Key = "php",
            Title = "PHP FastCGI",
            IconText = "</>",
            IconBgHex = "#F3E8FF",
            IconFgHex = "#7C3AED",
            PortText = $"Port {Settings.PhpFastCgiPort}",
            Versions = new ObservableCollection<string>(phpVers.Count > 0 ? phpVers : new List<string> { "Chưa cài đặt" }),
            SelectedVersion = activePhp,
            ConfigPath = ProcessService.GetConfigPath("php", activePhp, false)
        };

        if (activePhp != "Chưa cài đặt" && Settings.ActivePhpVersion != activePhp)
        {
            Settings.ActivePhpVersion = activePhp;
            SettingsService.SaveSettings(Settings);
        }

        PhpService.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(ServiceCardModel.SelectedVersion) && !string.IsNullOrEmpty(PhpService.SelectedVersion) && PhpService.SelectedVersion != "Chưa cài đặt")
            {
                Settings.ActivePhpVersion = PhpService.SelectedVersion;
                PhpService.ConfigPath = ProcessService.GetConfigPath("php", PhpService.SelectedVersion, false);
                SettingsService.SaveSettings(Settings);

                int basePortLocal = int.TryParse(Settings.PhpFastCgiPort, out int bpLocal) ? bpLocal : 9000;
                foreach (var p in PhpPools)
                    p.Port = ProcessManagerService.GetPhpPortForVersion(p.Version, Settings.ActivePhpVersion, basePortLocal);

                if (PhpService.IsRunning)
                    await ToggleServiceAsync(PhpService, true);
                await ProcessService.ReloadOrRestartWebServerAsync(Settings, WebService.SelectedVersion, PhpService.SelectedVersion, _sitePhpMap, _siteVHostMap, _allSites);
            }
        };

        MySqlService = new ServiceCardModel
        {
            Key = "mysql",
            Title = "MySQL / MariaDB",
            IconText = "🗄",
            IconBgHex = "#FEF3C7",
            IconFgHex = "#D97706",
            PortText = $"Port {Settings.MySqlPort}",
            Versions = new ObservableCollection<string>(dbVers.Count > 0 ? dbVers : new List<string> { "Chưa cài đặt" }),
            SelectedVersion = activeDb,
            ConfigPath = ProcessService.GetConfigPath("mysql", activeDb, false)
        };

        // Populate selectable multi-PHP FastCGI pools with deterministic ports matching Apache/Nginx routing
        int basePort = int.TryParse(Settings.PhpFastCgiPort, out int bp) ? bp : 9000;
        var savedPools = Settings.SelectedPhpPools ?? new List<string>();
        var poolList = new ObservableCollection<PhpPoolVersionModel>();
        foreach (string ver in phpVers)
        {
            bool isSel = savedPools.Count > 0
                ? savedPools.Contains(ver, StringComparer.OrdinalIgnoreCase)
                : ver.Equals(activePhp, StringComparison.OrdinalIgnoreCase);

            var pool = new PhpPoolVersionModel
            {
                Version = ver,
                Port = ProcessManagerService.GetPhpPortForVersion(ver, activePhp, basePort),
                IsSelected = isSel
            };
            pool.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PhpPoolVersionModel.IsSelected))
                {
                    Settings.SelectedPhpPools = PhpPools.Where(p => p.IsSelected).Select(p => p.Version).ToList();
                    SettingsService.SaveSettings(Settings);
                    OnPropertyChanged(nameof(TrayPhpLabel));
                }
            };
            poolList.Add(pool);
        }
        PhpPools = poolList;
    }

    [RelayCommand]
    private void Navigate(string tab) => ActiveTab = tab;

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        Settings.IsDarkTheme = IsDarkTheme;
        ApplyTheme(IsDarkTheme);
        SettingsService.SaveSettings(Settings);
    }

    private static void ApplyTheme(bool dark)
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    [RelayCommand]
    private void RefreshServices()
    {
        SetupServiceCards();
        RefreshServiceStates();
        TopStatusText = "Đã làm mới trạng thái dịch vụ";
    }

    private void RefreshServiceStates()
    {
        bool isNginx = Settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        WebService.IsRunning = ProcessService.IsProcessRunning(isNginx ? "nginx" : "httpd");
        PhpService.IsRunning = ProcessService.IsProcessRunning("php-cgi");
        MySqlService.IsRunning = ProcessService.IsProcessRunning("mysqld") || ProcessService.IsProcessRunning("mariadbd");
        RunningServicesCount = (WebService.IsRunning ? 1 : 0) + (PhpService.IsRunning ? 1 : 0) + (MySqlService.IsRunning ? 1 : 0);
    }

    public async Task ToggleServiceAsync(ServiceCardModel svc, bool targetState)
    {
        bool isNginx = Settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        if (svc.Key == "web")
        {
            if (targetState)
            {
                var res = await ProcessService.StartWebServerAsync(Settings, svc.SelectedVersion, PhpService.SelectedVersion, _sitePhpMap, _siteVHostMap, _allSites);
                TopStatusText = res.Message;
            }
            else
            {
                ProcessService.StopWebServer(isNginx);
                TopStatusText = $"Đã dừng {svc.Title}";
            }
        }
        else if (svc.Key == "php")
        {
            if (targetState)
            {
                if (!PhpPools.Any(p => p.IsSelected) && PhpPools.Count > 0)
                {
                    var match = PhpPools.FirstOrDefault(p => p.Version == svc.SelectedVersion) ?? PhpPools[0];
                    match.IsSelected = true;
                }
                var res = await ProcessService.StartPhpFastCgiPoolsAsync(Settings, PhpPools);
                TopStatusText = res.Message;
            }
            else
            {
                ProcessService.StopPhpFastCgi();
                foreach (var p in PhpPools) p.IsRunning = false;
                TopStatusText = "Đã dừng tất cả phiên bản PHP FastCGI";
            }
        }
        else if (svc.Key == "mysql")
        {
            if (targetState)
            {
                var res = await ProcessService.StartMySqlAsync(Settings, svc.SelectedVersion);
                TopStatusText = res.Message;
            }
            else
            {
                ProcessService.StopMySql();
                TopStatusText = "Đã dừng MySQL / MariaDB";
            }
        }
        RefreshServiceStates();
    }

    [RelayCommand]
    private async Task StartAllAsync()
    {
        TopStatusText = "Đang khởi động toàn bộ dịch vụ...";
        await ToggleServiceAsync(PhpService, true);
        await ToggleServiceAsync(MySqlService, true);
        await ToggleServiceAsync(WebService, true);
        RefreshServiceStates();
        TopStatusText = RunningServicesCount == 3 ? "Tất cả dịch vụ đang chạy" : $"Đang chạy {RunningServicesCount}/3 dịch vụ";
    }

    [RelayCommand]
    private void StopAll()
    {
        bool isNginx = Settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        ProcessService.StopWebServer(isNginx);
        ProcessService.StopPhpFastCgi();
        foreach (var p in PhpPools) p.IsRunning = false;
        ProcessService.StopMySql();
        RefreshServiceStates();
        TopStatusText = "Đã dừng toàn bộ dịch vụ";
    }

    [RelayCommand]
    private void OpenPhpMyAdmin() => OpenUrl($"http://localhost:{Settings.WebPort}/phpmyadmin");

    [RelayCommand]
    private void OpenLocalhost() => OpenUrl(Settings.WebPort == "80" ? "http://localhost" : $"http://localhost:{Settings.WebPort}");

    [RelayCommand]
    private void OpenDocumentRoot()
    {
        if (Directory.Exists(Settings.DocumentRoot))
            Process.Start(new ProcessStartInfo("explorer.exe", Settings.DocumentRoot) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenServiceConfig(ServiceCardModel? svc)
    {
        if (svc == null) return;
        bool isNginx = Settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        string path = ProcessService.GetConfigPath(svc.Key, svc.SelectedVersion, isNginx);
        if (File.Exists(path))
            RequestOpenConfigEditor?.Invoke($"{svc.Title} — {Path.GetFileName(path)}", path);
        else
            RequestShowAlert?.Invoke("Không tìm thấy file cấu hình", $"File không tồn tại:\n{path}");
    }

    [RelayCommand]
    private void ExitApp() => RequestExitAction?.Invoke();

    // ─── SITES ─────────────────────────────────────────────────────────────────
    [RelayCommand]
    private void LoadSites()
    {
        _allSites.Clear();
        string root = Settings.DocumentRoot;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) root = PathHelper.WwwDir;
        if (!Directory.Exists(root)) { ApplySitesFilter(); return; }

        var phpVers = ProcessService.GetInstalledVersions("php");
        if (phpVers.Count == 0) phpVers.Add("php-8.3.36");
        string defaultPhp = PhpService.SelectedVersion;
        if (string.IsNullOrEmpty(defaultPhp) || !phpVers.Contains(defaultPhp)) defaultPhp = phpVers[0];

        int idx = 0;
        foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(dir);
            if (name.StartsWith(".") || name.Equals("phpmyadmin", StringComparison.OrdinalIgnoreCase)) continue;

            string rel = Path.GetRelativePath(PathHelper.WwwDir, dir).Replace('\\', '/');
            if (rel.StartsWith("..")) rel = name;

            string initials = name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
            var (bg, fg) = AvatarPalette[idx++ % AvatarPalette.Length];

            string sitePhp = _sitePhpMap.TryGetValue(rel, out var mappedPhp) && phpVers.Contains(mappedPhp)
                ? mappedPhp
                : defaultPhp;

            bool ssl = _siteSslMap.TryGetValue(rel, out var sslVal) && sslVal == "1";
            bool vhostOn = false;
            string vhostDom = name + Settings.VHostSuffix;
            if (_siteVHostMap.TryGetValue(rel, out var vhVal))
            {
                var parts = vhVal.Split('|');
                if (parts.Length >= 2)
                {
                    vhostOn = parts[0] == "1";
                    vhostDom = parts[1];
                }
            }

            string scheme = ssl ? "https" : "http";
            string url = vhostOn ? $"{scheme}://{vhostDom}" : $"{scheme}://localhost/{rel}";

            var site = new SiteItemModel
            {
                RelativePath = rel,
                FullPath = dir,
                Name = name,
                Initials = initials,
                AvatarBgHex = bg,
                AvatarFgHex = fg,
                DisplayUrl = url,
                SelectedPhpVersion = sitePhp,
                AvailablePhpVersions = new ObservableCollection<string>(phpVers),
                IsSslEnabled = ssl,
                IsVHostEnabled = vhostOn,
                VHostDomain = vhostDom,
                IsNodeProject = File.Exists(Path.Combine(dir, "package.json")) && !File.Exists(Path.Combine(dir, "composer.json"))
            };

            site.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SiteItemModel.SelectedPhpVersion) && s is SiteItemModel item)
                    _ = ApplySitePhpVersionSwitchAsync(item);
            };

            _allSites.Add(site);
        }

        SitesCount = _allSites.Count;
        ApplySitesFilter();
    }

    private async Task ApplySitePhpVersionSwitchAsync(SiteItemModel item)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(item.SelectedPhpVersion) || item.SelectedPhpVersion == "Chưa cài đặt")
                return;

            _sitePhpMap[item.RelativePath] = item.SelectedPhpVersion;
            SettingsService.SaveJsonDict("sites.json", _sitePhpMap);
            TopStatusText = $"Đang chuyển {item.Name} sang {item.SelectedPhpVersion}...";

            var pool = PhpPools.FirstOrDefault(p => p.Version.Equals(item.SelectedPhpVersion, StringComparison.OrdinalIgnoreCase));
            int port = pool?.Port ?? ProcessManagerService.GetPhpPortForVersion(item.SelectedPhpVersion, PhpService.SelectedVersion);

            if (pool != null)
            {
                if (!pool.IsSelected)
                {
                    pool.IsSelected = true;
                    Settings.SelectedPhpPools = PhpPools.Where(p => p.IsSelected).Select(p => p.Version).ToList();
                    SettingsService.SaveSettings(Settings);
                    OnPropertyChanged(nameof(TrayPhpLabel));
                }

                // Ensure the chosen PHP version FastCGI pool is actually running so Apache/Nginx never returns 503
                await ProcessService.EnsurePhpPoolRunningAsync(Settings, pool);
            }
            else
            {
                await ProcessService.EnsurePhpPoolRunningAsync(Settings, new PhpPoolVersionModel
                {
                    Version = item.SelectedPhpVersion,
                    Port = port,
                    IsSelected = true
                });
            }

            await ProcessService.ReloadOrRestartWebServerAsync(Settings, WebService.SelectedVersion, PhpService.SelectedVersion, _sitePhpMap, _siteVHostMap, _allSites);
            RefreshServiceStates();
            TopStatusText = $"Đã chuyển {item.Name} sang {item.SelectedPhpVersion} (Port {port})";
        }
        catch (Exception ex)
        {
            TopStatusText = $"Lỗi chuyển PHP: {ex.Message}";
        }
    }

    private void ApplySitesFilter()
    {
        string q = SiteSearchText?.Trim() ?? "";
        var list = string.IsNullOrEmpty(q)
            ? _allSites
            : _allSites.Where(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || s.DisplayUrl.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        FilteredSites = new ObservableCollection<SiteItemModel>(list);
    }

    [RelayCommand]
    private async Task ChangeDocumentRootAsync()
    {
        if (RequestPickFolderAsync == null) return;
        string? picked = await RequestPickFolderAsync(Settings.DocumentRoot);
        if (!string.IsNullOrEmpty(picked) && Directory.Exists(picked))
        {
            Settings.DocumentRoot = picked;
            SettingsService.SaveSettings(Settings);
            OnPropertyChanged(nameof(Settings));
            LoadSites();
            TopStatusText = $"Đã đổi thư mục gốc: {picked}";
        }
    }

    [RelayCommand]
    private void OpenSiteUrl(SiteItemModel? site)
    {
        if (site != null) OpenUrl(site.DisplayUrl);
    }

    [RelayCommand]
    private void ToggleSiteSsl(SiteItemModel? site)
    {
        if (site == null) return;
        site.IsSslEnabled = !site.IsSslEnabled;
        _siteSslMap[site.RelativePath] = site.IsSslEnabled ? "1" : "0";
        SettingsService.SaveJsonDict("ssl.json", _siteSslMap);
        string scheme = site.IsSslEnabled ? "https" : "http";
        site.DisplayUrl = site.IsVHostEnabled ? $"{scheme}://{site.VHostDomain}" : $"{scheme}://localhost/{site.RelativePath}";
        TopStatusText = $"{(site.IsSslEnabled ? "Đã bật" : "Đã tắt")} SSL cho {site.Name}";
    }

    [RelayCommand]
    private void OpenSiteVHost(SiteItemModel? site)
    {
        if (site != null) RequestOpenVHostModal?.Invoke(site);
    }

    public void SaveSiteVHost(SiteItemModel site, bool enabled, string domain)
    {
        site.IsVHostEnabled = enabled;
        site.VHostDomain = domain;
        _siteVHostMap[site.RelativePath] = $"{(enabled ? "1" : "0")}|{domain}|1";
        SettingsService.SaveJsonDict("sites_vhosts.json", _siteVHostMap);
        if (Settings.AutoHostsPermission)
            ProcessManagerService.UpdateHostsEntry(domain, enabled);
        string scheme = site.IsSslEnabled ? "https" : "http";
        site.DisplayUrl = enabled ? $"{scheme}://{domain}" : $"{scheme}://localhost/{site.RelativePath}";
        TopStatusText = $"Đã lưu tên miền ảo {domain}";
    }

    [RelayCommand]
    private void OpenSiteDeploy(SiteItemModel? site)
    {
        if (site == null) return;
        SettingsService.SaveDemoServers(DemoServers);
        RequestOpenDeployModal?.Invoke(site);
    }

    [RelayCommand]
    private void OpenSiteInCode(SiteItemModel? site)
    {
        if (site == null) return;
        try { Process.Start(new ProcessStartInfo("code", $"\"{site.FullPath}\"") { UseShellExecute = true, CreateNoWindow = true }); }
        catch { OpenSiteFolder(site); }
    }

    [RelayCommand]
    private void OpenSiteFolder(SiteItemModel? site)
    {
        if (site != null && Directory.Exists(site.FullPath))
            Process.Start(new ProcessStartInfo("explorer.exe", site.FullPath) { UseShellExecute = true });
    }

    // ─── MAIL SANDBOX ──────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleMailServer()
    {
        if (MailService.IsRunning) MailService.Stop();
        else MailService.Start(2525);
        IsMailRunning = MailService.IsRunning;
    }

    [RelayCommand]
    private void ClearEmails()
    {
        Emails.Clear();
        SelectedEmail = null;
        MailService.SaveEmails(new List<CaughtEmailModel>());
    }

    // ─── DOWNLOAD CENTER ───────────────────────────────────────────────────────
    [RelayCommand]
    private void SelectDownloadCategory(string cat)
    {
        SelectedDownloadCategory = cat;
        foreach (var tab in DownloadCategories)
            tab.IsSelected = (tab.Name == cat);
    }

    [RelayCommand]
    private async Task SyncDownloadsAsync()
    {
        IsSyncingDownloads = true;
        DownloadSubtitle = "Đang quét danh sách phiên bản mới nhất từ trang chủ...";
        _allDownloads = await DownloadService.FetchAllCatalogAsync();
        ApplyDownloadsFilter();
        IsSyncingDownloads = false;
        DownloadSubtitle = "Đã đồng bộ trực tiếp các phiên bản mới nhất từ trang chủ dịch vụ";
        TopStatusText = "Đã đồng bộ phiên bản mới nhất";
    }

    private void ApplyDownloadsFilter()
    {
        var items = _allDownloads.Where(x => x.Category == SelectedDownloadCategory).ToList();
        FilteredDownloads = new ObservableCollection<DownloadItemModel>(items);
    }

    [RelayCommand]
    private async Task InstallDownloadAsync(DownloadItemModel? item)
    {
        if (item == null || item.IsDownloading) return;
        TopStatusText = $"Đang tải {item.DisplayName}...";
        var res = await DownloadService.DownloadAndInstallAsync(item);
        TopStatusText = res.Message;
        if (res.Ok)
        {
            SetupServiceCards();
            RefreshServiceStates();
        }
    }

    // ─── SETTINGS & DEMO SERVERS ───────────────────────────────────────────────
    [RelayCommand]
    private void AddDemoServer()
    {
        DemoServers.Add(new DemoServerModel
        {
            Name = $"Server Demo {DemoServers.Count + 1}",
            IsDefault = DemoServers.Count == 0,
            DaPort = "1111",
            FtpRoot = "/domains/{domain}/public_html"
        });
    }

    [RelayCommand]
    private void SetDefaultDemoServer(DemoServerModel? server)
    {
        if (server == null) return;
        foreach (var s in DemoServers) s.IsDefault = (s == server);
        SettingsService.SaveDemoServers(DemoServers);
        TopStatusText = $"Đã đặt '{server.Name}' làm Server Demo mặc định";
    }

    [RelayCommand]
    private void DeleteDemoServer(DemoServerModel? server)
    {
        if (server == null) return;
        DemoServers.Remove(server);
        if (DemoServers.Count > 0 && !DemoServers.Any(s => s.IsDefault))
            DemoServers[0].IsDefault = true;
        SettingsService.SaveDemoServers(DemoServers);
    }

    [RelayCommand]
    private async Task QuickPasteDemoServerAsync(DemoServerModel? server)
    {
        if (server == null || RequestReadClipboardAsync == null) return;
        string? text = await RequestReadClipboardAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            DeployService.ParseQuickPaste(text, server);
            TopStatusText = $"Đã dán nhanh thông tin cho {server.Name}";
        }
    }

    [RelayCommand]
    private async Task TestDemoServerAsync(DemoServerModel? server)
    {
        if (server == null) return;
        server.TestStatus = "Đang kiểm tra...";
        var res = await DeployService.TestConnectionAsync(server);
        server.TestStatus = res.Ok ? "✅ " + res.Message : "❌ " + res.Message;
        TopStatusText = server.TestStatus;
    }

    [RelayCommand]
    private void SaveAllSettings()
    {
        Settings.ActivePhpVersion = PhpService.SelectedVersion;
        Settings.ActiveMySqlVersion = MySqlService.SelectedVersion;
        if (Settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase))
            Settings.ActiveNginxVersion = WebService.SelectedVersion;
        else
            Settings.ActiveApacheVersion = WebService.SelectedVersion;

        SettingsService.SaveSettings(Settings);
        SettingsService.SaveDemoServers(DemoServers);
        SetupServiceCards();
        RefreshServiceStates();
        TopStatusText = "Đã lưu toàn bộ cài đặt thành công!";
    }

    [RelayCommand]
    private void OpenHostsFile()
    {
        string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
        RequestOpenConfigEditor?.Invoke("Windows Hosts File", hostsPath);
    }

    // ─── UPDATE ────────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdate = true;
        LatestVersionText = "Đang kiểm tra...";
        var rel = await UpdateService.CheckLatestReleaseAsync();
        IsCheckingUpdate = false;
        if (rel == null)
        {
            LatestVersionText = "Không thể kết nối GitHub";
            return;
        }
        LatestRelease = rel;
        LatestVersionText = rel.TagName;
        ReleaseNotesText = string.IsNullOrWhiteSpace(rel.Body) ? "Không có ghi chú phát hành." : rel.Body;
        HasUpdateAvailable = !rel.TagName.TrimStart('v', 'V').Equals(UpdateService.CurrentVersion, StringComparison.OrdinalIgnoreCase);
        TopStatusText = HasUpdateAvailable ? $"Có phiên bản mới: {rel.TagName}" : $"Đang dùng bản mới nhất ({CurrentVersion})";
    }

    [RelayCommand]
    private void OpenLatestReleaseUrl()
    {
        string url = LatestRelease?.HtmlUrl ?? "https://github.com/rambowoon/RBWStack/releases";
        OpenUrl(url);
    }

    [RelayCommand]
    private async Task SelfUpdateAsync()
    {
        if (LatestRelease == null) return;
        string dlUrl = !string.IsNullOrEmpty(LatestRelease.DownloadUrl)
            ? LatestRelease.DownloadUrl
            : "https://github.com/rambowoon/RBWStack/raw/main/RBWStack.exe";

        try
        {
            TopStatusText = $"Đang tải bản cập nhật {LatestRelease.TagName}...";
            string currentExe = Environment.ProcessPath ?? Path.Combine(PathHelper.RootDir, "RBWStack.exe");
            string newExe = currentExe + ".new";
            string batPath = Path.Combine(PathHelper.TmpDir, "self_update.bat");

            using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("RBWStack-Updater");
                var bytes = await http.GetByteArrayAsync(dlUrl);
                await File.WriteAllBytesAsync(newExe, bytes);
            }

            string bat = $@"@echo off
timeout /t 2 /nobreak >nul
:retry
move /Y ""{newExe}"" ""{currentExe}"" >nul 2>&1
if errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto retry
)
start """" ""{currentExe}""
del ""%~f0""
";
            await File.WriteAllTextAsync(batPath, bat);
            Process.Start(new ProcessStartInfo(batPath) { CreateNoWindow = true, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
            RequestExitAction?.Invoke();
        }
        catch (Exception ex)
        {
            TopStatusText = $"Lỗi cập nhật: {ex.Message}";
        }
    }

    public void CleanupOnExit()
    {
        try { StopAll(); } catch { }
        try { MailService.Stop(); } catch { }
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}

