using RBWStack.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RBWStack.Avalonia.Services;

public class MailSandboxService
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    public event Action<CaughtEmailModel>? EmailReceived;
    public bool IsRunning => _listener != null;
    public int Port { get; private set; } = 2525;

    public List<CaughtEmailModel> LoadEmails()
    {
        try
        {
            string path = PathHelper.GetDataFile("caught_emails.json");
            if (File.Exists(path))
                return JsonSerializer.Deserialize<List<CaughtEmailModel>>(File.ReadAllText(path)) ?? new();
        }
        catch { }
        return new();
    }

    public void SaveEmails(List<CaughtEmailModel> emails)
    {
        try
        {
            File.WriteAllText(PathHelper.GetDataFile("caught_emails.json"),
                JsonSerializer.Serialize(emails, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public void Start(int port = 2525)
    {
        if (_listener != null) return;
        Port = port;
        _cts = new CancellationTokenSource();
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _ = AcceptLoopAsync(_cts.Token);
        }
        catch
        {
            _listener = null;
        }
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _listener = null;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client);
            }
            catch { break; }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        await using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        await using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
        {
            await writer.WriteLineAsync("220 RBWStack Mail Sandbox Ready");
            string from = "", to = "";
            var rawSb = new StringBuilder();
            bool inData = false;

            while (true)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null) break;

                if (inData)
                {
                    if (line == ".")
                    {
                        inData = false;
                        await writer.WriteLineAsync("250 OK: Message caught");
                        var mail = ParseEmail(from, to, rawSb.ToString());
                        EmailReceived?.Invoke(mail);
                        rawSb.Clear();
                    }
                    else
                    {
                        rawSb.AppendLine(line);
                    }
                    continue;
                }

                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                    await writer.WriteLineAsync("250 Hello");
                else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
                {
                    from = line[10..].Trim(' ', '<', '>');
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                {
                    to = line[8..].Trim(' ', '<', '>');
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    inData = true;
                    await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 Bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        }
    }

    private static CaughtEmailModel ParseEmail(string from, string to, string raw)
    {
        var subjMatch = Regex.Match(raw, @"^Subject:\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        int bodyIdx = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (bodyIdx < 0) bodyIdx = raw.IndexOf("\n\n", StringComparison.Ordinal);
        string body = bodyIdx >= 0 ? raw[(bodyIdx + 2)..].Trim() : raw;

        // Extract HTML part from multipart MIME if present
        var htmlPartMatch = Regex.Match(raw, @"Content-Type:\s*text/html[^\r\n]*(?:\r?\n[^\r\n]+)*?\r?\n\r?\n([\s\S]*?)(?:\r?\n--[a-zA-Z0-9_=\-\.]+|$)", RegexOptions.IgnoreCase);
        if (htmlPartMatch.Success)
        {
            body = htmlPartMatch.Groups[1].Value.Trim();
            if (htmlPartMatch.Value.Contains("quoted-printable", StringComparison.OrdinalIgnoreCase))
                body = DecodeQuotedPrintable(body);
            else if (htmlPartMatch.Value.Contains("base64", StringComparison.OrdinalIgnoreCase))
                body = TryDecodeBase64(body);
        }
        else if (raw.Contains("Content-Transfer-Encoding: quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            body = DecodeQuotedPrintable(body);
        }
        else if (raw.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
        {
            body = TryDecodeBase64(body);
        }

        return new CaughtEmailModel
        {
            Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            From = string.IsNullOrEmpty(from) ? "local@rbwstack" : from,
            To = string.IsNullOrEmpty(to) ? "dev@rbwstack" : to,
            Subject = subjMatch.Success ? subjMatch.Groups[1].Value.Trim() : "(Không có tiêu đề)",
            Body = body,
            Raw = raw
        };
    }

    private static string DecodeQuotedPrintable(string input)
    {
        string cleaned = Regex.Replace(input, @"=\r?\n", "");
        var bytes = new List<byte>();
        for (int i = 0; i < cleaned.Length; i++)
        {
            if (cleaned[i] == '=' && i + 2 < cleaned.Length &&
                byte.TryParse(cleaned.Substring(i + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                bytes.Add(b);
                i += 2;
            }
            else
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(new[] { cleaned[i] }));
            }
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static string TryDecodeBase64(string input)
    {
        try
        {
            string clean = Regex.Replace(input, @"\s+", "");
            return Encoding.UTF8.GetString(Convert.FromBase64String(clean));
        }
        catch { return input; }
    }
}

