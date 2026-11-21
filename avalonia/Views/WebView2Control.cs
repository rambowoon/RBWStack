using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core;
using RBWStack.Avalonia.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RBWStack.Avalonia.Views;

public class WebView2Control : NativeControlHost
{
    public static readonly StyledProperty<string?> HtmlContentProperty =
        AvaloniaProperty.Register<WebView2Control, string?>(nameof(HtmlContent));

    public string? HtmlContent
    {
        get => GetValue(HtmlContentProperty);
        set => SetValue(HtmlContentProperty, value);
    }

    private IntPtr _hwnd;
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _webView;
    private string _pendingHtml = "";
    private bool _initStarted;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    static WebView2Control()
    {
        HtmlContentProperty.Changed.AddClassHandler<WebView2Control>((c, e) =>
        {
            c.NavigateToHtml(e.NewValue as string ?? "");
        });
        IsVisibleProperty.Changed.AddClassHandler<WebView2Control>((c, e) =>
        {
            if (c._controller != null && e.NewValue is bool vis)
                c._controller.IsVisible = vis;
        });
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        const int WS_CHILD = 0x40000000;
        const int WS_VISIBLE = 0x10000000;
        _hwnd = CreateWindowEx(0, "STATIC", "", WS_CHILD | WS_VISIBLE, 0, 0, 100, 100, parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        _ = InitWebView2Async();
        return new PlatformHandle(_hwnd, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        try { _controller?.Close(); } catch { }
        _controller = null;
        _webView = null;
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        UpdateBounds(size);
        return size;
    }

    private void UpdateBounds(Size size)
    {
        if (_controller == null) return;
        double scale = VisualRoot?.RenderScaling ?? 1.0;
        int w = Math.Max(1, (int)(size.Width * scale));
        int h = Math.Max(1, (int)(size.Height * scale));
        _controller.Bounds = new System.Drawing.Rectangle(0, 0, w, h);
    }

    private async System.Threading.Tasks.Task InitWebView2Async()
    {
        if (_initStarted || _hwnd == IntPtr.Zero) return;
        _initStarted = true;
        try
        {
            string userDataFolder = Path.Combine(PathHelper.TmpDir, "webview2_mail");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            if (_hwnd == IntPtr.Zero) return;
            _controller = await env.CreateCoreWebView2ControllerAsync(_hwnd);
            _webView = _controller.CoreWebView2;
            _controller.IsVisible = true;
            UpdateBounds(Bounds.Size);

            string html = string.IsNullOrEmpty(HtmlContent) ? _pendingHtml : HtmlContent;
            if (!string.IsNullOrEmpty(html))
                _webView.NavigateToString(html);
        }
        catch { }
    }

    private void NavigateToHtml(string html)
    {
        _pendingHtml = html;
        try
        {
            if (_webView != null)
                _webView.NavigateToString(string.IsNullOrEmpty(html) ? "<html><body></body></html>" : html);
        }
        catch { }
    }
}
