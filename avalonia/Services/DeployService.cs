using RBWStack.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.Services;

public class DeployService
{
    public static void ParseQuickPaste(string clipboardText, DemoServerModel target)
    {
        if (string.IsNullOrWhiteSpace(clipboardText)) return;
        var ipM = Regex.Match(clipboardText, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
        if (ipM.Success) target.Host = ipM.Groups[1].Value;

        var domM = Regex.Match(clipboardText, @"(?:domain|tên miền|url|link|web)\s*[:=]\s*https?://([^\s/]+)|(?:domain|tên miền)\s*[:=]\s*([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})", RegexOptions.IgnoreCase);
        if (domM.Success) target.Domain = !string.IsNullOrEmpty(domM.Groups[1].Value) ? domM.Groups[1].Value : domM.Groups[2].Value;

        var usrM = Regex.Match(clipboardText, @"(?:user|username|tài khoản|tk)\s*[:=]\s*([^\s]+)", RegexOptions.IgnoreCase);
        if (usrM.Success) target.Username = usrM.Groups[1].Value.Trim();

        var pwdM = Regex.Match(clipboardText, @"(?:pass|password|mật khẩu|mk)\s*[:=]\s*([^\s]+)", RegexOptions.IgnoreCase);
        if (pwdM.Success) target.Password = pwdM.Groups[1].Value.Trim();

        var portM = Regex.Match(clipboardText, @"(?:port|cổng)\s*[:=]\s*(\d{2,5})", RegexOptions.IgnoreCase);
        if (portM.Success) target.DaPort = portM.Groups[1].Value.Trim();
    }

    public async Task<(bool Ok, string Message)> TestConnectionAsync(DemoServerModel server)
    {
        if (string.IsNullOrWhiteSpace(server.Host) || string.IsNullOrWhiteSpace(server.Username))
            return (false, "Thiếu Host hoặc Username");

        try
        {
            string ftpRoot = (server.FtpRoot ?? "/public_html").Replace("{domain}", server.Domain ?? "").TrimStart('/');
            string url = $"ftp://{server.Host}/{ftpRoot}/";
#pragma warning disable SYSLIB0014
            var req = (FtpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014
            req.Method = WebRequestMethods.Ftp.ListDirectory;
            req.Credentials = new NetworkCredential(server.Username, server.Password);
            req.Timeout = 8000;
            using var resp = (FtpWebResponse)await req.GetResponseAsync();
            return (true, $"Kết nối FTP OK ({resp.StatusCode})");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi FTP: {ex.Message}");
        }
    }

    public async Task<(bool Ok, string DemoUrl)> DeployProjectAsync(
        SiteItemModel site,
        DemoServerModel server,
        string overrideDomain,
        string customDbSuffix,
        bool useSsl,
        bool onlyDb,
        bool use7Zip,
        Action<string> log,
        string dbNamingRule = "Standard ({user}_{dbname})",
        Action<double, string>? onProgress = null)
    {
        string activeDomain = !string.IsNullOrWhiteSpace(overrideDomain) ? overrideDomain.Trim() : (server.Domain ?? "").Trim();
        if (string.IsNullOrWhiteSpace(server.Host) || string.IsNullOrWhiteSpace(server.Username) || string.IsNullOrWhiteSpace(activeDomain))
        {
            log("❌ Thiếu thông tin Host, Username hoặc Tên miền đích. Vui lòng kiểm tra ở Cài đặt chung!");
            return (false, "");
        }

        string ftpRoot = string.IsNullOrWhiteSpace(server.FtpRoot) ? "/public_html" : server.FtpRoot.Trim();
        ftpRoot = ftpRoot.Replace("{domain}", activeDomain);

        string relPath = site.RelativePath.Replace('\\', '/').Trim('/');
        string ftpBase = $"ftp://{server.Host}/{(ftpRoot.TrimStart('/') + "/" + relPath).Replace("//", "/").TrimEnd('/')}/";
        string scheme = useSsl ? "https://" : "http://";
        string demoUrl = $"{scheme}{activeDomain}/{relPath}";
        string bridgeUrl = $"{demoUrl}/bridge.php?action={(onlyDb ? "deployDb" : "deploy")}";

        string jobId = DateTime.Now.ToString("yyyyMMddHHmmss");
        string zipPath = Path.Combine(PathHelper.TmpDir, $"dist_{jobId}.zip");
        string sqlPath = Path.Combine(PathHelper.TmpDir, $"dist_{jobId}.sql");

        try
        {
            log($"🚀 Bắt đầu deploy {site.Name} → {demoUrl}");
            log($"📁 FTP Target: {ftpBase}");

            // Step 1: Zip source
            if (!onlyDb)
            {
                string exe7z = @"C:\Program Files\7-Zip\7z.exe";
                if (!File.Exists(exe7z)) exe7z = @"C:\Program Files (x86)\7-Zip\7z.exe";

                if (use7Zip && File.Exists(exe7z))
                {
                    log("📦 Đang nén mã nguồn bằng 7-Zip...");
                    onProgress?.Invoke(0, "Đang quét & nén mã nguồn bằng 7-Zip...");
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    int code = await Run7ZipWithProgressAsync(exe7z, site.FullPath, zipPath, onProgress);
                    if (code != 0) throw new Exception($"7-Zip thoát với mã lỗi {code}");
                }
                else
                {
                    log("📦 Đang nén mã nguồn (ZIP)...");
                    onProgress?.Invoke(0, "Đang nén mã nguồn (ZIP)...");
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    await CreateZipWithProgressAsync(site.FullPath, zipPath, onProgress);
                }
                onProgress?.Invoke(100, $"Nén xong: {new FileInfo(zipPath).Length / 1024.0 / 1024.0:N2} MB");
                log($"✅ Nén xong: {new FileInfo(zipPath).Length / 1024.0 / 1024.0:N2} MB");
            }

            // Step 2: Export local MySQL DB from .env
            var (dbHost, dbPort, dbName, dbUser, dbPass) = ReadLocalEnv(site.FullPath);
            if (!string.IsNullOrEmpty(dbName))
            {
                log($"🗄 Đang export database `{dbName}`...");
                var (dumped, dumpErr) = await ExportDatabaseAsync(dbHost, dbPort, dbUser, dbPass, dbName, sqlPath);
                if (dumped && File.Exists(sqlPath) && new FileInfo(sqlPath).Length > 0)
                {
                    log($"✅ Export SQL xong: {new FileInfo(sqlPath).Length / 1024.0:N1} KB");
                }
                else
                {
                    try { if (File.Exists(sqlPath)) File.Delete(sqlPath); } catch { }
                    string reason = string.IsNullOrWhiteSpace(dumpErr) ? "MySQL chưa chạy hoặc sai thông tin .env" : dumpErr;
                    if (onlyDb)
                    {
                        log($"❌ Không thể export SQL (`{dbName}`): {reason}");
                        return (false, demoUrl);
                    }
                    log($"⚠️ Không thể export SQL ({reason}) — tiếp tục không kèm file SQL.");
                }
            }
            else if (onlyDb)
            {
                log("❌ Không tìm thấy tên database (DB_DATABASE) trong file .env của dự án.");
                return (false, demoUrl);
            }

            // Step 3: DirectAdmin DB create/sync
            string mainUser = server.Username.Contains('_') ? server.Username.Split('_')[0] : server.Username;
            string dbSuffix = ResolveDbSuffix(site.Name, customDbSuffix, dbNamingRule);
            string finalDbName = $"{mainUser}_{dbSuffix}";
            string finalDbPass = GenerateRandomPassword();

            if (!onlyDb)
            {
                await EnsureDirectAdminDatabaseAsync(server, mainUser, dbSuffix, finalDbName, finalDbPass, log);
            }

            // Step 4: Ensure remote FTP directory exists, then upload bridge.php, dist.zip, dist.sql
            await EnsureFtpDirectoryAsync(ftpBase, server.Username, server.Password);

            string bridgePath = PathHelper.GetDataFile("bridge.php");
            if (File.Exists(bridgePath))
            {
                log("☁ Uploading bridge.php...");
                onProgress?.Invoke(0, "Đang upload bridge.php...");
                await UploadFtpAsync(ftpBase + "bridge.php", server.Username, server.Password, bridgePath, onProgress, "bridge.php");
            }

            if (!onlyDb && File.Exists(zipPath))
            {
                log("☁ Uploading dist.zip...");
                onProgress?.Invoke(0, "Đang upload dist.zip...");
                await UploadFtpAsync(ftpBase + "dist.zip", server.Username, server.Password, zipPath, onProgress, "dist.zip");
            }

            if (File.Exists(sqlPath) && new FileInfo(sqlPath).Length > 0)
            {
                log("☁ Uploading dist.sql...");
                onProgress?.Invoke(0, "Đang upload dist.sql...");
                await UploadFtpAsync(ftpBase + "dist.sql", server.Username, server.Password, sqlPath, onProgress, "dist.sql");
            }

            // Step 5: Trigger bridge.php (follow 301/302/307/308 redirects including HTTPS<->HTTP while preserving POST)
            log($"⚡ Đang kích hoạt bridge: {bridgeUrl}");
            onProgress?.Invoke(95, "Đang chạy Bridge trên server...");

            var formDict = new Dictionary<string, string>
            {
                ["clear_db"] = "1",
                ["app_config"] = $"{{\"app_url\":\"{demoUrl}\",\"ssl\":{(useSsl ? "true" : "false")},\"skip_lock\":true}}"
            };
            if (!onlyDb)
            {
                formDict["db_config"] = $"{{\"host\":\"localhost\",\"name\":\"{finalDbName}\",\"user\":\"{finalDbName}\",\"pass\":\"{finalDbPass}\"}}";
            }

            var (statusCode, body, finalUrl) = await PostBridgeWithRedirectAsync(bridgeUrl, formDict, TimeSpan.FromMinutes(10), log);
            if (!string.IsNullOrEmpty(finalUrl) && Uri.TryCreate(finalUrl, UriKind.Absolute, out var finalUri))
                demoUrl = $"{finalUri.Scheme}://{finalUri.Authority}/{relPath}";

            bool ok = LogBridgeResponse(statusCode, body, log);

            if (!ok && onlyDb && ShouldRetryDbWithDirectAdminSync(body))
            {
                log("🔄 Không thể kết nối DB bằng .env trên host. Tự động đồng bộ lại mật khẩu trên DirectAdmin & cập nhật .env...");
                await EnsureDirectAdminDatabaseAsync(server, mainUser, dbSuffix, finalDbName, finalDbPass, log);
                await Task.Delay(2000);

                formDict["db_config"] = $"{{\"host\":\"localhost\",\"name\":\"{finalDbName}\",\"user\":\"{finalDbName}\",\"pass\":\"{finalDbPass}\"}}";
                string retryUrl = string.IsNullOrEmpty(finalUrl) ? bridgeUrl : finalUrl;
                var (retryCode, retryBody, _) = await PostBridgeWithRedirectAsync(retryUrl, formDict, TimeSpan.FromMinutes(10), log);
                ok = LogBridgeResponse(retryCode, retryBody, log);
            }

            onProgress?.Invoke(100, ok ? "Deploy hoàn tất!" : "Có lỗi từ Bridge");
            return (ok, demoUrl);
        }
        catch (Exception ex)
        {
            log($"❌ Lỗi deploy: {ex.Message}");
            return (false, demoUrl);
        }
        finally
        {
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
            try { if (File.Exists(sqlPath)) File.Delete(sqlPath); } catch { }
        }
    }

    public async Task<bool> CleanupRemoteAsync(SiteItemModel site, DemoServerModel server, string overrideDomain, bool useSsl, Action<string> log)
    {
        try
        {
            string activeDomain = !string.IsNullOrWhiteSpace(overrideDomain) ? overrideDomain.Trim() : (server.Domain ?? "").Trim();
            string relPath = site.RelativePath.Replace('\\', '/').Trim('/');
            string scheme = useSsl ? "https://" : "http://";
            string cleanupUrl = $"{scheme}{activeDomain}/{relPath}/bridge.php?action=cleanup";
            log($"🧹 Đang dọn dẹp file tạm trên {cleanupUrl}...");
            var (statusCode, body, _) = await PostBridgeWithRedirectAsync(cleanupUrl, new Dictionary<string, string>(), TimeSpan.FromSeconds(30), log);
            return LogBridgeResponse(statusCode, body, log);
        }
        catch (Exception ex)
        {
            log($"⚠️ Lỗi dọn dẹp: {ex.Message}");
            return false;
        }
    }

    private static (string Host, string Port, string Db, string User, string Pass) ReadLocalEnv(string projectDir)
    {
        string host = "127.0.0.1", port = "3306", db = "", user = "root", pass = "";
        string envPath = Path.Combine(projectDir, ".env");
        if (!File.Exists(envPath)) return (host, port, db, user, pass);

        foreach (var line in File.ReadAllLines(envPath))
        {
            var t = line.Trim();
            if (string.IsNullOrEmpty(t) || t.StartsWith('#')) continue;
            int idx = t.IndexOf('=');
            if (idx <= 0) continue;
            string k = t[..idx].Trim().ToUpperInvariant();
            string v = t[(idx + 1)..].Trim().Trim('"', '\'');
            if (k == "DB_HOST") host = v;
            else if (k == "DB_PORT") port = v;
            else if (k is "DB_DATABASE" or "DB_NAME") db = v;
            else if (k is "DB_USERNAME" or "DB_USER") user = v;
            else if (k is "DB_PASSWORD" or "DB_PASS") pass = v;
        }
        return (host, port, db, user, pass);
    }

    private static async Task<(bool Ok, string Error)> ExportDatabaseAsync(string host, string port, string user, string pass, string db, string outSql)
    {
        string dumpExe = "mysqldump";
        string mysqlDir = Path.Combine(PathHelper.BinDir, "mysql");
        if (Directory.Exists(mysqlDir))
        {
            foreach (var dir in Directory.GetDirectories(mysqlDir))
            {
                string cand = Path.Combine(dir, "bin", "mysqldump.exe");
                if (File.Exists(cand)) { dumpExe = cand; break; }
            }
        }

        string passArg = string.IsNullOrEmpty(pass) ? "" : $"-p\"{pass}\"";
        string args = $"--host={host} --port={port} --user={user} {passArg} --default-character-set=utf8mb4 --result-file=\"{outSql}\" {db}";
        return await Task.Run(() =>
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = dumpExe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                });
                if (p == null) return (false, "Không khởi chạy được mysqldump");
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit(120000);
                bool ok = p.ExitCode == 0 && File.Exists(outSql) && new FileInfo(outSql).Length > 0;
                return (ok, err.Trim());
            }
            catch (Exception ex) { return (false, ex.Message); }
        });
    }

    private static async Task<(int StatusCode, string Body, string FinalUrl)> PostBridgeWithRedirectAsync(
        string url, Dictionary<string, string> formDict, TimeSpan timeout, Action<string> log)
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var http = new HttpClient(handler) { Timeout = timeout };

        string currentUrl = url;
        for (int hop = 0; hop < 4; hop++)
        {
            var resp = await http.PostAsync(currentUrl, new FormUrlEncodedContent(formDict));
            int code = (int)resp.StatusCode;
            if (code is 301 or 302 or 303 or 307 or 308 && resp.Headers.Location != null)
            {
                Uri nextUri = resp.Headers.Location.IsAbsoluteUri
                    ? resp.Headers.Location
                    : new Uri(new Uri(currentUrl), resp.Headers.Location);
                currentUrl = nextUri.ToString();
                log($"↪ Chuyển hướng HTTP {code} → {currentUrl}");
                continue;
            }
            string body = await resp.Content.ReadAsStringAsync();
            return (code, body, currentUrl);
        }
        return (0, "{\"status\":\"error\",\"message\":\"Quá nhiều lần chuyển hướng (redirect loop)\"}", currentUrl);
    }

    private static bool LogBridgeResponse(int statusCode, string body, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            log($"📡 Bridge HTTP {statusCode}: (Không có phản hồi)");
            return statusCode is >= 200 and < 300;
        }

        try
        {
            string jsonBody = body.Trim();
            int jsonStart = jsonBody.LastIndexOf("{\"status\"", StringComparison.OrdinalIgnoreCase);
            if (jsonStart > 0) jsonBody = jsonBody[jsonStart..];

            using var doc = JsonDocument.Parse(jsonBody);
            var root = doc.RootElement;
            string status = root.TryGetProperty("status", out var sProp) ? (sProp.GetString() ?? "") : "";
            string msg = root.TryGetProperty("message", out var mProp) ? (mProp.GetString() ?? "") : "";
            bool ok = status.Equals("success", StringComparison.OrdinalIgnoreCase);

            log($"📡 Bridge HTTP {statusCode} — Trạng thái: {(ok ? "Thành công" : (string.IsNullOrEmpty(status) ? "Lỗi" : status))}");
            if (!string.IsNullOrWhiteSpace(msg))
                log($"   {(ok ? "✅" : "❌")} {msg}");

            if (root.TryGetProperty("logs", out var logsProp) && logsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in logsProp.EnumerateArray())
                {
                    string line = entry.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(line))
                        log($"   • {line}");
                }
            }
            return statusCode is >= 200 and < 300 && ok;
        }
        catch
        {
            string plain = WebUtility.HtmlDecode(Regex.Replace(body, "<[^>]+>", " "));
            plain = Regex.Replace(plain, @"\s+", " ").Trim();
            if (plain.Length > 350) plain = plain[..350] + "...";
            log($"📡 Bridge HTTP {statusCode}: {plain}");
            return false;
        }
    }

    private static bool ShouldRetryDbWithDirectAdminSync(string body)
    {
        return body.Contains("Access denied", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Unknown database", StringComparison.OrdinalIgnoreCase)
            || body.Contains("SQLSTATE[HY000]", StringComparison.OrdinalIgnoreCase)
            || body.Contains("file .env", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureDirectAdminDatabaseAsync(
        DemoServerModel server, string mainUser, string dbSuffix, string finalDbName, string finalDbPass, Action<string> log)
    {
        log($"🛠 Đang tạo/đồng bộ Database `{finalDbName}` trên DirectAdmin...");
        string daPort = string.IsNullOrWhiteSpace(server.DaPort) ? "1111" : server.DaPort.Trim();
        string daBody = $"action=create&name={Uri.EscapeDataString(dbSuffix)}&user={Uri.EscapeDataString(dbSuffix)}&passwd={Uri.EscapeDataString(finalDbPass)}&passwd2={Uri.EscapeDataString(finalDbPass)}&create=Create";
        string daRes = await CallDirectAdminApiAsync(server.Host, daPort, mainUser, server.Password, daBody, "CMD_API_DATABASES");
        if (daRes.Contains("already exists", StringComparison.OrdinalIgnoreCase) || daRes.Contains("500"))
        {
            log("  -> Database đã tồn tại, đồng bộ lại mật khẩu...");
            string modBody = $"action=modify&database={Uri.EscapeDataString(finalDbName)}&user={Uri.EscapeDataString(finalDbName)}&passwd={Uri.EscapeDataString(finalDbPass)}&passwd2={Uri.EscapeDataString(finalDbPass)}";
            await CallDirectAdminApiAsync(server.Host, daPort, mainUser, server.Password, modBody, "CMD_API_DB_USER");
        }
        else
        {
            log("✅ DirectAdmin: Đã sẵn sàng Database.");
        }
    }

    private static async Task<string> CallDirectAdminApiAsync(string host, string port, string user, string pass, string body, string endpoint)
    {
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

        foreach (var scheme in new[] { "https", "http" })
        {
            try
            {
                var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                var resp = await http.PostAsync($"{scheme}://{host}:{port}/{endpoint}", content);
                return await resp.Content.ReadAsStringAsync();
            }
            catch { }
        }
        return "error";
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rng = Random.Shared;
        var sb = new StringBuilder(12);
        for (int i = 0; i < 12; i++) sb.Append(chars[rng.Next(chars.Length)]);
        return sb.ToString();
    }

    public static string ResolveDbSuffix(string siteName, string customDbSuffix, string dbNamingRule)
    {
        string raw;
        if (!string.IsNullOrWhiteSpace(customDbSuffix))
        {
            raw = customDbSuffix.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(dbNamingRule) && dbNamingRule.Contains("Compact", StringComparison.OrdinalIgnoreCase))
        {
            var parts = siteName.Split('_', StringSplitOptions.RemoveEmptyEntries);
            raw = parts.Length >= 2 ? parts[^1] : siteName;
        }
        else
        {
            raw = siteName;
        }

        string cleaned = Regex.Replace(raw, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
        if (cleaned.Length > 16) cleaned = cleaned[..16];
        return cleaned;
    }

    private static async Task<int> Run7ZipWithProgressAsync(string exe7z, string workingDir, string zipPath, Action<double, string>? onProgress)
    {
        return await Task.Run(() =>
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe7z,
                    Arguments = $"a -tzip -mx=1 -bsp1 \"{zipPath}\" *",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            double lastPct = 0;
            p.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                var m = Regex.Match(e.Data, @"(\d{1,3})%");
                if (m.Success && double.TryParse(m.Groups[1].Value, out double pct))
                {
                    lastPct = Math.Clamp(pct, 0, 100);
                    string sizeInfo = "";
                    try { if (File.Exists(zipPath)) sizeInfo = $" ({new FileInfo(zipPath).Length / 1024.0 / 1024.0:N1} MB)"; } catch { }
                    onProgress?.Invoke(lastPct, $"Đang nén 7-Zip... {lastPct:0}%{sizeInfo}");
                }
            };

            p.Start();
            p.BeginOutputReadLine();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(300);
                if (lastPct <= 0 && File.Exists(zipPath))
                {
                    try
                    {
                        double mb = new FileInfo(zipPath).Length / 1024.0 / 1024.0;
                        if (mb > 0) onProgress?.Invoke(5, $"Đang nén 7-Zip... ({mb:N1} MB)");
                    }
                    catch { }
                }
            }

            p.WaitForExit();
            return p.ExitCode;
        });
    }

    private static async Task CreateZipWithProgressAsync(string sourceDir, string zipPath, Action<double, string>? onProgress)
    {
        await Task.Run(() =>
        {
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            long totalBytes = 0;
            foreach (var f in files)
            {
                try { totalBytes += new FileInfo(f).Length; } catch { }
            }
            if (totalBytes <= 0) totalBytes = 1;

            long processedBytes = 0;
            var lastReport = DateTime.MinValue;

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string entryName = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                try
                {
                    archive.CreateEntryFromFile(file, entryName, CompressionLevel.Fastest);
                    processedBytes += new FileInfo(file).Length;
                }
                catch { }

                if ((DateTime.UtcNow - lastReport).TotalMilliseconds > 150 || i == files.Length - 1)
                {
                    lastReport = DateTime.UtcNow;
                    double pct = Math.Clamp((double)processedBytes / totalBytes * 100.0, 0, 100);
                    onProgress?.Invoke(pct, $"Đang nén ZIP... {pct:0}% ({i + 1}/{files.Length} tệp)");
                }
            }
        });
    }

    private static async Task EnsureFtpDirectoryAsync(string ftpDirUrl, string user, string pass)
    {
        if (!Uri.TryCreate(ftpDirUrl, UriKind.Absolute, out var uri)) return;
        string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string currentPath = "";
        foreach (string seg in segments)
        {
            currentPath += "/" + seg;
            string targetUrl = $"{uri.Scheme}://{uri.Authority}{currentPath}";
            try
            {
#pragma warning disable SYSLIB0014
                var req = (FtpWebRequest)WebRequest.Create(targetUrl);
#pragma warning restore SYSLIB0014
                req.Method = WebRequestMethods.Ftp.MakeDirectory;
                req.Credentials = new NetworkCredential(user, pass);
                req.UsePassive = true;
                req.Timeout = 10000;
                using var resp = (FtpWebResponse)await req.GetResponseAsync();
            }
            catch
            {
                // Ignore 550 (directory already exists or parent level restricted)
            }
        }
    }

    private static async Task UploadFtpAsync(string ftpUrl, string user, string pass, string localFile, Action<double, string>? onProgress = null, string? displayFileName = null)
    {
#pragma warning disable SYSLIB0014
        var req = (FtpWebRequest)WebRequest.Create(ftpUrl);
#pragma warning restore SYSLIB0014
        req.Method = WebRequestMethods.Ftp.UploadFile;
        req.Credentials = new NetworkCredential(user, pass);
        req.UseBinary = true;
        req.UsePassive = true;

        long totalBytes = new FileInfo(localFile).Length;
        req.ContentLength = totalBytes;

        byte[] buffer = new byte[65536];
        long sent = 0;
        var lastReport = DateTime.MinValue;
        string labelName = displayFileName ?? Path.GetFileName(localFile);

        await using (var fs = File.OpenRead(localFile))
        await using (var stream = await req.GetRequestStreamAsync())
        {
            int read;
            while ((read = await fs.ReadAsync(buffer)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, read));
                sent += read;
                if (onProgress != null && totalBytes > 0 && ((DateTime.UtcNow - lastReport).TotalMilliseconds > 150 || sent == totalBytes))
                {
                    lastReport = DateTime.UtcNow;
                    double pct = Math.Clamp((double)sent / totalBytes * 100.0, 0, 100);
                    onProgress(pct, $"Đang upload {labelName}... {pct:0}% ({sent / 1024.0 / 1024.0:N1} / {totalBytes / 1024.0 / 1024.0:N1} MB)");
                }
            }
        }

        using var resp = (FtpWebResponse)await req.GetResponseAsync();
    }
}
