using RBWStack.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.Services;

public class ScraperDownloadService
{
    private readonly HttpClient _http;
    private readonly ProcessManagerService _procService;

    public ScraperDownloadService(ProcessManagerService procService)
    {
        _procService = procService;
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0.0.0 Safari/537.36");
    }

    public async Task<List<DownloadItemModel>> FetchAllCatalogAsync()
    {
        var all = new List<DownloadItemModel>();

        var tPhp = ScrapePhpAsync();
        var tNode = ScrapeNodeAsync();
        var tApache = ScrapeApacheAsync();
        var tNginx = ScrapeNginxAsync();
        var tDb = ScrapeDatabaseAsync();
        var tPma = ScrapePhpMyAdminAsync();

        await Task.WhenAll(tPhp, tNode, tApache, tNginx, tDb, tPma);

        all.AddRange(tPhp.Result);
        all.AddRange(tNode.Result);
        all.AddRange(tApache.Result);
        all.AddRange(tNginx.Result);
        all.AddRange(tDb.Result);
        all.AddRange(tPma.Result);
        all.AddRange(GetToolsCatalog());

        RefreshInstalledStatus(all);
        MergeInstalledVersions(all);
        return all;
    }

    private static readonly (string Category, string BaseDestFolder, string SubFolder)[] LocalVersionFolders =
    {
        ("PHP", @"bin\php", "php"),
        ("Node.js", @"bin\node", "node"),
        ("Apache", @"bin\apache", "apache"),
        ("Nginx", @"bin\nginx", "nginx"),
        ("Database", @"bin\mysql", "mysql")
    };

    /// <summary>
    /// Quét toàn bộ phiên bản đã cài đặt sẵn trên máy để hiển thị ngay cả khi offline hoặc chưa có trong danh sách online.
    /// </summary>
    public List<DownloadItemModel> GetLocalInstalledCatalog()
    {
        var list = new List<DownloadItemModel>();

        foreach (var (category, baseDestFolder, subFolder) in LocalVersionFolders)
        {
            string root = Path.Combine(PathHelper.BinDir, subFolder);
            if (!Directory.Exists(root)) continue;

            foreach (var dir in Directory.GetDirectories(root)
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any()) continue;
                string folderName = Path.GetFileName(dir);
                list.Add(BuildLocalDownloadItem(category, baseDestFolder, folderName));
            }
        }

        string pmaDir = Path.Combine(PathHelper.RootDir, "phpmyadmin");
        if (Directory.Exists(pmaDir) && Directory.EnumerateFileSystemEntries(pmaDir).Any())
        {
            list.Add(new DownloadItemModel
            {
                Category = "phpMyAdmin",
                DisplayName = "phpMyAdmin (Đã cài trên máy)",
                Url = "https://files.phpmyadmin.net/phpMyAdmin/5.2.3/phpMyAdmin-5.2.3-all-languages.zip",
                BaseDestFolder = "phpmyadmin",
                FolderName = "",
                IsInstalled = true
            });
        }

        var tools = GetToolsCatalog();
        RefreshInstalledStatus(tools);
        list.AddRange(tools.Where(t => t.IsInstalled));

        return list;
    }

    private void MergeInstalledVersions(List<DownloadItemModel> catalog)
    {
        foreach (var (category, baseDestFolder, subFolder) in LocalVersionFolders)
        {
            string root = Path.Combine(PathHelper.BinDir, subFolder);
            if (!Directory.Exists(root)) continue;

            foreach (var dir in Directory.GetDirectories(root)
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any()) continue;
                string folderName = Path.GetFileName(dir);
                bool exists = catalog.Any(x =>
                    x.Category == category &&
                    x.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    catalog.Add(BuildLocalDownloadItem(category, baseDestFolder, folderName));
            }
        }

        var ordered = catalog.OrderByDescending(x => x.IsInstalled).ToList();
        catalog.Clear();
        catalog.AddRange(ordered);
    }

    private static DownloadItemModel BuildLocalDownloadItem(string category, string baseDestFolder, string folderName)
    {
        string displayName;
        string url;

        switch (category)
        {
            case "PHP":
            {
                string ver = Regex.Replace(folderName, @"^php[-_]?", "", RegexOptions.IgnoreCase);
                displayName = $"PHP {ver} (Đã cài trên máy)";
                url = "https://windows.php.net/downloads/releases/archives/";
                break;
            }
            case "Node.js":
            {
                string ver = Regex.Replace(folderName, @"^node[-_]?", "", RegexOptions.IgnoreCase);
                if (!ver.StartsWith("v", StringComparison.OrdinalIgnoreCase)) ver = "v" + ver;
                displayName = $"Node.js {ver} (Win x64)";
                url = $"https://nodejs.org/dist/{ver}/node-{ver}-win-x64.zip";
                break;
            }
            case "Apache":
            {
                string ver = Regex.Replace(folderName, @"^(httpd|apache)[-_]?", "", RegexOptions.IgnoreCase);
                displayName = $"Apache httpd {ver} (Win64)";
                url = "https://www.apachelounge.com/download/";
                break;
            }
            case "Nginx":
            {
                string ver = Regex.Replace(folderName, @"^nginx[-_]?", "", RegexOptions.IgnoreCase);
                displayName = $"Nginx {ver} (Windows)";
                url = $"https://nginx.org/download/nginx-{ver}.zip";
                break;
            }
            case "Database":
            {
                if (folderName.StartsWith("mariadb", StringComparison.OrdinalIgnoreCase))
                {
                    string ver = Regex.Replace(folderName, @"^mariadb[-_]?", "", RegexOptions.IgnoreCase);
                    displayName = $"MariaDB {ver} (Win x64)";
                    url = $"https://archive.mariadb.org/mariadb-{ver}/winx64-packages/mariadb-{ver}-winx64.zip";
                }
                else
                {
                    string ver = Regex.Replace(folderName, @"^mysql[-_]?", "", RegexOptions.IgnoreCase);
                    displayName = $"MySQL {ver} (Oracle)";
                    url = $"https://downloads.mysql.com/archives/get/p/23/file/mysql-{ver}-winx64.zip";
                }
                break;
            }
            default:
                displayName = folderName;
                url = "";
                break;
        }

        return new DownloadItemModel
        {
            Category = category,
            DisplayName = displayName,
            Url = url,
            BaseDestFolder = baseDestFolder,
            FolderName = folderName,
            IsInstalled = true
        };
    }

    private async Task<string> ResolvePhpArchiveUrlAsync(DownloadItemModel item)
    {
        if (!item.Url.TrimEnd('/').Equals("https://windows.php.net/downloads/releases/archives", StringComparison.OrdinalIgnoreCase))
            return item.Url;

        string ver = Regex.Replace(item.FolderName, @"^php[-_]?", "", RegexOptions.IgnoreCase);
        var sources = new[]
        {
            "https://windows.php.net/downloads/releases/archives/",
            "https://windows.php.net/downloads/releases/"
        };

        string pattern = $@"href=""(?:[^""]*/)?(php-{Regex.Escape(ver)}(?:-\d+)?-Win32-(?:vs\d+|vc\d+)-x64\.zip)""";
        foreach (string baseUrl in sources)
        {
            try
            {
                string html = await _http.GetStringAsync(baseUrl);
                var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                    return baseUrl + m.Groups[1].Value;
            }
            catch { }
        }

        throw new InvalidOperationException($"Không tìm thấy gói tải về cho phiên bản PHP {ver} trên trang chủ.");
    }

    public void RefreshInstalledStatus(IEnumerable<DownloadItemModel> items)
    {
        foreach (var item in items)
        {
            string target = string.IsNullOrEmpty(item.FolderName)
                ? Path.Combine(PathHelper.RootDir, item.BaseDestFolder)
                : Path.Combine(PathHelper.RootDir, item.BaseDestFolder, item.FolderName);
            item.IsInstalled = Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any();
        }
    }

    private async Task<List<DownloadItemModel>> ScrapePhpAsync()
    {
        var list = new List<DownloadItemModel>();
        try
        {
            string html = await _http.GetStringAsync("https://windows.php.net/downloads/releases/");
            var matches = Regex.Matches(html, @"href=""(php-(\d+\.\d+\.\d+)(?:-\d+)?-Win32-(?:vs\d+|vc\d+)-x64\.zip)""", RegexOptions.IgnoreCase);
            var added = new HashSet<string>();
            var temp = new List<(Version Ver, DownloadItemModel Item)>();

            foreach (Match m in matches)
            {
                string filename = m.Groups[1].Value;
                string verStr = m.Groups[2].Value;
                if (!added.Add(verStr)) continue;
                Version.TryParse(verStr, out var ver);
                temp.Add((ver ?? new Version(0, 0, 0), new DownloadItemModel
                {
                    Category = "PHP",
                    DisplayName = $"PHP {verStr} (Thread Safe x64)",
                    Url = $"https://windows.php.net/downloads/releases/{filename}",
                    BaseDestFolder = @"bin\php",
                    FolderName = $"php-{verStr}"
                }));
            }

            list.AddRange(temp.OrderByDescending(x => x.Ver).Take(8).Select(x => x.Item));
        }
        catch { }

        if (list.Count == 0)
        {
            list.Add(new DownloadItemModel { Category = "PHP", DisplayName = "PHP 8.5.12 (Thread Safe x64)", Url = "https://windows.php.net/downloads/releases/php-8.5.12-1-Win32-vs17-x64.zip", BaseDestFolder = @"bin\php", FolderName = "php-8.5.12" });
            list.Add(new DownloadItemModel { Category = "PHP", DisplayName = "PHP 8.4.27 (Thread Safe x64)", Url = "https://windows.php.net/downloads/releases/php-8.4.27-1-Win32-vs17-x64.zip", BaseDestFolder = @"bin\php", FolderName = "php-8.4.27" });
            list.Add(new DownloadItemModel { Category = "PHP", DisplayName = "PHP 8.3.36 (Thread Safe x64)", Url = "https://windows.php.net/downloads/releases/php-8.3.36-1-Win32-vs16-x64.zip", BaseDestFolder = @"bin\php", FolderName = "php-8.3.36" });
            list.Add(new DownloadItemModel { Category = "PHP", DisplayName = "PHP 8.2.35 (Thread Safe x64)", Url = "https://windows.php.net/downloads/releases/php-8.2.35-1-Win32-vs16-x64.zip", BaseDestFolder = @"bin\php", FolderName = "php-8.2.35" });
        }

        if (!list.Any(x => x.FolderName == "php-7.4.33"))
            list.Add(new DownloadItemModel { Category = "PHP", DisplayName = "PHP 7.4.33 (Kế thừa cũ)", Url = "https://windows.php.net/downloads/releases/archives/php-7.4.33-Win32-vc15-x64.zip", BaseDestFolder = @"bin\php", FolderName = "php-7.4.33" });
        if (!list.Any(x => x.FolderName == "php-5.6.40"))
            list.Add(new DownloadItemModel { Category = "PHP", DisplayName = "PHP 5.6.40 (Kế thừa rất cũ)", Url = "https://windows.php.net/downloads/releases/archives/php-5.6.40-Win32-VC11-x64.zip", BaseDestFolder = @"bin\php", FolderName = "php-5.6.40" });

        return list;
    }

    private async Task<List<DownloadItemModel>> ScrapeNodeAsync()
    {
        var list = new List<DownloadItemModel>();
        try
        {
            string json = await _http.GetStringAsync("https://nodejs.org/dist/index.json");
            var matches = Regex.Matches(json, @"""version""\s*:\s*""(v(\d+)\.\d+\.\d+)""");
            var seenMajor = new HashSet<string>();
            foreach (Match m in matches)
            {
                string v = m.Groups[1].Value;
                string major = m.Groups[2].Value;
                if (!seenMajor.Add(major)) continue;
                list.Add(new DownloadItemModel
                {
                    Category = "Node.js",
                    DisplayName = $"Node.js {v} (Win x64)",
                    Url = $"https://nodejs.org/dist/{v}/node-{v}-win-x64.zip",
                    BaseDestFolder = @"bin\node",
                    FolderName = $"node-{v}"
                });
                if (list.Count >= 6) break;
            }
        }
        catch { }

        if (list.Count == 0)
        {
            list.Add(new DownloadItemModel { Category = "Node.js", DisplayName = "Node.js v22.14.0 LTS (Win x64)", Url = "https://nodejs.org/dist/v22.14.0/node-v22.14.0-win-x64.zip", BaseDestFolder = @"bin\node", FolderName = "node-v22.14.0" });
            list.Add(new DownloadItemModel { Category = "Node.js", DisplayName = "Node.js v20.19.0 LTS (Win x64)", Url = "https://nodejs.org/dist/v20.19.0/node-v20.19.0-win-x64.zip", BaseDestFolder = @"bin\node", FolderName = "node-v20.19.0" });
        }
        return list;
    }

    private async Task<List<DownloadItemModel>> ScrapeApacheAsync()
    {
        var list = new List<DownloadItemModel>();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.apachelounge.com/download/");
            req.Headers.Referrer = new Uri("https://www.apachelounge.com/");
            var resp = await _http.SendAsync(req);
            string html = await resp.Content.ReadAsStringAsync();
            var matches = Regex.Matches(html, @"href=""(/download/(?:VS\d+)/binaries/(httpd-([\d\.]+)[^""']*-Win64-VS\d+\.zip))""", RegexOptions.IgnoreCase);
            var added = new HashSet<string>();
            foreach (Match m in matches)
            {
                string ver = m.Groups[3].Value;
                if (!added.Add(ver)) continue;
                list.Add(new DownloadItemModel
                {
                    Category = "Apache",
                    DisplayName = $"Apache httpd {ver} (Win64)",
                    Url = "https://www.apachelounge.com" + m.Groups[1].Value,
                    BaseDestFolder = @"bin\apache",
                    FolderName = $"httpd-{ver}"
                });
            }
        }
        catch { }

        if (list.Count == 0)
            list.Add(new DownloadItemModel { Category = "Apache", DisplayName = "Apache httpd 2.4.69 (Win64)", Url = "https://www.apachelounge.com/download/VS17/binaries/httpd-2.4.69-Win64-VS17.zip", BaseDestFolder = @"bin\apache", FolderName = "httpd-2.4.69" });
        return list;
    }

    private async Task<List<DownloadItemModel>> ScrapeNginxAsync()
    {
        var list = new List<DownloadItemModel>();
        try
        {
            string html = await _http.GetStringAsync("https://nginx.org/en/download.html");
            var matches = Regex.Matches(html, @"href=""(/download/nginx-(\d+\.\d+\.\d+)\.zip)""");
            foreach (Match m in matches.Cast<Match>().Take(6))
            {
                string ver = m.Groups[2].Value;
                list.Add(new DownloadItemModel
                {
                    Category = "Nginx",
                    DisplayName = $"Nginx {ver} (Windows)",
                    Url = "https://nginx.org" + m.Groups[1].Value,
                    BaseDestFolder = @"bin\nginx",
                    FolderName = $"nginx-{ver}"
                });
            }
        }
        catch { }
        return list;
    }

    private Task<List<DownloadItemModel>> ScrapeDatabaseAsync()
    {
        var list = new List<DownloadItemModel>
        {
            new() { Category = "Database", DisplayName = "MariaDB 11.4.4 LTS (Win x64)", Url = "https://archive.mariadb.org/mariadb-11.4.4/winx64-packages/mariadb-11.4.4-winx64.zip", BaseDestFolder = @"bin\mysql", FolderName = "mariadb-11.4.4" },
            new() { Category = "Database", DisplayName = "MariaDB 10.11.10 LTS (Win x64)", Url = "https://archive.mariadb.org/mariadb-10.11.10/winx64-packages/mariadb-10.11.10-winx64.zip", BaseDestFolder = @"bin\mysql", FolderName = "mariadb-10.11.10" },
            new() { Category = "Database", DisplayName = "MySQL 8.0.36 (Oracle Community)", Url = "https://downloads.mysql.com/archives/get/p/23/file/mysql-8.0.36-winx64.zip", BaseDestFolder = @"bin\mysql", FolderName = "mysql-8.0.36" },
            new() { Category = "Database", DisplayName = "MySQL 5.7.44 (Oracle Classic)", Url = "https://downloads.mysql.com/archives/get/p/23/file/mysql-5.7.44-winx64.zip", BaseDestFolder = @"bin\mysql", FolderName = "mysql-5.7.44" }
        };
        return Task.FromResult(list);
    }

    private async Task<List<DownloadItemModel>> ScrapePhpMyAdminAsync()
    {
        var list = new List<DownloadItemModel>();
        try
        {
            string html = await _http.GetStringAsync("https://www.phpmyadmin.net/downloads/");
            var matches = Regex.Matches(html, @"href=""(https://files\.phpmyadmin\.net/phpMyAdmin/(\d+\.\d+\.\d+)/phpMyAdmin-[\d\.]+-all-languages\.zip)""");
            var added = new HashSet<string>();
            foreach (Match m in matches)
            {
                string ver = m.Groups[2].Value;
                if (!added.Add(ver)) continue;
                list.Add(new DownloadItemModel
                {
                    Category = "phpMyAdmin",
                    DisplayName = $"phpMyAdmin {ver} (All Languages)",
                    Url = m.Groups[1].Value,
                    BaseDestFolder = "phpmyadmin",
                    FolderName = ""
                });
            }
        }
        catch { }

        if (list.Count == 0)
            list.Add(new DownloadItemModel { Category = "phpMyAdmin", DisplayName = "phpMyAdmin 5.2.3 (All Languages)", Url = "https://files.phpmyadmin.net/phpMyAdmin/5.2.3/phpMyAdmin-5.2.3-all-languages.zip", BaseDestFolder = "phpmyadmin", FolderName = "" });
        return list;
    }

    private static List<DownloadItemModel> GetToolsCatalog() => new()
    {
        new() { Category = "Tiện ích & Runtime", DisplayName = "Visual C++ Redistributable 2015-2022 (x64)", Url = "https://aka.ms/vs/17/release/vc_redist.x64.exe", BaseDestFolder = @"bin\vcruntime", FolderName = "" },
        new() { Category = "Tiện ích & Runtime", DisplayName = "Cloudflared Tunnel (Stable x64)", Url = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe", BaseDestFolder = @"bin\cloudflared", FolderName = "" }
    };

    public async Task<(bool Ok, string Message)> DownloadAndInstallAsync(DownloadItemModel item)
    {
        item.IsDownloading = true;
        item.Progress = 0;
        item.StatusLabel = "Đang tải xuống...";
        try
        {
            string destDir = string.IsNullOrEmpty(item.FolderName)
                ? Path.Combine(PathHelper.RootDir, item.BaseDestFolder)
                : Path.Combine(PathHelper.RootDir, item.BaseDestFolder, item.FolderName);
            Directory.CreateDirectory(destDir);

            string downloadUrl = item.Category == "PHP"
                ? await ResolvePhpArchiveUrlAsync(item)
                : item.Url;

            if (string.IsNullOrWhiteSpace(downloadUrl) ||
                (!downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                 !downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Phiên bản {item.DisplayName} chỉ có sẵn trên máy và không có link tải lại trực tiếp.");
            }

            bool isExe = downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            string tempFile = Path.Combine(PathHelper.TmpDir, $"dl_{Guid.NewGuid():N}" + (isExe ? ".exe" : ".zip"));

            using (var resp = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                long total = resp.Content.Headers.ContentLength ?? -1;
                await using var src = await resp.Content.ReadAsStreamAsync();
                await using var dst = File.Create(tempFile);
                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await src.ReadAsync(buffer)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, read));
                    readTotal += read;
                    if (total > 0)
                        item.Progress = Math.Min(95, (double)readTotal / total * 95.0);
                }
            }

            item.StatusLabel = "Đang giải nén & cấu hình...";
            item.Progress = 96;

            if (isExe)
            {
                string fileName = Path.GetFileName(new Uri(item.Url).LocalPath);
                string finalExe = Path.Combine(destDir, fileName);
                File.Copy(tempFile, finalExe, true);
                if (fileName.Contains("vc_redist", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(finalExe, "/install /passive /norestart") { UseShellExecute = true });
                }
            }
            else
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(tempFile, destDir, true));
                // Flatten single nested folder if zip had a wrapper directory
                var subDirs = Directory.GetDirectories(destDir);
                var topFiles = Directory.GetFiles(destDir);
                if (subDirs.Length == 1 && topFiles.Length == 0 && item.Category != "Apache")
                {
                    string nested = subDirs[0];
                    foreach (var f in Directory.GetFiles(nested))
                        File.Move(f, Path.Combine(destDir, Path.GetFileName(f)), true);
                    foreach (var d in Directory.GetDirectories(nested))
                    {
                        string targetSub = Path.Combine(destDir, Path.GetFileName(d));
                        if (Directory.Exists(targetSub)) Directory.Delete(targetSub, true);
                        Directory.Move(d, targetSub);
                    }
                    Directory.Delete(nested, true);
                }

                if (item.Category == "PHP")
                    await _procService.ConfigurePhpIniAsync(destDir);
            }

            try { File.Delete(tempFile); } catch { }
            item.Progress = 100;
            item.IsInstalled = true;
            item.IsDownloading = false;
            return (true, $"Đã cài đặt {item.DisplayName} thành công!");
        }
        catch (Exception ex)
        {
            item.IsDownloading = false;
            item.StatusLabel = "Lỗi tải xuống";
            return (false, $"Lỗi cài đặt {item.DisplayName}: {ex.Message}");
        }
    }
}

