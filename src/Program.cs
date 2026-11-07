using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Threading;

namespace RBWStack
{
    static class Program
    {
        private static System.Threading.Mutex mutex;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int HWND_BROADCAST = 0xffff;

        private static IntPtr GetOriginalInstanceHandle()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLower().TrimEnd('\\', '/');
                string cleanPath = Regex.Replace(baseDir, @"[^a-zA-Z0-9]", "_");
                if (cleanPath.Length > 200) cleanPath = cleanPath.Substring(cleanPath.Length - 200);
                string regPath = @"SOFTWARE\RBWStack\" + cleanPath;

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("MainWindowHandle");
                        if (val != null)
                        {
                            long handleVal = Convert.ToInt64(val);
                            return new IntPtr(handleVal);
                        }
                    }
                }
            }
            catch { }
            return IntPtr.Zero;
        }

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            ConfigHelper.MigrateFilesToDataFolder();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLower().TrimEnd('\\', '/');
            string cleanPath = Regex.Replace(baseDir, @"[^a-zA-Z0-9]", "_");
            if (cleanPath.Length > 50) cleanPath = cleanPath.Substring(cleanPath.Length - 50);
            string mutexName = "RBWStackMutex_" + cleanPath;

            mutex = new System.Threading.Mutex(true, mutexName);

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                IntPtr hWnd = GetOriginalInstanceHandle();
                if (hWnd != IntPtr.Zero)
                {
                    uint msg = RegisterWindowMessage("RBWSTACK_RESTORE_INSTANCE");
                    if (msg != 0)
                    {
                        PostMessage(hWnd, msg, IntPtr.Zero, IntPtr.Zero);
                        return;
                    }
                }

                // Fallback to broadcast
                uint msgBroadcast = RegisterWindowMessage("RBWSTACK_RESTORE_INSTANCE");
                if (msgBroadcast != 0)
                {
                    PostMessage((IntPtr)HWND_BROADCAST, msgBroadcast, IntPtr.Zero, IntPtr.Zero);
                }
                return;
            }

            // Enable TLS 1.2 and TLS 1.3 globally for all network connections in this app
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)12288 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
            catch { }

            Application.ThreadException += (sender, ev) => {
                try {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "crash_log.txt"), ev.Exception.ToString());
                } catch {}
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
