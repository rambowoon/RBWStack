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
    public static class ConfigHelper
    {
        public static string GetDataFilePath(string filename)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return Path.Combine(dir, filename);
        }

        public static void MigrateFilesToDataFolder()
        {
            try
            {
                string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                string[] filesToMigrate = new string[]
                {
                    "demo_global_config.json",
                    "demo_deploy_status.json",
                    "demo_ssl_config.json",
                    "demo_db_config.json",
                    "sites.json",
                    "ssl.json",
                    "tunnels.json",
                    "sites_root.txt"
                };

                foreach (string file in filesToMigrate)
                {
                    string src = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                    if (File.Exists(src))
                    {
                        if (!Directory.Exists(dataDir))
                        {
                            Directory.CreateDirectory(dataDir);
                        }
                        string dest = Path.Combine(dataDir, file);
                        if (!File.Exists(dest))
                        {
                            File.Move(src, dest);
                        }
                        else
                        {
                            File.Delete(src);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
