using RBWStack.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
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
        Action<string> log)
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
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    int code = await Task.Run(() =>
                    {
                        using var p = Process.Start(new ProcessStartInfo
                        {
                            FileName = exe7z,
                            Arguments = $"a -tzip -mx=1 \"{zipPath}\" *",
                            WorkingDirectory = site.FullPath,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        p?.WaitForExit();
                        return p?.ExitCode ?? -1;
                    });
                    if (code != 0) throw new Exception($"7-Zip thoát với mã lỗi {code}");
                }
                else
                {
                    log("📦 Đang nén mã nguồn (ZIP)...");
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    await Task.Run(() => ZipFile.CreateFromDirectory(site.FullPath, zipPath, CompressionLevel.Fastest, false));
                }
                log($"✅ Nén xong: {new FileInfo(zipPath).Length / 1024.0 / 1024.0:N2} MB");
            }

            // Step 2: Export local MySQL DB from .env
            var (dbHost, dbPort, dbName, dbUser, dbPass) = ReadLocalEnv(site.FullPath);
            if (!string.IsNullOrEmpty(dbName))
            {
                log($"🗄 Đang export database `{dbName}`...");
                bool dumped = await ExportDatabaseAsync(dbHost, dbPort, dbUser, dbPass, dbName, sqlPath);
                if (dumped && File.Exists(sqlPath))
                    log($"✅ Export SQL xong: {new FileInfo(sqlPath).Length / 1024.0:N1} KB");
                else
                    log("⚠️ Không thể export SQL (tiếp tục không kèm file SQL).");
            }

            // Step 3: DirectAdmin DB create/sync
            string mainUser = server.Username.Contains('_') ? server.Username.Split('_')[0] : server.Username;
            string dbSuffix = string.IsNullOrWhiteSpace(customDbSuffix)
                ? Regex.Replace(site.Name, @"[^a-zA-Z0-9]", "").ToLowerInvariant()
                : customDbSuffix.Trim();
            if (dbSuffix.Length > 16) dbSuffix = dbSuffix[..16];
            string finalDbName = $"{mainUser}_{dbSuffix}";
            string finalDbPass = GenerateRandomPassword();

            if (!onlyDb)
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

            // Step 4: Upload bridge.php, dist.zip, dist.sql via FTP
            string bridgePath = PathHelper.GetDataFile("bridge.php");
            if (File.Exists(bridgePath))
            {
                log("☁ Uploading bridge.php...");
                await UploadFtpAsync(ftpBase + "bridge.php", server.Username, server.Password, bridgePath);
            }

            if (!onlyDb && File.Exists(zipPath))
            {
                log("☁ Uploading dist.zip...");
                await UploadFtpAsync(ftpBase + "dist.zip", server.Username, server.Password, zipPath);
            }

            if (File.Exists(sqlPath))
            {
                log("☁ Uploading dist.sql...");
                await UploadFtpAsync(ftpBase + "dist.sql", server.Username, server.Password, sqlPath);
            }

            // Step 5: Trigger bridge.php
            log($"⚡ Đang kích hoạt bridge: {bridgeUrl}");
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };

            var formDict = new Dictionary<string, string> { ["clear_db"] = "1" };
            if (!onlyDb)
            {
                formDict["db_config"] = $"{{\"host\":\"localhost\",\"name\":\"{finalDbName}\",\"user\":\"{finalDbName}\",\"pass\":\"{finalDbPass}\"}}";
                formDict["app_config"] = $"{{\"app_url\":\"{demoUrl}\",\"ssl\":{(useSsl ? "true" : "false")},\"skip_lock\":true}}";
            }

            var resp = await http.PostAsync(bridgeUrl, new FormUrlEncodedContent(formDict));
            string body = await resp.Content.ReadAsStringAsync();
            log($"📡 Bridge HTTP {(int)resp.StatusCode}: {body}");

            bool ok = resp.IsSuccessStatusCode && body.Contains("success", StringComparison.OrdinalIgnoreCase);
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

            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            var resp = await http.PostAsync(cleanupUrl, new FormUrlEncodedContent(new Dictionary<string, string>()));
            string body = await resp.Content.ReadAsStringAsync();
            log($"📡 Cleanup HTTP {(int)resp.StatusCode}: {body}");
            return resp.IsSuccessStatusCode;
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

    private static async Task<bool> ExportDatabaseAsync(string host, string port, string user, string pass, string db, string outSql)
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
                    CreateNoWindow = true
                });
                p?.WaitForExit(120000);
                return p?.ExitCode == 0 && File.Exists(outSql) && new FileInfo(outSql).Length > 0;
            }
            catch { return false; }
        });
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

    private static async Task UploadFtpAsync(string ftpUrl, string user, string pass, string localFile)
    {
#pragma warning disable SYSLIB0014
        var req = (FtpWebRequest)WebRequest.Create(ftpUrl);
#pragma warning restore SYSLIB0014
        req.Method = WebRequestMethods.Ftp.UploadFile;
        req.Credentials = new NetworkCredential(user, pass);
        req.UseBinary = true;
        req.UsePassive = true;

        byte[] data = await File.ReadAllBytesAsync(localFile);
        req.ContentLength = data.Length;
        await using var stream = await req.GetRequestStreamAsync();
        await stream.WriteAsync(data);
        using var resp = (FtpWebResponse)await req.GetResponseAsync();
    }
}
