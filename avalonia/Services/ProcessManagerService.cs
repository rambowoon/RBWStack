using RBWStack.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.Services;

public class ProcessManagerService
{
    private readonly Dictionary<string, Process> _phpPoolProcs = new(StringComparer.OrdinalIgnoreCase);
    private Process? _webProc;
    private Process? _mysqlProc;

    public List<string> GetInstalledVersions(string subFolder)
    {
        string dir = Path.Combine(PathHelper.BinDir, subFolder);
        if (!Directory.Exists(dir)) return new List<string>();
        return Directory.GetDirectories(dir)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool IsProcessRunning(string processName)
    {
        try { return Process.GetProcessesByName(processName).Length > 0; }
        catch { return false; }
    }

    public string GetConfigPath(string serviceKey, string version, bool isNginx)
    {
        if (string.IsNullOrEmpty(version)) return "";
        return serviceKey switch
        {
            "web" => isNginx
                ? Path.Combine(PathHelper.BinDir, "nginx", version, "conf", "nginx.conf")
                : Path.Combine(PathHelper.BinDir, "apache", version, "conf", "httpd.conf"),
            "php" => Path.Combine(PathHelper.BinDir, "php", version, "php.ini"),
            "mysql" => Path.Combine(PathHelper.BinDir, "mysql", version, "my.ini"),
            _ => ""
        };
    }

    public static int GetPhpPortForVersion(string phpVersionDir, string activePhpVersion, int basePort = 9000)
    {
        if (!string.IsNullOrEmpty(activePhpVersion) && phpVersionDir.Equals(activePhpVersion, StringComparison.OrdinalIgnoreCase))
            return basePort;

        string phpRoot = Path.Combine(PathHelper.BinDir, "php");
        if (Directory.Exists(phpRoot))
        {
            string[] phpDirs = Directory.GetDirectories(phpRoot, "php*");
            Array.Sort(phpDirs, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < phpDirs.Length; i++)
            {
                if (Path.GetFileName(phpDirs[i]).Equals(phpVersionDir, StringComparison.OrdinalIgnoreCase))
                    return basePort + 1 + i;
            }
        }
        return basePort;
    }

    public async Task<(bool Ok, string Message)> StartWebServerAsync(
        AppSettingsModel settings,
        string version,
        string activePhpVersion,
        Dictionary<string, string> sitePhpMap,
        Dictionary<string, string> vhostsMap,
        IEnumerable<SiteItemModel>? allSites = null)
    {
        await EnsureAllPhpIonCubeAsync();
        bool isNginx = settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(version) || version == "Chưa cài đặt") return (false, "Chưa cài đặt Web Server.");

        if (!isNginx)
            await UpdateApacheMultiPhpRoutingAsync(settings, version, activePhpVersion, sitePhpMap, allSites);
        else
            await UpdateNginxMultiPhpRoutingAsync(settings, version, activePhpVersion, sitePhpMap, allSites);

        if (isNginx)
        {
            string exe = Path.Combine(PathHelper.BinDir, "nginx", version, "nginx.exe");
            if (!File.Exists(exe)) return (false, $"Không tìm thấy {exe}");
            StopProcessByName("nginx");
            await Task.Delay(200);
            _webProc = StartHidden(exe, "", Path.GetDirectoryName(exe)!);
            await Task.Delay(400);
            return (IsProcessRunning("nginx"), IsProcessRunning("nginx") ? "Nginx đang chạy" : "Không thể khởi động Nginx");
        }
        else
        {
            string apacheDir = Path.Combine(PathHelper.BinDir, "apache", version);
            string exe = Path.Combine(apacheDir, "bin", "httpd.exe");
            if (!File.Exists(exe))
            {
                // Some Apache packages nest inside Apache24/
                string nested = Path.Combine(apacheDir, "Apache24", "bin", "httpd.exe");
                if (File.Exists(nested)) exe = nested;
            }
            if (!File.Exists(exe)) return (false, $"Không tìm thấy httpd.exe trong {apacheDir}");

            StopProcessByName("httpd");
            await Task.Delay(200);
            _webProc = StartHidden(exe, "", Path.GetDirectoryName(exe)!);
            await Task.Delay(500);
            return (IsProcessRunning("httpd"), IsProcessRunning("httpd") ? "Apache đang chạy" : "Không thể khởi động Apache (kiểm tra cổng " + settings.WebPort + ")");
        }
    }

    public void StopWebServer(bool isNginx)
    {
        StopProcessByName(isNginx ? "nginx" : "httpd");
        _webProc = null;
    }

    public async Task<bool> EnsurePhpPoolRunningAsync(AppSettingsModel settings, PhpPoolVersionModel pool)
    {
        if (string.IsNullOrEmpty(pool.Version) || pool.Version == "Chưa cài đặt")
            return false;

        if (_phpPoolProcs.TryGetValue(pool.Version, out var existing))
        {
            try
            {
                if (!existing.HasExited)
                {
                    pool.IsRunning = true;
                    return true;
                }
            }
            catch { }
            _phpPoolProcs.Remove(pool.Version);
        }

        string phpDir = Path.Combine(PathHelper.BinDir, "php", pool.Version);
        string phpCgi = Path.Combine(phpDir, "php-cgi.exe");
        if (!File.Exists(phpCgi)) return false;

        await ConfigureIonCubeOnlyAsync(phpDir);
        if (settings.AutoOptimizePhpIni)
            await ConfigurePhpIniAsync(phpDir);

        string iniPath = Path.Combine(phpDir, "php.ini");
        string args = File.Exists(iniPath)
            ? $"-b 127.0.0.1:{pool.Port} -c \"{iniPath}\""
            : $"-b 127.0.0.1:{pool.Port}";

        var proc = StartHidden(phpCgi, args, phpDir, new Dictionary<string, string>
        {
            ["PHP_FCGI_MAX_REQUESTS"] = "0",
            ["PHP_FCGI_CHILDREN"] = "4"
        });
        if (proc != null)
        {
            _phpPoolProcs[pool.Version] = proc;
            await Task.Delay(150);
            pool.IsRunning = !proc.HasExited;
            return pool.IsRunning;
        }
        return false;
    }

    public async Task<(bool Ok, string Message)> StartPhpFastCgiPoolsAsync(AppSettingsModel settings, IEnumerable<PhpPoolVersionModel> pools)
    {
        var selected = pools.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0)
            return (false, "Chưa chọn phiên bản PHP nào để chạy.");

        StopPhpFastCgi();
        await Task.Delay(250);

        int startedCount = 0;
        foreach (var pool in selected)
        {
            if (await EnsurePhpPoolRunningAsync(settings, pool))
                startedCount++;
        }

        await Task.Delay(300);
        bool running = IsProcessRunning("php-cgi");
        if (!running)
        {
            foreach (var pool in pools) pool.IsRunning = false;
        }
        string names = string.Join(", ", selected.Select(p => $"{p.Version}:{p.Port}"));
        return (running, running ? $"PHP FastCGI đang chạy ({startedCount} bản: {names})" : "Không thể khởi động PHP FastCGI");
    }

    public async Task<(bool Ok, string Message)> StartPhpFastCgiAsync(AppSettingsModel settings, string version, Dictionary<string, string> sitePhpMap)
    {
        return await StartPhpFastCgiPoolsAsync(settings, new[]
        {
            new PhpPoolVersionModel
            {
                Version = version,
                Port = int.TryParse(settings.PhpFastCgiPort, out int p) ? p : 9000,
                IsSelected = true
            }
        });
    }

    public void StopPhpFastCgi()
    {
        foreach (var p in _phpPoolProcs.Values)
        {
            try { if (!p.HasExited) p.Kill(true); } catch { }
        }
        _phpPoolProcs.Clear();
        StopProcessByName("php-cgi");
    }

    public async Task<(bool Ok, string Message)> StartMySqlAsync(AppSettingsModel settings, string version)
    {
        if (string.IsNullOrEmpty(version)) return (false, "Chưa cài đặt MySQL / MariaDB.");
        string mysqlDir = Path.Combine(PathHelper.BinDir, "mysql", version);
        string exe = Path.Combine(mysqlDir, "bin", "mysqld.exe");
        if (!File.Exists(exe)) exe = Path.Combine(mysqlDir, "bin", "mariadbd.exe");
        if (!File.Exists(exe)) return (false, $"Không tìm thấy mysqld.exe trong {mysqlDir}");

        StopProcessByName("mysqld");
        StopProcessByName("mariadbd");
        await Task.Delay(250);

        string iniPath = Path.Combine(mysqlDir, "my.ini");
        string args = File.Exists(iniPath) ? $"--defaults-file=\"{iniPath}\" --console" : "--console";
        _mysqlProc = StartHidden(exe, args, Path.GetDirectoryName(exe)!);
        await Task.Delay(700);
        bool running = IsProcessRunning("mysqld") || IsProcessRunning("mariadbd");
        return (running, running ? "MySQL / MariaDB đang chạy" : "Không thể khởi động MySQL / MariaDB");
    }

    public void StopMySql()
    {
        StopProcessByName("mysqld");
        StopProcessByName("mariadbd");
        _mysqlProc = null;
    }

    public async Task EnsureAllPhpIonCubeAsync()
    {
        string phpRoot = Path.Combine(PathHelper.BinDir, "php");
        if (!Directory.Exists(phpRoot)) return;
        foreach (var dir in Directory.GetDirectories(phpRoot))
        {
            await ConfigureIonCubeOnlyAsync(dir);
        }
    }

    public async Task ConfigureIonCubeOnlyAsync(string phpDir)
    {
        try
        {
            string iniPath = Path.Combine(phpDir, "php.ini");
            if (!File.Exists(iniPath))
            {
                string devIni = Path.Combine(phpDir, "php.ini-development");
                string prodIni = Path.Combine(phpDir, "php.ini-production");
                if (File.Exists(devIni)) File.Copy(devIni, iniPath);
                else if (File.Exists(prodIni)) File.Copy(prodIni, iniPath);
                else return;
            }

            string content = await File.ReadAllTextAsync(iniPath);
            bool changed = false;

            if (!Regex.IsMatch(content, @"(?m)^\s*extension_dir\s*=\s*""ext"""))
            {
                content = Regex.Replace(content, @"(?m)^\s*;?\s*extension_dir\s*=\s*""ext""", "extension_dir = \"ext\"");
                changed = true;
            }

            var m = Regex.Match(Path.GetFileName(phpDir.TrimEnd('\\', '/')), @"(\d+\.\d+)");
            if (m.Success)
            {
                string shortVer = m.Groups[1].Value;
                string loaderName = $"ioncube_loader_win_{shortVer}.dll";
                string localSource = Path.Combine(PathHelper.DataDir, "ioncube", loaderName);
                string extDir = Path.Combine(phpDir, "ext");
                Directory.CreateDirectory(extDir);
                string targetDll = Path.Combine(extDir, loaderName);

                if (!File.Exists(targetDll))
                {
                    if (File.Exists(localSource))
                    {
                        File.Copy(localSource, targetDll, true);
                    }
                    else
                    {
                        try
                        {
                            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                            var bytes = await http.GetByteArrayAsync($"https://raw.githubusercontent.com/rambowoon/RBWStack/main/data/ioncube/{loaderName}");
                            if (bytes.Length > 50_000)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(localSource)!);
                                await File.WriteAllBytesAsync(localSource, bytes);
                                await File.WriteAllBytesAsync(targetDll, bytes);
                            }
                        }
                        catch { }
                    }
                }

                if (File.Exists(targetDll) && !Regex.IsMatch(content, @"(?m)^\s*zend_extension\s*=.*" + Regex.Escape(loaderName), RegexOptions.IgnoreCase))
                {
                    string zendLine = $"zend_extension = \"{loaderName}\"";
                    if (Regex.IsMatch(content, @"\[PHP\]", RegexOptions.IgnoreCase))
                        content = Regex.Replace(content, @"(\[PHP\]\r?\n)", $"$1{zendLine}\r\n", RegexOptions.IgnoreCase);
                    else
                        content = $"{zendLine}\r\n{content}";
                    changed = true;
                }
            }

            if (changed)
                await File.WriteAllTextAsync(iniPath, content);
        }
        catch { }
    }

    public async Task ConfigurePhpIniAsync(string phpDir)
    {
        try
        {
            await ConfigureIonCubeOnlyAsync(phpDir);
            string iniPath = Path.Combine(phpDir, "php.ini");
            if (!File.Exists(iniPath)) return;

            string content = await File.ReadAllTextAsync(iniPath);
            content = Regex.Replace(content, @"upload_max_filesize\s*=\s*\S+", "upload_max_filesize = 2048M");
            content = Regex.Replace(content, @"post_max_size\s*=\s*\S+", "post_max_size = 2048M");
            content = Regex.Replace(content, @"memory_limit\s*=\s*\S+", "memory_limit = 1024M");
            content = Regex.Replace(content, @"max_execution_time\s*=\s*\S+", "max_execution_time = 0");

            string[] commonExts = { "curl", "fileinfo", "gd", "gd2", "intl", "mbstring", "mysqli", "openssl", "pdo_mysql", "pdo_sqlite", "soap", "sockets", "zip", "exif" };
            foreach (var ext in commonExts)
            {
                content = Regex.Replace(content, $@"^\s*;\s*extension\s*=\s*{ext}\b", $"extension={ext}", RegexOptions.Multiline);
            }

            await File.WriteAllTextAsync(iniPath, content);
        }
        catch { }
    }

    public static void UpdateHostsEntry(string domain, bool add)
    {
        try
        {
            string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
            if (!File.Exists(hostsPath)) return;
            var lines = File.ReadAllLines(hostsPath).ToList();
            lines.RemoveAll(l => l.Contains(domain, StringComparison.OrdinalIgnoreCase) && l.Contains("# RBWStack"));
            if (add && !string.IsNullOrWhiteSpace(domain))
                lines.Add($"127.0.0.1 {domain.Trim()} # RBWStack");
            File.WriteAllLines(hostsPath, lines);
        }
        catch { }
    }

    public async Task ReloadOrRestartWebServerAsync(
        AppSettingsModel settings,
        string webVersion,
        string activePhpVersion,
        Dictionary<string, string> sitePhpMap,
        Dictionary<string, string> vhostsMap,
        IEnumerable<SiteItemModel>? allSites = null)
    {
        bool isNginx = settings.DefaultWebServer.Contains("Nginx", StringComparison.OrdinalIgnoreCase);
        if (!isNginx)
            await UpdateApacheMultiPhpRoutingAsync(settings, webVersion, activePhpVersion, sitePhpMap, allSites);
        else
            await UpdateNginxMultiPhpRoutingAsync(settings, webVersion, activePhpVersion, sitePhpMap, allSites);

        if (IsProcessRunning(isNginx ? "nginx" : "httpd"))
            await StartWebServerAsync(settings, webVersion, activePhpVersion, sitePhpMap, vhostsMap, allSites);
    }

    public async Task UpdateApacheMultiPhpRoutingAsync(
        AppSettingsModel settings,
        string apacheVersion,
        string activePhpVersion,
        Dictionary<string, string> sitePhpMap,
        IEnumerable<SiteItemModel>? allSites = null)
    {
        try
        {
            string apacheDir = Path.Combine(PathHelper.BinDir, "apache", apacheVersion);
            string confPath = Path.Combine(apacheDir, "conf", "httpd.conf");
            if (!File.Exists(confPath)) confPath = Path.Combine(apacheDir, "Apache24", "conf", "httpd.conf");
            if (!File.Exists(confPath)) return;

            string content = await File.ReadAllTextAsync(confPath);
            int basePort = int.TryParse(settings.PhpFastCgiPort, out int bp) ? bp : 9000;
            string activePhp = !string.IsNullOrEmpty(activePhpVersion) ? activePhpVersion : settings.ActivePhpVersion;
            string wwwSlash = PathHelper.WwwDir.Replace('\\', '/').TrimEnd('/');

            // Ensure global FilesMatch uses basePort (9000) unless overridden per-directory
            content = Regex.Replace(
                content,
                @"(?ms)^<FilesMatch\s+\\\.php\$>\s*SetHandler\s+""proxy:fcgi://127\.0\.0\.1:\d+//\./""\s*</FilesMatch>",
                $"<FilesMatch \\.php$>\r\n    SetHandler \"proxy:fcgi://127.0.0.1:{basePort}//./\"\r\n</FilesMatch>");

            // Remove old standalone per-site <Directory "..."> blocks previously written by older versions
            content = Regex.Replace(
                content,
                @"(?ms)^<Directory\s+""[^""]+""\s*>\s*<FilesMatch\s+\\\.php\$>\s*SetHandler\s+""proxy:fcgi://127\.0\.0\.1:\d+//\./""\s*</FilesMatch>[\s\S]*?</Directory>\s*",
                "");

            // Build lookup from RelativePath -> actual FullPath (covers sub-folder DocumentRoot like www/2026_11)
            var fullPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (allSites != null)
            {
                foreach (var s in allSites)
                {
                    string relKey = s.RelativePath.Replace('\\', '/').Trim('/');
                    if (!string.IsNullOrEmpty(relKey) && !string.IsNullOrEmpty(s.FullPath))
                        fullPathMap[relKey] = s.FullPath.Replace('\\', '/').TrimEnd('/');
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# BEGIN RBWSTACK MULTI-PHP");
            var emittedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in sitePhpMap)
            {
                string siteRel = kvp.Key.Replace('\\', '/').Trim('/');
                string phpVer = kvp.Value;
                if (string.IsNullOrWhiteSpace(siteRel) || string.IsNullOrWhiteSpace(phpVer)) continue;

                int port = GetPhpPortForVersion(phpVer, activePhp, basePort);
                var dirsForSite = new List<string> { $"{wwwSlash}/{siteRel}" };
                if (fullPathMap.TryGetValue(siteRel, out var mappedFull) && !dirsForSite.Contains(mappedFull, StringComparer.OrdinalIgnoreCase))
                    dirsForSite.Add(mappedFull);

                foreach (var siteDir in dirsForSite)
                {
                    if (!emittedDirs.Add(siteDir)) continue;
                    sb.AppendLine($"<Directory \"{siteDir}\">");
                    sb.AppendLine("    <FilesMatch \\.php$>");
                    sb.AppendLine($"        SetHandler \"proxy:fcgi://127.0.0.1:{port}//./\"");
                    sb.AppendLine("    </FilesMatch>");
                    sb.AppendLine("    AllowOverride All");
                    sb.AppendLine("    Require all granted");
                    sb.AppendLine("</Directory>");

                    // Also update any matching <VirtualHost> block inside httpd.conf for this project directory
                    string escapedDir = Regex.Escape(siteDir);
                    content = Regex.Replace(
                        content,
                        $@"(?ms)(<Directory\s+""{escapedDir}"">(?:(?!</Directory>)[\s\S])*?<FilesMatch\s+\\\.php\$>\s*SetHandler\s+""proxy:fcgi://127\.0\.0\.1:)\d+(//\./"")",
                        $"${{1}}{port}${{2}}");
                }
            }
            sb.AppendLine("# END RBWSTACK MULTI-PHP");

            string block = sb.ToString();
            if (Regex.IsMatch(content, @"# BEGIN RBWSTACK MULTI-PHP[\s\S]*?# END RBWSTACK MULTI-PHP"))
                content = Regex.Replace(content, @"# BEGIN RBWSTACK MULTI-PHP[\s\S]*?# END RBWSTACK MULTI-PHP", block);
            else
                content = content.TrimEnd() + "\r\n\r\n" + block;

            await File.WriteAllTextAsync(confPath, content);
        }
        catch { }
    }

    public async Task UpdateNginxMultiPhpRoutingAsync(
        AppSettingsModel settings,
        string nginxVersion,
        string activePhpVersion,
        Dictionary<string, string> sitePhpMap,
        IEnumerable<SiteItemModel>? allSites = null)
    {
        try
        {
            string nginxDir = Path.Combine(PathHelper.BinDir, "nginx", nginxVersion);
            string confPath = Path.Combine(nginxDir, "conf", "nginx.conf");
            if (!File.Exists(confPath)) return;

            string content = await File.ReadAllTextAsync(confPath);
            int basePort = int.TryParse(settings.PhpFastCgiPort, out int bp) ? bp : 9000;
            string activePhp = !string.IsNullOrEmpty(activePhpVersion) ? activePhpVersion : settings.ActivePhpVersion;

            foreach (var kvp in sitePhpMap)
            {
                string siteRel = kvp.Key.Replace('\\', '/').Trim('/');
                int port = GetPhpPortForVersion(kvp.Value, activePhp, basePort);
                string escapedRel = Regex.Escape(siteRel);
                content = Regex.Replace(
                    content,
                    $@"(?ms)(location\s+\^~\s+/{escapedRel}/[\s\S]*?fastcgi_pass\s+127\.0\.0\.1:)\d+(;)",
                    $"${{1}}{port}${{2}}");
            }
            await File.WriteAllTextAsync(confPath, content);
        }
        catch { }
    }

    private static Process? StartHidden(string fileName, string args, string workDir, Dictionary<string, string>? envVars = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        if (envVars != null)
        {
            foreach (var kv in envVars) psi.Environment[kv.Key] = kv.Value;
        }
        return Process.Start(psi);
    }

    private static void StopProcessByName(string name)
    {
        try
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try { p.Kill(true); } catch { }
            }
        }
        catch { }

        // Force-kill full process tree via taskkill so no child php-cgi/httpd/mysqld remains
        try
        {
            using var tk = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/F /IM {name}.exe /T",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            tk?.WaitForExit(2000);
        }
        catch { }
    }
}

