using RBWStack.Avalonia.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.Services;

public class UpdateService
{
    public const string CurrentVersion = "4.0.0";

    public async Task<GithubReleaseModel?> CheckLatestReleaseAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("RBWStack-Avalonia-Updater");
            string json = await http.GetStringAsync("https://api.github.com/repos/rambowoon/RBWStack/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var rel = new GithubReleaseModel
            {
                TagName = root.TryGetProperty("tag_name", out var t) ? (t.GetString() ?? "") : "",
                Name = root.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "",
                Body = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "",
                HtmlUrl = root.TryGetProperty("html_url", out var u) ? (u.GetString() ?? "") : "",
                PublishedAt = root.TryGetProperty("published_at", out var p) ? (p.GetString() ?? "") : ""
            };

            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    string dl = a.TryGetProperty("browser_download_url", out var d) ? (d.GetString() ?? "") : "";
                    if (dl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || dl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        rel.DownloadUrl = dl;
                        break;
                    }
                }
            }
            return rel;
        }
        catch { return null; }
    }
}

