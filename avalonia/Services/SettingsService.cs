using Microsoft.Win32;
using RBWStack.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RBWStack.Avalonia.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettingsModel LoadSettings()
    {
        var model = new AppSettingsModel();
        try
        {
            string path = PathHelper.GetDataFile("avalonia_settings.json");
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<AppSettingsModel>(File.ReadAllText(path));
                if (loaded != null) model = loaded;
            }
            else
            {
                // Migrate from legacy demo_global_config.json if present
                string legacyPath = PathHelper.GetDataFile("demo_global_config.json");
                if (File.Exists(legacyPath))
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(legacyPath));
                    if (dict != null)
                    {
                        if (dict.TryGetValue("vhost_suffix", out var suffix) && !string.IsNullOrWhiteSpace(suffix))
                            model.VHostSuffix = suffix.StartsWith(".") ? suffix : "." + suffix;
                    }
                }
            }

            // Document root from sites_root.txt
            string sitesRootFile = PathHelper.GetDataFile("sites_root.txt");
            if (File.Exists(sitesRootFile))
            {
                string r = File.ReadAllText(sitesRootFile).Trim();
                if (!string.IsNullOrEmpty(r) && Directory.Exists(r))
                    model.DocumentRoot = r;
            }
            if (string.IsNullOrEmpty(model.DocumentRoot))
            {
                model.DocumentRoot = PathHelper.WwwDir;
                Directory.CreateDirectory(model.DocumentRoot);
            }
        }
        catch { }

        return model;
    }

    public void SaveSettings(AppSettingsModel settings)
    {
        try
        {
            string path = PathHelper.GetDataFile("avalonia_settings.json");
            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOpts));

            if (!string.IsNullOrWhiteSpace(settings.DocumentRoot))
                File.WriteAllText(PathHelper.GetDataFile("sites_root.txt"), settings.DocumentRoot);

            ApplyStartupRegistry(settings.StartWithWindows);
        }
        catch { }
    }

    public List<DemoServerModel> LoadDemoServers()
    {
        try
        {
            string path = PathHelper.GetDataFile("demo_servers.json");
            if (File.Exists(path))
            {
                string raw = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<DemoServerModel>>(raw, JsonOpts);
                if (list != null && list.Count > 0)
                {
                    // Also map legacy snake_case keys (web_domain, ftp_host, ftp_user, ftp_pass, da_port, ftp_root) if present
                    var rawDicts = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(raw, JsonOpts);
                    if (rawDicts != null)
                    {
                        for (int i = 0; i < Math.Min(list.Count, rawDicts.Count); i++)
                        {
                            var d = rawDicts[i];
                            if (string.IsNullOrEmpty(list[i].Domain) && d.TryGetValue("web_domain", out var wd)) list[i].Domain = wd.ToString();
                            if (string.IsNullOrEmpty(list[i].Host) && d.TryGetValue("ftp_host", out var fh)) list[i].Host = fh.ToString();
                            if (string.IsNullOrEmpty(list[i].Username) && d.TryGetValue("ftp_user", out var fu)) list[i].Username = fu.ToString();
                            if (string.IsNullOrEmpty(list[i].Password) && d.TryGetValue("ftp_pass", out var fp)) list[i].Password = fp.ToString();
                            if (string.IsNullOrEmpty(list[i].FtpRoot) && d.TryGetValue("ftp_root", out var fr)) list[i].FtpRoot = fr.ToString();
                            if ((string.IsNullOrEmpty(list[i].DaPort) || list[i].DaPort == "1111") && d.TryGetValue("da_port", out var dp)) list[i].DaPort = dp.ToString();
                            if (!list[i].IsDefault && d.TryGetValue("is_default", out var isd) && isd.ValueKind == JsonValueKind.True) list[i].IsDefault = true;
                        }
                    }
                    return list;
                }
            }

            // Fallback: import legacy demo_global_config.json as default server
            string legacyPath = PathHelper.GetDataFile("demo_global_config.json");
            if (File.Exists(legacyPath))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(legacyPath));
                if (dict != null && dict.TryGetValue("ftp_host", out var host) && !string.IsNullOrWhiteSpace(host))
                {
                    return new List<DemoServerModel>
                    {
                        new DemoServerModel
                        {
                            Name = "Server Mặc Định",
                            IsDefault = true,
                            Host = host,
                            Domain = dict.TryGetValue("web_domain", out var dom) ? dom : "",
                            Username = dict.TryGetValue("ftp_user", out var usr) ? usr : "",
                            Password = dict.TryGetValue("ftp_pass", out var pwd) ? pwd : "",
                            DaPort = dict.TryGetValue("da_port", out var port) ? port : "1111",
                            FtpRoot = dict.TryGetValue("ftp_root", out var root) ? root : "/domains/{domain}/public_html"
                        }
                    };
                }
            }
        }
        catch { }

        return new List<DemoServerModel>
        {
            new DemoServerModel
            {
                Name = "PHP 8.5",
                IsDefault = true,
                Host = "",
                Domain = "",
                Username = "",
                Password = "",
                DaPort = "1111",
                FtpRoot = "/domains/{domain}/public_html"
            }
        };
    }

    public void SaveDemoServers(IEnumerable<DemoServerModel> servers)
    {
        try
        {
            var list = servers.ToList();
            File.WriteAllText(PathHelper.GetDataFile("demo_servers.json"), JsonSerializer.Serialize(list, JsonOpts));

            // Keep legacy demo_global_config.json in sync with default server
            var def = list.FirstOrDefault(s => s.IsDefault) ?? list.FirstOrDefault();
            if (def != null)
            {
                string legacyPath = PathHelper.GetDataFile("demo_global_config.json");
                var dict = File.Exists(legacyPath)
                    ? (JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(legacyPath)) ?? new())
                    : new Dictionary<string, string>();
                dict["ftp_host"] = def.Host;
                dict["ftp_user"] = def.Username;
                dict["ftp_pass"] = def.Password;
                dict["web_domain"] = def.Domain;
                dict["ftp_root"] = def.FtpRoot;
                dict["da_user"] = def.Username;
                dict["da_port"] = def.DaPort;
                File.WriteAllText(legacyPath, JsonSerializer.Serialize(dict, JsonOpts));
            }
        }
        catch { }
    }

    public Dictionary<string, string> LoadJsonDict(string fileName)
    {
        try
        {
            string path = PathHelper.GetDataFile(fileName);
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
        }
        catch { }
        return new();
    }

    public void SaveJsonDict(string fileName, Dictionary<string, string> data)
    {
        try
        {
            File.WriteAllText(PathHelper.GetDataFile(fileName), JsonSerializer.Serialize(data, JsonOpts));
        }
        catch { }
    }

    private void ApplyStartupRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue("RBWStackAvalonia", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("RBWStackAvalonia", false);
            }
        }
        catch { }
    }
}

