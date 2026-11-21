using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RBWStack.Avalonia.Services;

public sealed class Win32TrayService : IDisposable
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSW
    {
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassW(ref WNDCLASSW lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImageW(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(int dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    private IntPtr _hwnd;
    private IntPtr _hIcon;
    private WndProcDelegate? _wndProc;
    private NOTIFYICONDATAW _nid;

    public event Action? LeftClicked;
    public event Action? RightClicked;

    public Win32TrayService(string tooltip, string? iconFilePath)
    {
        _wndProc = WndProc;
        IntPtr hInst = GetModuleHandleW(null);
        string className = "RBWStackTrayMsg_" + Guid.NewGuid().ToString("N");

        var wc = new WNDCLASSW
        {
            lpfnWndProc = _wndProc,
            hInstance = hInst,
            lpszClassName = className
        };
        RegisterClassW(ref wc);

        _hwnd = CreateWindowExW(0, className, "", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hInst, IntPtr.Zero);

        if (!string.IsNullOrEmpty(iconFilePath) && File.Exists(iconFilePath))
            _hIcon = LoadImageW(IntPtr.Zero, iconFilePath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE | LR_DEFAULTSIZE);

        _nid = new NOTIFYICONDATAW
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _hIcon,
            szTip = tooltip.Length > 127 ? tooltip[..127] : tooltip
        };
        Shell_NotifyIconW(NIM_ADD, ref _nid);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            int mouseMsg = (int)(lParam.ToInt64() & 0xFFFF);
            if (mouseMsg == WM_RBUTTONUP)
                RightClicked?.Invoke();
            else if (mouseMsg == WM_LBUTTONUP || mouseMsg == WM_LBUTTONDBLCLK)
                LeftClicked?.Invoke();
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            Shell_NotifyIconW(NIM_DELETE, ref _nid);
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
        _wndProc = null;
    }
}

