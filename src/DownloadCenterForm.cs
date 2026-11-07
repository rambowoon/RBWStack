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
    public class DownloadCenterForm : Form
    {
        private Color colorBg = Color.FromArgb(18, 20, 30);
        private Color colorCard = Color.FromArgb(30, 32, 46);
        private Color colorText = Color.FromArgb(255, 255, 255);
        private Color colorTextDim = Color.FromArgb(168, 170, 190);
        private Color colorGreen = Color.FromArgb(22, 198, 98);
        private Color colorAccent = Color.FromArgb(0, 120, 212);

        private Panel pnlHeader;
        private Label lblTitle;
        private Label btnClose;

        private ProgressBar pbDownload;
        private Label lblStatusText;
        private WebClient webClient = null;

        private List<DownloadRow> rowsList = new List<DownloadRow>();

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public DownloadCenterForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(620, 610);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = colorBg;

            // Header panel
            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 45;
            pnlHeader.BackColor = Color.Transparent;
            pnlHeader.MouseDown += Header_MouseDown;
            pnlHeader.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (System.Drawing.Drawing2D.LinearGradientBrush hBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    pnlHeader.ClientRectangle,
                    Color.FromArgb(0, 120, 212),
                    Color.FromArgb(0, 84, 168),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(hBrush, pnlHeader.ClientRectangle);
                }
                using (Pen pen = new Pen(Color.FromArgb(50, 255, 255, 255), 1f))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            lblTitle = new Label();
            lblTitle.Text = "⬇  KHO TảI & CÀI ĐẶT TỰ ĐỘNG";
            lblTitle.ForeColor = Color.White;
            lblTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(15, 12);
            pnlHeader.Controls.Add(lblTitle);

            btnClose = new Label();
            btnClose.Text = "×";
            btnClose.ForeColor = colorTextDim;
            btnClose.Font = new Font("Segoe UI", 20f);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Size = new Size(35, 35);
            btnClose.Location = new Point(580, 5);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.Click += (s, e) => {
                if (webClient != null && webClient.IsBusy)
                {
                    if (MessageBox.Show("Đang tải dữ liệu, bạn có chắc chắn muốn hủy?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        webClient.CancelAsync();
                        this.Close();
                    }
                }
                else
                {
                    this.Close();
                }
            };
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            SetupDownloadRows();

            // Render Items Rows
            int yOffset = 60;
            foreach (var row in rowsList)
            {
                Panel pnlRow = new Panel();
                pnlRow.Size = new Size(580, 64);
                pnlRow.Location = new Point(20, yOffset);
                pnlRow.BackColor = Color.FromArgb(32, 32, 40);
                pnlRow.Paint += DrawCardBorder;
                this.Controls.Add(pnlRow);
                ApplyRoundedRegion(pnlRow, 12);

                Label lblName = new Label();
                lblName.Text = row.ComponentName;
                lblName.ForeColor = colorText;
                lblName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblName.Location = new Point(15, 6);
                lblName.AutoSize = true;
                pnlRow.Controls.Add(lblName);

                ComboBox cb = new NoScrollComboBox();
                cb.BackColor = colorBg;
                cb.ForeColor = colorText;
                cb.FlatStyle = FlatStyle.Flat;
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
                cb.Location = new Point(15, 29);
                cb.Size = new Size(185, 23);
                foreach (var opt in row.Options)
                {
                    cb.Items.Add(opt);
                }
                row.ComboSelector = cb;
                pnlRow.Controls.Add(cb);

                Label lblDesc = new Label();
                lblDesc.ForeColor = colorTextDim;
                lblDesc.Font = new Font("Segoe UI", 8.25f, FontStyle.Italic);
                lblDesc.Location = new Point(210, 32);
                lblDesc.Size = new Size(240, 20);
                row.LabelDesc = lblDesc;
                pnlRow.Controls.Add(lblDesc);

                DownloadRow capturedRow = row;
                cb.SelectedIndexChanged += (s, e) => {
                    VersionOption opt = cb.SelectedItem as VersionOption;
                    if (opt != null)
                    {
                        capturedRow.LabelDesc.Text = "Lưu tại: .\\" + capturedRow.BaseDestFolder + "\\" + opt.FolderName;
                    }
                };
                cb.SelectedIndex = 0;

                ModernButton btnDownload = new ModernButton();
                btnDownload.Text = "TẢI & CÀI ĐẶT";
                btnDownload.NormalColor = colorAccent;
                btnDownload.HoverColor = Color.FromArgb(71, 82, 196);
                btnDownload.PressedColor = Color.FromArgb(58, 66, 159);
                btnDownload.Location = new Point(455, 15);
                btnDownload.Size = new Size(110, 32);
                btnDownload.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                
                btnDownload.Click += (s, e) => StartDownloadFlow(capturedRow);
                pnlRow.Controls.Add(btnDownload);

                yOffset += 73;
            }

            // Bottom Area for Progress info
            Panel pnlProgress = new Panel();
            pnlProgress.Size = new Size(580, 85);
            pnlProgress.Location = new Point(20, 505);
            pnlProgress.BackColor = Color.FromArgb(32, 32, 40);
            pnlProgress.Paint += DrawCardBorder;
            this.Controls.Add(pnlProgress);
            ApplyRoundedRegion(pnlProgress, 12);

            lblStatusText = new Label();
            lblStatusText.Text = "Đang quét các phiên bản mới nhất trực tuyến...";
            lblStatusText.ForeColor = colorText;
            lblStatusText.Font = new Font("Segoe UI", 9.25f);
            lblStatusText.Location = new Point(15, 15);
            lblStatusText.Size = new Size(550, 20);
            pnlProgress.Controls.Add(lblStatusText);

            pbDownload = new ProgressBar();
            pbDownload.Location = new Point(15, 45);
            pbDownload.Size = new Size(550, 25);
            pbDownload.Style = ProgressBarStyle.Blocks;
            pnlProgress.Controls.Add(pbDownload);

            // Force creation of handle to make Invoke safe on background threads
            var forceHandle = this.Handle;

            // Fetch Online Web Scraper in Background
            LoadLatestVersionsFromWeb();
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(18, 20, 30),
                Color.FromArgb(26, 28, 44),
                90f))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            using (Pen pen = new Pen(Color.FromArgb(35, 100, 150, 255), 1.0f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void DrawCardBorder(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            if (p != null)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radius = 12;
                using (Pen pen = new Pen(Color.FromArgb(35, 120, 180, 255), 1.0f))
                {
                    pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                    using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), radius))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                // Top glow line
                using (Pen topPen = new Pen(Color.FromArgb(50, 160, 210, 255), 1f))
                    e.Graphics.DrawLine(topPen, radius, 0, p.Width - radius, 0);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ApplyRoundedRegion(Control control, int radius)
        {
            try
            {
                control.Region = new Region(GetRoundedRectPath(new Rectangle(0, 0, control.Width, control.Height), radius));
            }
            catch { }
        }

        private void SetupDownloadRows()
        {
            // Offline robust fallback defaults
            // 1. PHP Row
            var phpRow = new DownloadRow
            {
                ComponentName = "PHP Engine (x64 Thread Safe)",
                BaseDestFolder = @"bin\php",
                ZipFileName = "php_temp.zip",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "PHP 8.3.6 (Mới nhất)", Url = "https://windows.php.net/downloads/releases/php-8.3.6-Win32-vs16-x64.zip", FolderName = "php-8.3.6" },
                    new VersionOption { DisplayName = "PHP 8.2.12 (Ổn định)", Url = "https://windows.php.net/downloads/releases/php-8.2.12-Win32-vs16-x64.zip", FolderName = "php-8.2.12" },
                    new VersionOption { DisplayName = "PHP 8.1.28 (Tương thích)", Url = "https://windows.php.net/downloads/releases/php-8.1.28-Win32-vs16-x64.zip", FolderName = "php-8.1.28" },
                    new VersionOption { DisplayName = "PHP 7.4.33 (Kế thừa cũ)", Url = "https://windows.php.net/downloads/releases/archives/php-7.4.33-Win32-vc15-x64.zip", FolderName = "php-7.4.33" },
                    new VersionOption { DisplayName = "PHP 5.6.40 (Kế thừa rất cũ)", Url = "https://windows.php.net/downloads/releases/archives/php-5.6.40-Win32-VC11-x64.zip", FolderName = "php-5.6.40" }
                },
                PostExtractAction = (destFolder) => ConfigurePHP(destFolder)
            };
            rowsList.Add(phpRow);

            // 2. Apache Row
            var apacheRow = new DownloadRow
            {
                ComponentName = "Apache Web Server (httpd)",
                BaseDestFolder = @"bin\apache",
                ZipFileName = "apache_temp.zip",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "Apache 2.4.67 (VS18 Mới nhất)", Url = "https://www.apachelounge.com/download/VS18/binaries/httpd-2.4.67-260504-Win64-VS18.zip", FolderName = "httpd-2.4.67" },
                    new VersionOption { DisplayName = "Apache 2.4.59 (VS17)", Url = "https://www.apachelounge.com/download/VS17/binaries/httpd-2.4.59-win64-VS17.zip", FolderName = "httpd-2.4.59" }
                },
                PostExtractAction = (destFolder) => {
                    string currentPort = "80";
                    string phpDir = null;
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is MainForm)
                        {
                            currentPort = ((MainForm)f).GetWebPort();
                            string pathExe = ((MainForm)f).pathPhpExe;
                            if (!string.IsNullOrEmpty(pathExe))
                            {
                                phpDir = Path.GetDirectoryName(pathExe);
                            }
                            break;
                        }
                    }
                    ConfigureApache(destFolder, currentPort, phpDir);
                }
            };
            rowsList.Add(apacheRow);

            // 3. Nginx Row
            var nginxRow = new DownloadRow
            {
                ComponentName = "Nginx Web Server",
                BaseDestFolder = @"bin\nginx",
                ZipFileName = "nginx_temp.zip",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "Nginx 1.26.0 (Stable)", Url = "https://nginx.org/download/nginx-1.26.0.zip", FolderName = "nginx-1.26.0" },
                    new VersionOption { DisplayName = "Nginx 1.24.0 (Legacy)", Url = "https://nginx.org/download/nginx-1.24.0.zip", FolderName = "nginx-1.24.0" }
                },
                PostExtractAction = (destFolder) => {
                    string currentPort = "80";
                    string phpDir = null;
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is MainForm)
                        {
                            currentPort = ((MainForm)f).GetWebPort();
                            string pathExe = ((MainForm)f).pathPhpExe;
                            if (!string.IsNullOrEmpty(pathExe))
                            {
                                phpDir = Path.GetDirectoryName(pathExe);
                            }
                            break;
                        }
                    }
                    ConfigureNginx(destFolder, currentPort, phpDir);
                }
            };
            rowsList.Add(nginxRow);

            // 4. Database Row
            var mysqlRow = new DownloadRow
            {
                ComponentName = "Cơ sở dữ liệu (MySQL / MariaDB)",
                BaseDestFolder = @"bin\mysql",
                ZipFileName = "mysql_temp.zip",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "MariaDB 11.3.2 (Stable x64)", Url = "https://archive.mariadb.org/mariadb-11.3.2/winx64-packages/mariadb-11.3.2-winx64.zip", FolderName = "mariadb-11.3.2" },
                    new VersionOption { DisplayName = "MariaDB 10.11.2 (LTS x64)", Url = "https://archive.mariadb.org/mariadb-10.11.2/winx64-packages/mariadb-10.11.2-winx64.zip", FolderName = "mariadb-10.11.2" },
                    new VersionOption { DisplayName = "MySQL 8.0.36 (Oracle Community)", Url = "https://downloads.mysql.com/archives/get/p/23/file/mysql-8.0.36-winx64.zip", FolderName = "mysql-8.0.36" },
                    new VersionOption { DisplayName = "MySQL 5.7.44 (Oracle Classic)", Url = "https://downloads.mysql.com/archives/get/p/23/file/mysql-5.7.44-winx64.zip", FolderName = "mysql-5.7.44" }
                },
                PostExtractAction = (destFolder) => {
                    string currentPort = "3306";
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is MainForm)
                        {
                            currentPort = ((MainForm)f).GetMySqlPort();
                            break;
                        }
                    }
                    ConfigureMySQL(destFolder, currentPort);
                }
            };
            rowsList.Add(mysqlRow);

            // 5. phpMyAdmin Row
            var pmaRow = new DownloadRow
            {
                ComponentName = "phpMyAdmin Web Database Client",
                BaseDestFolder = @"",
                ZipFileName = "pma_temp.zip",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "phpMyAdmin 5.2.1", Url = "https://files.phpmyadmin.net/phpMyAdmin/5.2.1/phpMyAdmin-5.2.1-all-languages.zip", FolderName = "phpmyadmin" },
                    new VersionOption { DisplayName = "phpMyAdmin 4.9.11 (Cho PHP cũ)", Url = "https://files.phpmyadmin.net/phpMyAdmin/4.9.11/phpMyAdmin-4.9.11-all-languages.zip", FolderName = "phpmyadmin" }
                },
                PostExtractAction = (destFolder) => ConfigurePhpMyAdmin(destFolder)
            };
            rowsList.Add(pmaRow);

            // 6. Microsoft VC++ Redistributable Row
            var vcRow = new DownloadRow
            {
                ComponentName = "Microsoft Visual C++ Runtime (Thư viện nền bắt buộc)",
                BaseDestFolder = @"downloads",
                ZipFileName = "vc_redist.x64.exe",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "Visual C++ 2015-2022 (x64)", Url = "https://aka.ms/vs/17/release/vc_redist.x64.exe", FolderName = "" }
                },
                PostExtractAction = null
            };
            rowsList.Add(vcRow);

            // 7. Cloudflare Tunnel
            var cloudflaredRow = new DownloadRow
            {
                ComponentName = "Cloudflare Tunnel",
                BaseDestFolder = @"bin\cloudflared",
                ZipFileName = "cloudflared.exe",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "Cloudflared (Stable x64)", Url = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe", FolderName = "" }
                },
                PostExtractAction = null
            };
            rowsList.Add(cloudflaredRow);
        }

        // --- Web Scraper of Latest Releases (Dynamic & Non-Blocking) ---
        private void LoadLatestVersionsFromWeb()
        {
            ThreadPool.QueueUserWorkItem(state => {
                try
                {
                    // Enforce TLS 1.2 to prevent handshake issues
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                    List<VersionOption> webPhp = ScrapePhp();
                    List<VersionOption> webApache = ScrapeApache();
                    List<VersionOption> webNginx = ScrapeNginx();
                    List<VersionOption> webMaria = ScrapeMariaDB();
                    List<VersionOption> webPma = ScrapePma();

                    // Luôn thêm sẵn Oracle MySQL vào danh sách tải động để người dùng có đầy đủ lựa chọn
                    if (webMaria != null)
                    {
                        webMaria.Add(new VersionOption { DisplayName = "MySQL 8.0.36 (Oracle Community)", Url = "https://downloads.mysql.com/archives/get/p/23/file/mysql-8.0.36-winx64.zip", FolderName = "mysql-8.0.36" });
                        webMaria.Add(new VersionOption { DisplayName = "MySQL 5.7.44 (Oracle Classic)", Url = "https://downloads.mysql.com/archives/get/p/23/file/mysql-5.7.44-winx64.zip", FolderName = "mysql-5.7.44" });
                    }

                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.Invoke(new MethodInvoker(() => {
                            UpdateDropdowns(webPhp, webApache, webNginx, webMaria, webPma);
                            lblStatusText.Text = "Đã đồng bộ thành công toàn bộ phiên bản mới nhất từ trang chủ các dịch vụ!";
                        }));
                    }
                }
                catch (Exception)
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.Invoke(new MethodInvoker(() => {
                            lblStatusText.Text = "Không có kết nối Internet / Trang chủ đổi định dạng. Sử dụng danh sách phiên bản offline có sẵn.";
                        }));
                    }
                }
            });
        }

        private List<VersionOption> ScrapePhp()
        {
            List<VersionOption> list = new List<VersionOption>();
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                // Query directory listing directly to get ALL versions including 8.4, 8.5
                string html = wc.DownloadString("https://windows.php.net/downloads/releases/");
                
                MatchCollection matches = Regex.Matches(html, @"href=""(php-(\d+\.\d+\.\d+)(?:-\d+)?-Win32-(?:vs\d+|vc\d+)-x64\.zip)""", RegexOptions.IgnoreCase);
                HashSet<string> added = new HashSet<string>();
                
                List<VersionOption> temp = new List<VersionOption>();
                foreach (Match m in matches)
                {
                    string filename = m.Groups[1].Value;
                    string version = m.Groups[2].Value;

                    if (added.Contains(version)) continue;
                    added.Add(version);

                    temp.Add(new VersionOption
                    {
                        DisplayName = "PHP " + version + " (Thread Safe x64)",
                        Url = "https://windows.php.net/downloads/releases/" + filename,
                        FolderName = "php-" + version
                    });
                }
                
                // Sort descending safely
                temp.Sort((a, b) => {
                    try
                    {
                        string verA = a.FolderName.Replace("php-", "");
                        string verB = b.FolderName.Replace("php-", "");
                        Version va = new Version(verA);
                        Version vb = new Version(verB);
                        return vb.CompareTo(va);
                    }
                    catch { return b.FolderName.CompareTo(a.FolderName); }
                });

                // Take top 6 latest versions (e.g. 8.5.x, 8.4.x, 8.3.x, 8.2.x)
                for (int i = 0; i < Math.Min(temp.Count, 8); i++)
                {
                    list.Add(temp[i]);
                }

                // Luôn thêm PHP 7.4 và PHP 5.6 vào cuối danh sách tải
                list.Add(new VersionOption { DisplayName = "PHP 7.4.33 (Kế thừa cũ)", Url = "https://windows.php.net/downloads/releases/archives/php-7.4.33-Win32-vc15-x64.zip", FolderName = "php-7.4.33" });
                list.Add(new VersionOption { DisplayName = "PHP 5.6.40 (Kế thừa rất cũ)", Url = "https://windows.php.net/downloads/releases/archives/php-5.6.40-Win32-VC11-x64.zip", FolderName = "php-5.6.40" });
            }
            return list;
        }

        private List<VersionOption> ScrapeApache()
        {
            List<VersionOption> list = new List<VersionOption>();
            using (WebClient wc = new WebClient())
            {
                wc.Proxy = null;
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                wc.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                wc.Headers.Add("Referer", "https://www.apachelounge.com/");
                string html = wc.DownloadString("https://www.apachelounge.com/download/");
                
                // Scrape httpd win64 zip files
                MatchCollection matches = Regex.Matches(html, @"href=""(/download/(?:VS\d+)/binaries/(httpd-([\d\.]+)[^""']*-Win64-VS\d+\.zip))""", RegexOptions.IgnoreCase);
                HashSet<string> added = new HashSet<string>();
                foreach (Match m in matches)
                {
                    string relativeUrl = m.Groups[1].Value;
                    string zipName = m.Groups[2].Value;
                    string version = m.Groups[3].Value;

                    if (added.Contains(version)) continue;
                    added.Add(version);

                    list.Add(new VersionOption
                    {
                        DisplayName = "Apache " + version + " (Lounge x64)",
                        Url = "https://www.apachelounge.com" + relativeUrl,
                        FolderName = "httpd-" + version
                    });
                    if (list.Count >= 3) break;
                }
            }
            return list;
        }

        private List<VersionOption> ScrapeNginx()
        {
            List<VersionOption> list = new List<VersionOption>();
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                string html = wc.DownloadString("https://nginx.org/en/download.html");
                
                MatchCollection matches = Regex.Matches(html, @"href=""(/download/(nginx-(\d+\.\d+\.\d+)\.zip))""");
                HashSet<string> added = new HashSet<string>();
                foreach (Match m in matches)
                {
                    string relativeUrl = m.Groups[1].Value;
                    string zipName = m.Groups[2].Value;
                    string version = m.Groups[3].Value;

                    if (added.Contains(version)) continue;
                    added.Add(version);

                    list.Add(new VersionOption
                    {
                        DisplayName = "Nginx " + version + " (Stable/Legacy)",
                        Url = "https://nginx.org" + relativeUrl,
                        FolderName = "nginx-" + version
                    });
                    if (list.Count >= 4) break;
                }
            }
            return list;
        }

        private List<VersionOption> ScrapeMariaDB()
        {
            List<VersionOption> list = new List<VersionOption>();
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                string html = wc.DownloadString("https://archive.mariadb.org/");
                
                MatchCollection matches = Regex.Matches(html, @"href=""mariadb-(\d+\.\d+\.\d+)/""");
                HashSet<string> added = new HashSet<string>();
                
                List<string> versions = new List<string>();
                foreach (Match m in matches)
                {
                    versions.Add(m.Groups[1].Value);
                }
                
                // Sort by Version descending safely
                versions.Sort((a, b) => {
                    try
                    {
                        Version va = new Version(a);
                        Version vb = new Version(b);
                        return vb.CompareTo(va);
                    }
                    catch { return b.CompareTo(a); }
                });

                foreach (string ver in versions)
                {
                    if (added.Contains(ver)) continue;
                    
                    // We only grab active support versions (11.x or 10.11 LTS or 10.6 LTS) for safety
                    if (ver.StartsWith("11.3") || ver.StartsWith("10.11") || ver.StartsWith("10.6") || ver.StartsWith("11.2"))
                    {
                        added.Add(ver);
                        list.Add(new VersionOption
                        {
                            DisplayName = "MariaDB " + ver + " (Stable x64)",
                            Url = string.Format("https://archive.mariadb.org/mariadb-{0}/winx64-packages/mariadb-{0}-winx64.zip", ver),
                            FolderName = "mariadb-" + ver
                        });
                    }
                    if (list.Count >= 4) break;
                }
            }
            return list;
        }

        private List<VersionOption> ScrapePma()
        {
            List<VersionOption> list = new List<VersionOption>();
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                string html = wc.DownloadString("https://www.phpmyadmin.net/downloads/");
                
                MatchCollection matches = Regex.Matches(html, @"href=""(https://files\.phpmyadmin\.net/phpMyAdmin/(\d+\.\d+\.\d+)/(phpMyAdmin-\2-all-languages\.zip))""");
                HashSet<string> added = new HashSet<string>();
                foreach (Match m in matches)
                {
                    string url = m.Groups[1].Value;
                    string version = m.Groups[2].Value;

                    if (added.Contains(version)) continue;
                    added.Add(version);

                    list.Add(new VersionOption
                    {
                        DisplayName = "phpMyAdmin " + version,
                        Url = url,
                        FolderName = version.StartsWith("4") ? "phpmyadmin-old" : "phpmyadmin"
                    });
                    if (list.Count >= 2) break;
                }
            }
            return list;
        }

        private void UpdateDropdowns(List<VersionOption> phps, List<VersionOption> apaches, List<VersionOption> nginxes, List<VersionOption> marias, List<VersionOption> pmas)
        {
            if (phps.Count > 0) UpdateCombo(rowsList[0].ComboSelector, phps);
            if (apaches.Count > 0) UpdateCombo(rowsList[1].ComboSelector, apaches);
            if (nginxes.Count > 0) UpdateCombo(rowsList[2].ComboSelector, nginxes);
            if (marias.Count > 0) UpdateCombo(rowsList[3].ComboSelector, marias);
            if (pmas.Count > 0) UpdateCombo(rowsList[4].ComboSelector, pmas);
        }

        private void UpdateCombo(ComboBox cb, List<VersionOption> options)
        {
            cb.Items.Clear();
            foreach (var opt in options)
            {
                cb.Items.Add(opt);
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void StartDownloadFlow(DownloadRow row)
        {
            if (webClient != null && webClient.IsBusy)
            {
                MessageBox.Show("Có tiến trình tải khác đang chạy. Vui lòng chờ đợi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            VersionOption selectedOpt = row.ComboSelector.SelectedItem as VersionOption;
            if (selectedOpt == null) return;

            if (!Directory.Exists("downloads")) Directory.CreateDirectory("downloads");
            if (!Directory.Exists("www")) Directory.CreateDirectory("www");

            string zipPath = Path.Combine("downloads", row.ZipFileName);
            string finalTargetFolder = Path.Combine(row.BaseDestFolder, selectedOpt.FolderName);

            // Tự động tìm kiếm xem có tệp ZIP nào liên quan đã được tải sẵn thủ công trong thư mục downloads không
            string foundZip = null;
            try
            {
                string filter = "*.zip";
                if (row.BaseDestFolder.Contains("php")) filter = "*php*.zip";
                else if (row.BaseDestFolder.Contains("mysql")) filter = "*mysql*.zip";
                else if (row.BaseDestFolder.Contains("apache")) filter = "*httpd*.zip";
                else if (row.BaseDestFolder.Contains("nginx")) filter = "*nginx*.zip";
                else if (row.ComponentName.Contains("phpMyAdmin")) filter = "*phpmyadmin*.zip";

                List<string> fileList = new List<string>();
                string[] searchDirs = new string[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RBWStack"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools"), AppDomain.CurrentDomain.BaseDirectory };
                foreach (var dirPath in searchDirs)
                {
                    if (Directory.Exists(dirPath))
                    {
                        fileList.AddRange(Directory.GetFiles(dirPath, filter));
                    }
                }
                string[] files = fileList.ToArray();
                if (files.Length > 0)
                {
                    long maxLen = 0;
                    foreach (var f in files)
                    {
                        long len = new FileInfo(f).Length;
                        if (len > 5 * 1024 * 1024 && len > maxLen) // Phải lớn hơn 5MB
                        {
                            maxLen = len;
                            foundZip = f;
                        }
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(foundZip))
            {
                var dr = MessageBox.Show(
                    "Tìm thấy tệp tin ZIP đã được tải sẵn thủ công trong khay lưu trữ:\r\n" + Path.GetFileName(foundZip) + " (Kích thước: " + (new FileInfo(foundZip).Length / 1024 / 1024) + " MB).\r\n\r\n" +
                    "Bạn có muốn tiến hành giải nén, cấu hình tự động ngay lập tức từ tệp tin này mà không cần tải lại không?",
                    "Phát hiện tệp cài đặt có sẵn",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (dr == DialogResult.Yes)
                {
                    ExtractAndInstall(foundZip, finalTargetFolder, row, selectedOpt.DisplayName);
                    return;
                }
            }

            lblStatusText.Text = "Đang kết nối để tải " + selectedOpt.DisplayName + "...";
            pbDownload.Style = ProgressBarStyle.Marquee; // Set to marquee initially during connect
            pbDownload.Value = 0;

            webClient = new WebClient();
            webClient.Proxy = null; // Disable proxy auto-detection to make download start INSTANTLY!
            webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            webClient.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            if (selectedOpt.Url.Contains("apachelounge.com"))
            {
                webClient.Headers.Add("Referer", "https://www.apachelounge.com/download/");
            }
            else if (selectedOpt.Url.Contains("mysql.com"))
            {
                webClient.Headers.Add("Referer", "https://downloads.mysql.com/archives/community/");
            }

            webClient.DownloadProgressChanged += (s, e) => {
                this.Invoke(new MethodInvoker(() => {
                    double downloadedMB = (double)e.BytesReceived / 1024 / 1024;
                    if (e.TotalBytesToReceive > 0)
                    {
                        pbDownload.Style = ProgressBarStyle.Blocks;
                        pbDownload.Value = e.ProgressPercentage;
                        double totalMB = (double)e.TotalBytesToReceive / 1024 / 1024;
                        lblStatusText.Text = string.Format("Đang tải {0}... {1}% ({2:F2} MB / {3:F2} MB)", 
                            selectedOpt.DisplayName, 
                            e.ProgressPercentage, 
                            downloadedMB, 
                            totalMB);
                    }
                    else
                    {
                        // Handle chunked transfer or unknown total size safely
                        pbDownload.Style = ProgressBarStyle.Marquee;
                        lblStatusText.Text = string.Format("Đang tải {0}... (Đã nhận: {1:F2} MB - Đang tải tiếp...)", 
                            selectedOpt.DisplayName, 
                            downloadedMB);
                    }
                }));
            };

            webClient.DownloadFileCompleted += (s, e) => {
                this.Invoke(new MethodInvoker(() => {
                    if (e.Cancelled)
                    {
                        lblStatusText.Text = "Đã hủy tải xuống.";
                        return;
                    }
                    if (e.Error != null)
                    {
                        lblStatusText.Text = "Lỗi khi tải: " + e.Error.Message;
                        
                        var dr = MessageBox.Show(
                            "Không thể tải tự động tệp cài đặt (Lỗi: " + e.Error.Message + ").\r\n" +
                            "Điều này xảy ra do máy chủ của nhà cung cấp (hoặc Akamai CDN) chặn kết nối tự động từ ứng dụng tại khu vực của bạn.\r\n\r\n" +
                            "Bạn có muốn mở Trình duyệt Web để tải tệp ZIP chính chủ này bằng tay không?\r\n" +
                            "(Sau khi tải xong, bạn chỉ cần copy tệp ZIP vừa tải vào thư mục 'downloads' của ứng dụng và bấm lại nút 'TẢI & CÀI ĐẶT'. Phần mềm sẽ lập tức nhận diện tệp có sẵn, giải nén và cấu hình tự động từ A-Z cho bạn!)",
                            "Không thể tải tự động (403 Forbidden / Akamai Block)",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );
                        
                        if (dr == DialogResult.Yes)
                        {
                            try
                            {
                                Process.Start(selectedOpt.Url);
                            }
                            catch { }
                        }
                        return;
                    }

                    // Thêm kiểm tra kích thước file để chống lỗi tải nhầm HTML hoặc file hỏng
                    try
                    {
                        if (File.Exists(zipPath))
                        {
                            long fileSize = new FileInfo(zipPath).Length;
                            if (fileSize < 100 * 1024) // < 100 KB
                            {
                                string contentSample = "";
                                try { contentSample = File.ReadAllText(zipPath); } catch { }
                                if (contentSample.Contains("<!DOCTYPE html>") || contentSample.Contains("<html") || contentSample.Contains("404 Not Found") || contentSample.Contains("403 Forbidden") || contentSample.Contains("301 Moved") || contentSample.Contains("302 Found"))
                                {
                                    lblStatusText.Text = "Lỗi: File tải về không hợp lệ từ máy chủ!";
                                    MessageBox.Show("Lỗi: Máy chủ trả về lỗi (403/404/Redirect) thay vì file Zip thực tế.\r\nĐiều này xảy ra do máy chủ Apache Lounge đã gỡ bỏ phiên bản cũ này trên trang của họ.\r\n\r\nVui lòng thử chọn phiên bản khác mới hơn được cập nhật ở trên đầu danh sách!", "Lỗi Tải File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    try { File.Delete(zipPath); } catch { }
                                    return;
                                }
                            }
                        }
                    }
                    catch { }

                    if (zipPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        if (row.ZipFileName.Equals("cloudflared.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, row.BaseDestFolder);
                                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                                string destFile = Path.Combine(targetDir, "cloudflared.exe");
                                File.Copy(zipPath, destFile, true);
                                lblStatusText.Text = "Cài đặt Cloudflared thành công!";
                                pbDownload.Style = ProgressBarStyle.Blocks;
                                pbDownload.Value = 100;
                                MessageBox.Show("Cài đặt Cloudflared thành công tại:\r\n" + destFile, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                lblStatusText.Text = "Lỗi cài đặt Cloudflared: " + ex.Message;
                            }
                        }
                        else
                        {
                            InstallVCRedist(zipPath);
                        }
                    }
                    else
                    {
                        ExtractAndInstall(zipPath, finalTargetFolder, row, selectedOpt.DisplayName);
                    }
                }));
            };

            try
            {
                webClient.DownloadFileAsync(new Uri(selectedOpt.Url), zipPath);
            }
            catch (Exception ex)
            {
                lblStatusText.Text = "Không thể bắt đầu tải: " + ex.Message;
                MessageBox.Show("Lỗi khởi chạy tải xuống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InstallVCRedist(string exePath)
        {
            lblStatusText.Text = "Đang khởi chạy trình cài đặt Microsoft Visual C++...";
            pbDownload.Style = ProgressBarStyle.Marquee;

            var bw = new System.ComponentModel.BackgroundWorker();
            bw.DoWork += (s, ev) => {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Path.GetFullPath(exePath);
                    psi.Arguments = "/passive /norestart";
                    psi.UseShellExecute = true;

                    Process proc = Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Lỗi khởi động trình cài đặt: " + ex.Message);
                }
            };

            bw.RunWorkerCompleted += (s, ev) => {
                pbDownload.Style = ProgressBarStyle.Blocks;
                pbDownload.Value = 100;

                try { File.Delete(exePath); } catch { }

                if (ev.Error != null)
                {
                    lblStatusText.Text = "Cài đặt VC++ thất bại: " + ev.Error.Message;
                    MessageBox.Show("Lỗi cài đặt VC++: " + ev.Error.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    lblStatusText.Text = "Đã cài đặt thành công Microsoft Visual C++ Runtime!";
                    MessageBox.Show("Đã cài đặt thành công Microsoft Visual C++ 2015-2022 Runtime!\r\nBây giờ các dịch vụ Apache VS18/VS17 sẽ khởi động hoàn toàn bình thường.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            bw.RunWorkerAsync();
        }

        private void ExtractAndInstall(string zipPath, string finalTargetFolder, DownloadRow row, string displayName)
        {
            lblStatusText.Text = "Đang tự động giải nén " + displayName + "... (Vui lòng không tắt app)";
            pbDownload.Style = ProgressBarStyle.Marquee;

            var bw = new System.ComponentModel.BackgroundWorker();
            bw.DoWork += (s, ev) => {
                // Tắt các tiến trình đang khóa thư mục trước khi ghi đè
                try
                {
                    string targetProc = "";
                    if (row.BaseDestFolder.Contains("php")) targetProc = "php-cgi";
                    else if (row.BaseDestFolder.Contains("apache")) targetProc = "httpd";
                    else if (row.BaseDestFolder.Contains("nginx")) targetProc = "nginx";
                    else if (row.BaseDestFolder.Contains("mysql")) targetProc = "mysqld";

                    if (!string.IsNullOrEmpty(targetProc))
                    {
                        foreach (var p in System.Diagnostics.Process.GetProcessesByName(targetProc))
                        {
                            try { p.Kill(); p.WaitForExit(3000); } catch { }
                        }
                    }
                }
                catch { }

                string tempExtractDir = Path.Combine("downloads", "temp_" + Path.GetFileNameWithoutExtension(row.ZipFileName));
                if (Directory.Exists(tempExtractDir))
                {
                    try { Directory.Delete(tempExtractDir, true); } catch { }
                }
                Directory.CreateDirectory(tempExtractDir);

                ZipFile.ExtractToDirectory(zipPath, tempExtractDir);

                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, finalTargetFolder);
                if (Directory.Exists(targetDir))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            Directory.Delete(targetDir, true);
                            break;
                        }
                        catch
                        {
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                }
                Directory.CreateDirectory(targetDir);

                string[] subDirs = Directory.GetDirectories(tempExtractDir);
                string[] subFiles = Directory.GetFiles(tempExtractDir);

                string sourcePath = tempExtractDir;
                string apache24Dir = Path.Combine(tempExtractDir, "Apache24");
                if (Directory.Exists(apache24Dir))
                {
                    sourcePath = apache24Dir;
                }
                else if (subDirs.Length == 1)
                {
                    sourcePath = subDirs[0];
                }
                else
                {
                    foreach (string d in subDirs)
                    {
                        if (File.Exists(Path.Combine(d, "php.exe")) || 
                            File.Exists(Path.Combine(d, @"bin\httpd.exe")) || 
                            File.Exists(Path.Combine(d, "nginx.exe")) ||
                            File.Exists(Path.Combine(d, @"bin\mysqld.exe")))
                        {
                            sourcePath = d;
                            break;
                        }
                    }
                }

                CopyDirectory(sourcePath, targetDir);

                try { Directory.Delete(tempExtractDir, true); } catch { }
                try { File.Delete(zipPath); } catch { }

                if (row.PostExtractAction != null)
                {
                    row.PostExtractAction(finalTargetFolder);
                }
            };

            bw.RunWorkerCompleted += (s, ev) => {
                pbDownload.Style = ProgressBarStyle.Blocks;
                pbDownload.Value = 100;
                if (ev.Error != null)
                {
                    lblStatusText.Text = "Lỗi khi giải nén: " + ev.Error.Message;
                    MessageBox.Show("Lỗi giải nén: " + ev.Error.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    lblStatusText.Text = "Đã cài đặt thành công " + displayName + "!";
                    MessageBox.Show("Đã tải, giải nén và cấu hình tự động " + displayName + " thành công!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    if (this.Owner is MainForm)
                    {
                        ((MainForm)this.Owner).InvokeUpdatePaths();
                    }
                }
            };

            bw.RunWorkerAsync();
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string folder in Directory.GetDirectories(sourceDir))
            {
                string destFolder = Path.Combine(destinationDir, Path.GetFileName(folder));
                CopyDirectory(folder, destFolder);
            }
        }

        private void ConfigurePHP(string relativePhpDir)
        {
            string phpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePhpDir);
            string phpIniPath = Path.Combine(phpDir, "php.ini");
            string devIni = Path.Combine(phpDir, "php.ini-development");

            string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            string tempPathFwd = tempDir.Replace("\\", "/");

            if (!File.Exists(phpIniPath))
            {
                if (File.Exists(devIni))
                {
                    File.Copy(devIni, phpIniPath, true);
                }
                else
                {
                    string iniContent = "[PHP]\r\nengine = On\r\nshort_open_tag = Off\r\nmax_execution_time = 3600\r\nmax_input_time = 3600\r\nmemory_limit = 1024M\r\nerror_reporting = E_ALL\r\ndisplay_errors = Off\r\npost_max_size = 2048M\r\nupload_max_filesize = 2048M\r\nmax_file_uploads = 200\r\nupload_tmp_dir = \"" + tempPathFwd + "\"\r\nextension_dir = \"ext\"\r\nextension = curl\r\nextension = fileinfo\r\nextension = gd\r\nextension = mbstring\r\nextension = mysqli\r\nextension = openssl\r\nextension = pdo_mysql\r\nextension = ftp\r\nextension = zip\r\nsession.gc_maxlifetime = 2592000\r\ndate.timezone = Asia/Ho_Chi_Minh\r\n";
                    File.WriteAllText(phpIniPath, iniContent);
                    return;
                }
            }

            try
            {
                string content = File.ReadAllText(phpIniPath);

                // Only uncomment extension_dir if commented out
                content = Regex.Replace(content, @";\s*extension_dir\s*=\s*""ext""", "extension_dir = \"ext\"", RegexOptions.IgnoreCase);

                string[] exts = { "curl", "fileinfo", "gd", "mbstring", "mysqli", "openssl", "pdo_mysql", "ftp", "zip" };
                foreach (var ext in exts)
                {
                    string targetExt = ext;
                    if (ext == "gd")
                    {
                        string extDir = Path.Combine(phpDir, "ext");
                        if (Directory.Exists(extDir) && (File.Exists(Path.Combine(extDir, "php_gd2.dll")) || File.Exists(Path.Combine(extDir, "php_gd.dll"))))
                        {
                            targetExt = File.Exists(Path.Combine(extDir, "php_gd2.dll")) ? "gd2" : "gd";
                        }
                        else if (content.Contains("php_gd2.dll") || content.Contains(";extension=php_gd2.dll"))
                        {
                            targetExt = "gd2";
                        }
                    }

                    // Try to match both "extension=ext" and "extension=php_ext.dll"
                    string pattern = @"(?m)^\s*extension\s*=\s*(?:php_)?" + targetExt + @"(?:\.dll)?\s*$";
                    bool alreadyEnabled = Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase);
                    if (!alreadyEnabled)
                    {
                        // Try to uncomment the existing disabled line
                        string uncommentPattern = @"(?m)^;+\s*extension\s*=\s*((?:php_)?" + targetExt + @"(?:\.dll)?)\s*$";
                        string uncommented = Regex.Replace(content, uncommentPattern, "extension = $1", RegexOptions.IgnoreCase);
                        if (uncommented != content)
                        {
                            content = uncommented;
                        }
                        else
                        {
                            // If not found in any form, add once safely
                            if (content.Contains("php_") && content.Contains(".dll"))
                            {
                                content += "\r\nextension = php_" + targetExt + ".dll";
                            }
                            else
                            {
                                content += "\r\nextension = " + targetExt;
                            }
                        }
                    }
                }

                // Fix timezone
                if (Regex.IsMatch(content, @"date\.timezone\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @";?\s*date\.timezone\s*=.*", "date.timezone = Asia/Ho_Chi_Minh");
                }
                else
                {
                    content += "\r\n[Date]\r\ndate.timezone = Asia/Ho_Chi_Minh\r\n";
                }

                // Suppress warnings to prevent Bad header / 500 errors in CGI mode
                if (!Regex.IsMatch(content, @"^\s*display_errors\s*=\s*Off", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    content = Regex.Replace(content, @"(?m)^\s*display_errors\s*=.*$", "display_errors = Off", RegexOptions.IgnoreCase);
                }
                if (!Regex.IsMatch(content, @"^\s*log_errors\s*=\s*On", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    content = Regex.Replace(content, @"(?m)^\s*log_errors\s*=.*$", "log_errors = On", RegexOptions.IgnoreCase);
                }

                // Fix upload limits
                if (Regex.IsMatch(content, @"upload_max_filesize\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*upload_max_filesize\s*=.*$", "upload_max_filesize = 2048M", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nupload_max_filesize = 2048M\r\n";
                }

                if (Regex.IsMatch(content, @"post_max_size\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*post_max_size\s*=.*$", "post_max_size = 2048M", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\npost_max_size = 2048M\r\n";
                }

                if (Regex.IsMatch(content, @"max_file_uploads\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*max_file_uploads\s*=.*$", "max_file_uploads = 200", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_file_uploads = 200\r\n";
                }

                if (Regex.IsMatch(content, @"memory_limit\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*memory_limit\s*=.*$", "memory_limit = 1024M", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmemory_limit = 1024M\r\n";
                }

                if (Regex.IsMatch(content, @"max_execution_time\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*max_execution_time\s*=.*$", "max_execution_time = 0", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_execution_time = 0\r\n";
                }

                if (Regex.IsMatch(content, @"max_input_time\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*max_input_time\s*=.*$", "max_input_time = -1", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_input_time = -1\r\n";
                }

                // Fix upload_tmp_dir & sys_temp_dir → avoid system temp directory dependency
                if (Regex.IsMatch(content, @"upload_tmp_dir\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*upload_tmp_dir\s*=.*$", "upload_tmp_dir = \"" + tempPathFwd + "\"", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nupload_tmp_dir = \"" + tempPathFwd + "\"\r\n";
                }

                if (Regex.IsMatch(content, @"sys_temp_dir\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*sys_temp_dir\s*=.*$", "sys_temp_dir = \"" + tempPathFwd + "\"", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nsys_temp_dir = \"" + tempPathFwd + "\"\r\n";
                }

                // Fix session.save_path → avoid "Permission denied" / empty path errors
                string sessionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "sessions");
                if (!Directory.Exists(sessionDir)) Directory.CreateDirectory(sessionDir);
                string sessionPathFwd = sessionDir.Replace("\\", "/");
                // Replace any existing session.save_path line (commented or not) with the correct one
                if (Regex.IsMatch(content, @"session\.save_path\s*=", RegexOptions.IgnoreCase))
                {
                    bool replaced = false;
                    content = Regex.Replace(content, @"(?m)^;?\s*session\.save_path\s*=.*$", m => {
                        if (!replaced) { replaced = true; return "session.save_path = \"" + sessionPathFwd + "\""; }
                        return ""; // remove duplicates
                    }, RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nsession.save_path = \"" + sessionPathFwd + "\"\r\n";
                }

                if (Regex.IsMatch(content, @"session\.gc_maxlifetime\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*session\.gc_maxlifetime\s*=.*$", "session.gc_maxlifetime = 2592000", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nsession.gc_maxlifetime = 2592000\r\n";
                }

                // Auto-configure ionCube PHP Loader (Hỗ trợ chạy PHP mã hóa bằng ionCube 14/15)
                string phpDirName = Path.GetFileName(phpDir.TrimEnd('\\', '/'));
                var mVer = Regex.Match(phpDirName, @"(\d+\.\d+)");
                if (mVer.Success)
                {
                    string verStr = mVer.Groups[1].Value;
                    string loaderName = "ioncube_loader_win_" + verStr + ".dll";
                    string ioncubeDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "ioncube");
                    string srcLoader = Path.Combine(ioncubeDataDir, loaderName);
                    string extDir = Path.Combine(phpDir, "ext");
                    string dstLoader = Path.Combine(extDir, loaderName);

                    if (!File.Exists(srcLoader) && !File.Exists(dstLoader))
                    {
                        try
                        {
                            if (!Directory.Exists(ioncubeDataDir)) Directory.CreateDirectory(ioncubeDataDir);
                            string rawUrl = "https://raw.githubusercontent.com/rambowoon/RBWStack/main/data/ioncube/" + loaderName;
                            using (var wc = new WebClient())
                            {
                                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                                wc.DownloadFile(rawUrl, srcLoader);
                            }
                        }
                        catch { }
                    }

                    if (File.Exists(srcLoader))
                    {
                        if (!Directory.Exists(extDir)) Directory.CreateDirectory(extDir);
                        if (!File.Exists(dstLoader))
                        {
                            try { File.Copy(srcLoader, dstLoader, true); } catch { }
                        }
                    }

                    if (File.Exists(dstLoader))
                    {
                        if (!Regex.IsMatch(content, @"(?m)^\s*zend_extension\s*=.*" + Regex.Escape(loaderName), RegexOptions.IgnoreCase))
                        {
                            string zendLine = "zend_extension = \"" + loaderName + "\"";
                            if (Regex.IsMatch(content, @"\[PHP\]", RegexOptions.IgnoreCase))
                            {
                                content = Regex.Replace(content, @"(\[PHP\]\r?\n)", "$1" + zendLine + "\r\n", RegexOptions.IgnoreCase);
                            }
                            else
                            {
                                content = zendLine + "\r\n" + content;
                            }
                        }
                    }
                }

                File.WriteAllText(phpIniPath, content);
            }
            catch { }
        }

        public static void ConfigureApache(string relativeApacheDir, string webPort, string phpDir = null)
        {
            MainForm.EnsureSslCertificate();
            string apacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeApacheDir);
            string confPath = Path.Combine(apacheDir, @"conf\httpd.conf");
            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            string pmaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phpmyadmin");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string phpBinRoot = Path.Combine(baseDir, @"bin\php");
            if (string.IsNullOrEmpty(phpDir))
            {
                phpDir = Path.Combine(baseDir, @"bin\php\php-8.2.12");
                if (Directory.Exists(phpBinRoot))
                {
                    string[] phpDirs = Directory.GetDirectories(phpBinRoot, "php*");
                    if (phpDirs.Length > 0)
                    {
                        Array.Sort(phpDirs);
                        phpDir = phpDirs[phpDirs.Length - 1];
                    }
                }
            }

            if (!Directory.Exists(wwwDir)) Directory.CreateDirectory(wwwDir);
            if (!Directory.Exists(pmaDir)) Directory.CreateDirectory(pmaDir);

            // Build custom directory blocks for secondary PHP versions using proxy:fcgi
            System.Text.StringBuilder customDirectories = new System.Text.StringBuilder();
            Dictionary<string, string> sitesConfig = MainForm.LoadSitesConfig();
            string activeDocRoot = MainForm.GetActiveDocumentRoot(wwwDir);
            string rootProj = MainForm.LoadRootProjectConfig();
            string rootPhpVer = "Mặc định (Default)";
            if (!string.IsNullOrEmpty(rootProj))
            {
                string tempPhpVer;
                if (sitesConfig.TryGetValue(rootProj, out tempPhpVer) && tempPhpVer != null)
                {
                    rootPhpVer = tempPhpVer;
                }
            }
            string activePhpVer = Path.GetFileName(phpDir);
            foreach (var kvp in sitesConfig)
            {
                string siteName = kvp.Key;
                string phpVer = kvp.Value;
                int phpPort = phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                    ? 9000
                    : MainForm.GetPhpPortForVersion(phpVer);
                string siteDir = (wwwDir.Replace("\\", "/") + "/" + siteName).Replace("//", "/");

                customDirectories.AppendLine(string.Format("<Directory \"{0}\">", siteDir));
                customDirectories.AppendLine("    <FilesMatch \\.php$>");
                customDirectories.AppendLine(string.Format("        SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", phpPort));
                customDirectories.AppendLine("    </FilesMatch>");
                customDirectories.AppendLine("    AllowOverride All");
                customDirectories.AppendLine("    Require all granted");
                customDirectories.AppendLine("</Directory>");
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("ServerRoot \"" + apacheDir.Replace("\\", "/") + "\"");
            sb.AppendLine("Listen " + webPort);
            sb.AppendLine("Listen 443");
            sb.AppendLine("SSLSessionCache \"shmcb:logs/ssl_scache(512000)\"");
            sb.AppendLine("SetEnv PHPRC \"" + Path.GetFullPath(phpDir).Replace("\\", "/") + "\"");
            sb.AppendLine("LoadModule authn_file_module modules/mod_authn_file.so");
            sb.AppendLine("LoadModule authn_core_module modules/mod_authn_core.so");
            sb.AppendLine("LoadModule authz_host_module modules/mod_authz_host.so");
            sb.AppendLine("LoadModule authz_groupfile_module modules/mod_authz_groupfile.so");
            sb.AppendLine("LoadModule authz_user_module modules/mod_authz_user.so");
            sb.AppendLine("LoadModule authz_core_module modules/mod_authz_core.so");
            sb.AppendLine("LoadModule access_compat_module modules/mod_access_compat.so");
            sb.AppendLine("LoadModule auth_basic_module modules/mod_auth_basic.so");
            sb.AppendLine("LoadModule reqtimeout_module modules/mod_reqtimeout.so");
            sb.AppendLine("LoadModule filter_module modules/mod_filter.so");
            sb.AppendLine("LoadModule mime_module modules/mod_mime.so");
            sb.AppendLine("LoadModule log_config_module modules/mod_log_config.so");
            sb.AppendLine("LoadModule env_module modules/mod_env.so");
            sb.AppendLine("LoadModule headers_module modules/mod_headers.so");
            sb.AppendLine("LoadModule setenvif_module modules/mod_setenvif.so");
            sb.AppendLine("LoadModule version_module modules/mod_version.so");
            sb.AppendLine("LoadModule status_module modules/mod_status.so");
            sb.AppendLine("LoadModule autoindex_module modules/mod_autoindex.so");
            sb.AppendLine("LoadModule dir_module modules/mod_dir.so");
            sb.AppendLine("LoadModule alias_module modules/mod_alias.so");
            sb.AppendLine("LoadModule actions_module modules/mod_actions.so");
            sb.AppendLine("LoadModule rewrite_module modules/mod_rewrite.so");
            sb.AppendLine("LoadModule cgi_module modules/mod_cgi.so");
            sb.AppendLine("LoadModule proxy_module modules/mod_proxy.so");
            sb.AppendLine("LoadModule proxy_fcgi_module modules/mod_proxy_fcgi.so");
            sb.AppendLine("ProxyFCGIBackendType GENERIC");
            sb.AppendLine("LoadModule ssl_module modules/mod_ssl.so");
            sb.AppendLine("LoadModule socache_shmcb_module modules/mod_socache_shmcb.so");
            sb.AppendLine("");
            int rootPhpPortGlob = (rootPhpVer == "Mặc định (Default)" || rootPhpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)) ? 9000 : MainForm.GetPhpPortForVersion(rootPhpVer);
            sb.AppendLine("<FilesMatch \\.php$>");
            sb.AppendLine(string.Format("    SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", rootPhpPortGlob));
            sb.AppendLine("</FilesMatch>");
            sb.AppendLine("");
            sb.AppendLine("ServerAdmin admin@localhost");
            sb.AppendLine("ServerName localhost:" + webPort);
            sb.AppendLine("Timeout 3600");
            sb.AppendLine("ProxyTimeout 3600");
            sb.AppendLine("");
            sb.AppendLine("DocumentRoot \"" + activeDocRoot + "\"");
            sb.AppendLine("<Directory \"" + activeDocRoot + "\">");
            sb.AppendLine("    Options Indexes FollowSymLinks");
            sb.AppendLine("    AllowOverride All");
            sb.AppendLine("    Require all granted");
            sb.AppendLine("</Directory>");
            sb.AppendLine("");
            sb.AppendLine("Alias /phpmyadmin \"" + pmaDir.Replace("\\", "/") + "\"");
            sb.AppendLine("<Directory \"" + pmaDir.Replace("\\", "/") + "\">");
            sb.AppendLine("    Options Indexes FollowSymLinks MultiViews");
            sb.AppendLine("    AllowOverride All");
            sb.AppendLine("    Require all granted");
            sb.AppendLine("</Directory>");
            sb.AppendLine("");
            sb.AppendLine(customDirectories.ToString());
            sb.AppendLine("");
            sb.AppendLine("DirectoryIndex index.php index.html");
            sb.AppendLine("");
            sb.AppendLine("ErrorLog \"logs/error.log\"");
            sb.AppendLine("LogLevel warn");
            sb.AppendLine("");
            sb.AppendLine("<IfModule log_config_module>");
            sb.AppendLine("    LogFormat \"%h %l %u %t \\\"%r\\\" %>s %b\" common");
            sb.AppendLine("    CustomLog \"logs/access.log\" common");
            sb.AppendLine("</IfModule>");
 
            // ── GENERATE DEFAULT LOCALHOST VIRTUAL HOST ──
            sb.AppendLine(string.Format("<VirtualHost *:{0}>", webPort));
            sb.AppendLine("    ServerName localhost");
            sb.AppendLine(string.Format("    DocumentRoot \"{0}\"", activeDocRoot));
            sb.AppendLine(string.Format("    <Directory \"{0}\">", activeDocRoot));
            sb.AppendLine("        Options Indexes FollowSymLinks");
            sb.AppendLine("        AllowOverride All");
            sb.AppendLine("        Require all granted");
            int rootPhpPort = (rootPhpVer == "Mặc định (Default)" || rootPhpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase))
                ? 9000
                : MainForm.GetPhpPortForVersion(rootPhpVer);
            sb.AppendLine("    </Directory>");
            sb.AppendLine("</VirtualHost>");
            sb.AppendLine("");

            // Build Virtual Hosts for active tunnels
            try
            {
                Dictionary<string, string> tunnels = MainForm.LoadTunnelsConfig();
                foreach (var kvp in tunnels)
                {
                    string sitePath = kvp.Key;
                    string[] parts = kvp.Value.Split('|');
                    if (parts.Length >= 3)
                    {
                        string subdomain = parts[0];
                        string protocol = parts[1];
                        string active = parts[2];
                        if (active == "1" && !string.IsNullOrEmpty(subdomain))
                        {
                            string phpVer = "Mặc định (Default)";
                            string tempPhpVer;
                            if (sitesConfig.TryGetValue(sitePath, out tempPhpVer) && tempPhpVer != null)
                            {
                                phpVer = tempPhpVer;
                            }
                            
                            string projectDir = MainForm.GetSitesParentDirectory().StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase)
                                ? Path.Combine(wwwDir, sitePath).Replace("\\", "/")
                                : Path.Combine(MainForm.GetSitesParentDirectory(), sitePath).Replace("\\", "/");
                            
                            sb.AppendLine(string.Format("<VirtualHost *:{0}>", webPort));
                            sb.AppendLine(string.Format("    ServerName {0}.trycloudflare.com", subdomain));
                            sb.AppendLine(string.Format("    DocumentRoot \"{0}\"", projectDir));
                            sb.AppendLine(string.Format("    <Directory \"{0}\">", projectDir));
                            sb.AppendLine("        Options Indexes FollowSymLinks");
                            sb.AppendLine("        AllowOverride All");
                            sb.AppendLine("        Require all granted");
                            
                            int tunnelPhpPort = (phpVer == "Mặc định (Default)" || phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase))
                                ? 9000
                                : MainForm.GetPhpPortForVersion(phpVer);
                            sb.AppendLine("        <FilesMatch \\.php$>");
                            sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", tunnelPhpPort));
                            sb.AppendLine("        </FilesMatch>");
                            sb.AppendLine("    </Directory>");
                            sb.AppendLine("</VirtualHost>");
                            sb.AppendLine("");
                        }
                    }
                }

                // ── GENERATE LOCAL VIRTUAL HOSTS FOR ALL PROJECTS (.local) ──
                try
                {
                    string sitesParent = MainForm.GetSitesParentDirectory();
                    if (Directory.Exists(sitesParent))
                    {
                        string[] subDirs = Directory.GetDirectories(sitesParent);
                        foreach (string dir in subDirs)
                        {
                            string folderName = Path.GetFileName(dir);
                            string projectDir = dir.Replace("\\", "/");

                            string siteKey = "";
                            if (dir.StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase))
                            {
                                siteKey = dir.Substring(wwwDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                            }
                            else
                            {
                                siteKey = folderName;
                            }

                            bool vhostEnabled;
                            string vhostDomain;
                            bool vhostUseSsl;
                            MainForm.GetVHostConfig(siteKey, folderName, out vhostEnabled, out vhostDomain, out vhostUseSsl);

                            if (!vhostEnabled)
                            {
                                MainForm.RemoveHostsEntry(vhostDomain);
                                continue;
                            }

                            // Add hosts entry
                            MainForm.AddHostsEntry(vhostDomain);

                            // Load custom PHP version if any
                            string phpVer = "Mặc định (Default)";
                            string tempPhpVer;
                            if (sitesConfig.TryGetValue(siteKey, out tempPhpVer) && tempPhpVer != null)
                            {
                                phpVer = tempPhpVer;
                            }

                            // 1. HTTP Port 80
                            sb.AppendLine(string.Format("<VirtualHost *:{0}>", webPort));
                            sb.AppendLine(string.Format("    ServerName {0}", vhostDomain));
                            sb.AppendLine(string.Format("    DocumentRoot \"{0}\"", projectDir));
                            sb.AppendLine(string.Format("    <Directory \"{0}\">", projectDir));
                            sb.AppendLine("        Options Indexes FollowSymLinks");
                            sb.AppendLine("        AllowOverride All");
                            sb.AppendLine("        Require all granted");
                            
                            int sitePhpPort = (phpVer == "Mặc định (Default)" || phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase))
                                ? 9000
                                : MainForm.GetPhpPortForVersion(phpVer);
                            sb.AppendLine("        <FilesMatch \\.php$>");
                            sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", sitePhpPort));
                            sb.AppendLine("        </FilesMatch>");
                            sb.AppendLine("    </Directory>");
                            sb.AppendLine("</VirtualHost>");
                            sb.AppendLine("");

                            // 2. HTTPS Port 443 (SSL)
                            if (vhostUseSsl)
                            {
                                MainForm.EnsureSslCertificateForDomain(vhostDomain);
                                string crtFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("ssl/domains/{0}.crt", vhostDomain)).Replace("\\", "/");
                                string keyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("ssl/domains/{0}.key", vhostDomain)).Replace("\\", "/");

                                sb.AppendLine("<VirtualHost *:443>");
                                sb.AppendLine(string.Format("    ServerName {0}", vhostDomain));
                                sb.AppendLine(string.Format("    DocumentRoot \"{0}\"", projectDir));
                                sb.AppendLine("    SSLEngine on");
                                sb.AppendLine(string.Format("    SSLCertificateFile \"{0}\"", crtFile));
                                sb.AppendLine(string.Format("    SSLCertificateKeyFile \"{0}\"", keyFile));
                                sb.AppendLine(string.Format("    <Directory \"{0}\">", projectDir));
                                sb.AppendLine("        Options Indexes FollowSymLinks");
                                sb.AppendLine("        AllowOverride All");
                                sb.AppendLine("        Require all granted");
                                
                                int sslPhpPort = (phpVer == "Mặc định (Default)" || phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase))
                                    ? 9000
                                    : MainForm.GetPhpPortForVersion(phpVer);
                                sb.AppendLine("        <FilesMatch \\.php$>");
                                sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", sslPhpPort));
                                sb.AppendLine("        </FilesMatch>");
                                sb.AppendLine("    </Directory>");
                                sb.AppendLine("</VirtualHost>");
                                sb.AppendLine("");
                            }
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), ex.ToString()); } catch { }
            }

            // Append default SSL VirtualHost
            string sslCrt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ssl\localhost.crt").Replace("\\", "/");
            string sslKey = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ssl\localhost.key").Replace("\\", "/");
            sb.AppendLine("<VirtualHost *:443>");
            sb.AppendLine("    DocumentRoot \"" + activeDocRoot + "\"");
            sb.AppendLine("    ServerName localhost");
            sb.AppendLine("    SSLEngine on");
            sb.AppendLine("    SSLCertificateFile \"" + sslCrt + "\"");
            sb.AppendLine("    SSLCertificateKeyFile \"" + sslKey + "\"");
            sb.AppendLine("    <FilesMatch \\.php$>");
            sb.AppendLine(string.Format("        SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", rootPhpPort));
            sb.AppendLine("    </FilesMatch>");
            sb.AppendLine("</VirtualHost>");
            sb.AppendLine("");

            string rawConf = sb.ToString();

            try
            {
                File.WriteAllText(confPath, rawConf);
                CreateDefaultIndexPage();
            }
            catch { }
        }

        public static void ConfigureNginx(string relativeNginxDir, string webPort, string phpDir = null)
        {
            MainForm.EnsureSslCertificate();
            string nginxDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeNginxDir);
            string confPath = Path.Combine(nginxDir, @"conf\nginx.conf");
            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            string pmaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phpmyadmin");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string phpBinRoot = Path.Combine(baseDir, @"bin\php");
            if (string.IsNullOrEmpty(phpDir))
            {
                phpDir = Path.Combine(baseDir, @"bin\php\php-8.2.12");
                if (Directory.Exists(phpBinRoot))
                {
                    string[] phpDirs = Directory.GetDirectories(phpBinRoot, "php*");
                    if (phpDirs.Length > 0)
                    {
                        Array.Sort(phpDirs);
                        phpDir = phpDirs[phpDirs.Length - 1];
                    }
                }
            }
            string activePhpVer = Path.GetFileName(phpDir);

            if (!Directory.Exists(wwwDir)) Directory.CreateDirectory(wwwDir);
            if (!Directory.Exists(pmaDir)) Directory.CreateDirectory(pmaDir);

            // Build custom location blocks for sites having custom PHP versions
            System.Text.StringBuilder customLocations = new System.Text.StringBuilder();
            Dictionary<string, string> sitesConfig = MainForm.LoadSitesConfig();
            string activeDocRoot = MainForm.GetActiveDocumentRoot(wwwDir);
            string rootProj = MainForm.LoadRootProjectConfig();
            int rootPhpPort = 9000;
            if (!string.IsNullOrEmpty(rootProj))
            {
                string rootPhpVer = "Mặc định (Default)";
                string tempPhpVer;
                if (sitesConfig.TryGetValue(rootProj, out tempPhpVer) && tempPhpVer != null)
                {
                    rootPhpVer = tempPhpVer;
                }
                if (rootPhpVer != "Mặc định (Default)")
                {
                    rootPhpPort = rootPhpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                        ? 9000
                        : MainForm.GetPhpPortForVersion(rootPhpVer);
                }
            }
            foreach (var kvp in sitesConfig)
            {
                string siteName = kvp.Key;
                string phpVer = kvp.Value;
                int phpPort = phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                    ? 9000
                    : MainForm.GetPhpPortForVersion(phpVer);

                customLocations.AppendLine(string.Format(@"
        location ^~ /{0}/ {{
            alias ""{1}/{0}/"";
            index index.php index.html index.htm;
            try_files $uri $uri/ /{0}/index.php?$query_string;
            
            location ~ \.php$ {{
                fastcgi_pass   127.0.0.1:{2};
                fastcgi_index  index.php;
                fastcgi_param  SCRIPT_FILENAME  $request_filename;
                include        fastcgi_params;
            }}
        }}", siteName, wwwDir.Replace("\\", "/"), phpPort));
            }

            // Build server blocks for active tunnels
            System.Text.StringBuilder tunnelServerBlocks = new System.Text.StringBuilder();
            try
            {
                Dictionary<string, string> tunnels = MainForm.LoadTunnelsConfig();
                foreach (var kvp in tunnels)
                {
                    string sitePath = kvp.Key;
                    string[] parts = kvp.Value.Split('|');
                    if (parts.Length >= 3)
                    {
                        string subdomain = parts[0];
                        string protocol = parts[1];
                        string active = parts[2];
                        if (active == "1" && !string.IsNullOrEmpty(subdomain))
                        {
                            string phpVer = "Mặc định (Default)";
                            string tempPhpVer;
                            if (sitesConfig.TryGetValue(sitePath, out tempPhpVer) && tempPhpVer != null)
                            {
                                phpVer = tempPhpVer;
                            }
                            int phpPort = (phpVer == "Mặc định (Default)" || phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase))
                                ? 9000
                                : MainForm.GetPhpPortForVersion(phpVer);

                            string projectDir = MainForm.GetSitesParentDirectory().StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase)
                                ? Path.Combine(wwwDir, sitePath).Replace("\\", "/")
                                : Path.Combine(MainForm.GetSitesParentDirectory(), sitePath).Replace("\\", "/");
                            tunnelServerBlocks.AppendLine(string.Format(@"
    server {{
        listen       {0};
        server_name  {1}.trycloudflare.com;
        root         ""{2}"";
        index        index.php index.html index.htm;
        client_max_body_size 200M;
        
        location / {{
            try_files $uri $uri/ /index.php?$query_string;
        }}
        
        location ~ \.php$ {{
            fastcgi_pass   127.0.0.1:{3};
            fastcgi_index  index.php;
            fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
            include        fastcgi_params;
        }}
    }}", webPort, subdomain, projectDir, phpPort));
                        }
                }
            }

                // ── GENERATE LOCAL SERVER BLOCKS FOR ALL PROJECTS (.local) ──
                try
                {
                    string sitesParent = MainForm.GetSitesParentDirectory();
                    if (Directory.Exists(sitesParent))
                    {
                        string[] subDirs = Directory.GetDirectories(sitesParent);
                        foreach (string dir in subDirs)
                        {
                            string folderName = Path.GetFileName(dir);
                            string projectDir = dir.Replace("\\", "/");

                            string siteKey = "";
                            if (dir.StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase))
                            {
                                siteKey = dir.Substring(wwwDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                            }
                            else
                            {
                                siteKey = folderName;
                            }

                            bool vhostEnabled;
                            string vhostDomain;
                            bool vhostUseSsl;
                            MainForm.GetVHostConfig(siteKey, folderName, out vhostEnabled, out vhostDomain, out vhostUseSsl);

                            if (!vhostEnabled)
                            {
                                MainForm.RemoveHostsEntry(vhostDomain);
                                continue;
                            }

                            // Add hosts entry
                            MainForm.AddHostsEntry(vhostDomain);

                            // Load custom PHP version if any
                            string phpVer = "Mặc định (Default)";
                            string tempPhpVer;
                            if (sitesConfig.TryGetValue(siteKey, out tempPhpVer) && tempPhpVer != null)
                            {
                                phpVer = tempPhpVer;
                            }

                            int phpPort = (phpVer == "Mặc định (Default)" || phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase))
                                ? 9000
                                : MainForm.GetPhpPortForVersion(phpVer);

                            // 1. HTTP Port 80
                            tunnelServerBlocks.AppendLine(string.Format(@"
    server {{
        listen       {0};
        server_name  {1};
        root         ""{2}"";
        index        index.php index.html index.htm;
        client_max_body_size 200M;
        
        location / {{
            try_files $uri $uri/ /index.php?$query_string;
        }}
        
        location ~ \.php$ {{
            fastcgi_pass   127.0.0.1:{3};
            fastcgi_index  index.php;
            fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
            include        fastcgi_params;
        }}
    }}", webPort, vhostDomain, projectDir, phpPort));

                            // 2. HTTPS Port 443 (SSL)
                            if (vhostUseSsl)
                            {
                                MainForm.EnsureSslCertificateForDomain(vhostDomain);
                                string crtFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("ssl/domains/{0}.crt", vhostDomain)).Replace("\\", "/");
                                string keyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("ssl/domains/{0}.key", vhostDomain)).Replace("\\", "/");

                                tunnelServerBlocks.AppendLine(string.Format(@"
    server {{
        listen       443 ssl;
        server_name  {0};
        root         ""{1}"";
        index        index.php index.html index.htm;
        client_max_body_size 200M;
        
        ssl_certificate      ""{2}"";
        ssl_certificate_key  ""{3}"";
        
        location / {{
            try_files $uri $uri/ /index.php?$query_string;
        }}
        
        location ~ \.php$ {{
            fastcgi_pass   127.0.0.1:{4};
            fastcgi_index  index.php;
            fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
            include        fastcgi_params;
        }}
    }}", vhostDomain, projectDir, crtFile, keyFile, phpPort));
                            }
                        }
                    }
                }
                catch { }
            }
            catch { }

            string rawConf = 
@"worker_processes  1;
events {
    worker_connections  1024;
}
http {
    include       mime.types;
    default_type  application/octet-stream;
    sendfile        on;
    keepalive_timeout  3600;
    fastcgi_read_timeout 3600s;
    fastcgi_send_timeout 3600s;
    fastcgi_connect_timeout 3600s;
    fastcgi_buffer_size 128k;
    fastcgi_buffers 4 256k;
    fastcgi_busy_buffers_size 256k;
    fastcgi_temp_file_write_size 256k;
    proxy_read_timeout 3600s;
    proxy_send_timeout 3600s;
    
    server {
        listen       {PORT};
        server_name  localhost;
        root         ""{WWW_DIR}"";
        index        index.php index.html index.htm;
        client_max_body_size 200M;
        
        location / {
            try_files $uri $uri/ /index.php?$query_string;
        }
        
        location /phpmyadmin {
            alias ""{PMA_DIR}/"";
            index index.php index.html index.htm;
            
            location ~ \.php$ {
                fastcgi_pass   127.0.0.1:9000;
                fastcgi_index  index.php;
                fastcgi_param  SCRIPT_FILENAME  $request_filename;
                include        fastcgi_params;
            }
        }
        
{CUSTOM_LOCATIONS}

        location ~ \.php$ {
            fastcgi_pass   127.0.0.1:{ROOT_PHP_PORT};
            fastcgi_index  index.php;
            fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
            include        fastcgi_params;
        }
    }

    server {
        listen       443 ssl;
        server_name  localhost;
        root         ""{WWW_DIR}"";
        index        index.php index.html index.htm;
        client_max_body_size 200M;
        
        ssl_certificate      ""{SSL_CRT}"";
        ssl_certificate_key  ""{SSL_KEY}"";
        
        location / {
            try_files $uri $uri/ /index.php?$query_string;
        }
        
        location /phpmyadmin {
            alias ""{PMA_DIR}/"";
            index index.php index.html index.htm;
            
            location ~ \.php$ {
                fastcgi_pass   127.0.0.1:9000;
                fastcgi_index  index.php;
                fastcgi_param  SCRIPT_FILENAME  $request_filename;
                include        fastcgi_params;
            }
        }
        
{CUSTOM_LOCATIONS}

        location ~ \.php$ {
            fastcgi_pass   127.0.0.1:{ROOT_PHP_PORT};
            fastcgi_index  index.php;
            fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
            include        fastcgi_params;
        }
    }

{TUNNEL_SERVER_BLOCKS}
}
";
            string sslCrt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ssl\localhost.crt").Replace("\\", "/");
            string sslKey = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ssl\localhost.key").Replace("\\", "/");
            rawConf = rawConf.Replace("{WWW_DIR}", activeDocRoot.Replace("\\", "/"));
            rawConf = rawConf.Replace("{PMA_DIR}", pmaDir.Replace("\\", "/"));
            rawConf = rawConf.Replace("{PORT}", webPort);
            rawConf = rawConf.Replace("{SSL_CRT}", sslCrt);
            rawConf = rawConf.Replace("{SSL_KEY}", sslKey);
            rawConf = rawConf.Replace("{CUSTOM_LOCATIONS}", customLocations.ToString());
            rawConf = rawConf.Replace("{TUNNEL_SERVER_BLOCKS}", tunnelServerBlocks.ToString());
            rawConf = rawConf.Replace("{ROOT_PHP_PORT}", rootPhpPort.ToString());

            try
            {
                string confDir = Path.Combine(nginxDir, "conf");
                if (!Directory.Exists(confDir)) Directory.CreateDirectory(confDir);
                File.WriteAllText(confPath, rawConf);
                CreateDefaultIndexPage();
            }
            catch { }
        }

        public static void ConfigureMySQL(string relativeMysqlDir, string mysqlPort)
        {
            string mysqlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeMysqlDir);
            string myIniPath = Path.Combine(mysqlDir, "my.ini");

            string rawIni = 
@"[mysqld]
port = {PORT}
basedir = ""{MYSQL_DIR}""
datadir = ""{MYSQL_DIR}/data""
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci
default-storage-engine = INNODB
max_allowed_packet = 64M
innodb_log_file_size = 50M

[mysql]
default-character-set = utf8mb4

[client]
port = {PORT}
default-character-set = utf8mb4
";
            rawIni = rawIni.Replace("{MYSQL_DIR}", mysqlDir.Replace("\\", "/"));
            rawIni = rawIni.Replace("{PORT}", mysqlPort);

            try
            {
                File.WriteAllText(myIniPath, rawIni);

                string dataDir = Path.Combine(mysqlDir, "data");
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                    
                    string initExe = Path.Combine(mysqlDir, @"bin\mysql_install_db.exe");
                    if (File.Exists(initExe))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = initExe;
                        psi.Arguments = "--datadir=\"" + dataDir + "\"";
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process.Start(psi).WaitForExit(10000);
                    }
                }
            }
            catch { }
        }

        public static void ConfigurePhpMyAdmin(string relativePmaDir, string mysqlPort = "3306")
        {
            string pmaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePmaDir);
            if (!Directory.Exists(pmaDir)) Directory.CreateDirectory(pmaDir);
            string configReal = Path.Combine(pmaDir, "config.inc.php");

            string content = @"<?php
/**
 * phpMyAdmin configuration for RBWStack (local development)
 */

declare(strict_types=1);

/**
 * Blowfish secret - PHẢI đúng 32 bytes
 */
$cfg['blowfish_secret'] = 'RBWStack@LocalDev#2024!SecureK32';

/**
 * Servers configuration
 */
$i = 0;
$i++;

$cfg['Servers'][$i]['auth_type']     = 'config';
$cfg['Servers'][$i]['user']          = 'root';
$cfg['Servers'][$i]['password']      = '';
$cfg['Servers'][$i]['host']          = '127.0.0.1';
$cfg['Servers'][$i]['port']          = '" + mysqlPort + @"';
$cfg['Servers'][$i]['compress']      = false;
$cfg['Servers'][$i]['AllowNoPassword'] = true;
$cfg['Servers'][$i]['AllowRoot']     = true;

/* phpMyAdmin configuration storage */
$cfg['Servers'][$i]['pmadb']               = 'phpmyadmin';
$cfg['Servers'][$i]['bookmarktable']       = 'pma__bookmark';
$cfg['Servers'][$i]['relation']            = 'pma__relation';
$cfg['Servers'][$i]['table_info']          = 'pma__table_info';
$cfg['Servers'][$i]['table_coords']        = 'pma__table_coords';
$cfg['Servers'][$i]['pdf_pages']           = 'pma__pdf_pages';
$cfg['Servers'][$i]['column_info']         = 'pma__column_info';
$cfg['Servers'][$i]['history']             = 'pma__history';
$cfg['Servers'][$i]['table_uiprefs']       = 'pma__table_uiprefs';
$cfg['Servers'][$i]['tracking']            = 'pma__tracking';
$cfg['Servers'][$i]['userconfig']          = 'pma__userconfig';
$cfg['Servers'][$i]['recent']              = 'pma__recent';
$cfg['Servers'][$i]['favorite']            = 'pma__favorite';
$cfg['Servers'][$i]['users']               = 'pma__users';
$cfg['Servers'][$i]['usergroups']          = 'pma__usergroups';
$cfg['Servers'][$i]['navigationhiding']    = 'pma__navigationhiding';
$cfg['Servers'][$i]['savedsearches']       = 'pma__savedsearches';
$cfg['Servers'][$i]['central_columns']     = 'pma__central_columns';
$cfg['Servers'][$i]['designer_settings']   = 'pma__designer_settings';
$cfg['Servers'][$i]['export_templates']    = 'pma__export_templates';

/**
 * Tắt bắt buộc HTTPS
 */
$cfg['ForceSSL'] = false;

/**
 * Session cookie - cho phép HTTP
 */
ini_set('session.cookie_secure', '0');
ini_set('session.cookie_httponly', '1');
ini_set('session.cookie_samesite', 'Lax');
ini_set('session.cookie_lifetime', '2592000'); // Lưu session cookie 30 ngày để tránh mất đăng nhập khi khởi động lại máy/đóng trình duyệt
ini_set('session.gc_maxlifetime', '2592000'); // 30 ngày = khớp với LoginCookieStore/Validity

$cfg['LoginCookieValidity'] = 2592000;
$cfg['LoginCookieStore'] = 2592000;

/**
 * Session save path
 */
$sessionPath = __DIR__ . '/../tmp/sessions';
if (!is_dir($sessionPath)) {
    @mkdir($sessionPath, 0777, true);
}
ini_set('session.save_path', $sessionPath);

/**
 * Upload/Save directories
 */
$cfg['UploadDir'] = '';
$cfg['SaveDir']   = '';

/**
 * Misc
 */
$cfg['CheckConfigurationPermissions'] = false;
$cfg['LoginCookieValidity']           = 2592000;
$cfg['LoginCookieStore']              = 2592000; // Lưu cookie trên trình duyệt không bị xóa khi tắt
$cfg['DefaultLang']                   = 'en';
$cfg['SendErrorReports']              = 'never';
";

            try
            {
                File.WriteAllText(configReal, content);
            }
            catch { }
        }

        public static void CreateDefaultIndexPage()
        {
            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            string indexPath = Path.Combine(wwwDir, "index.php");

            if (!File.Exists(indexPath))
            {
                string html = 
@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>RBW Stack Manager - Welcome</title>
    <style>
        body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #1a1a1e; color: #e0e0e6; margin: 0; padding: 0; display: flex; align-items: center; justify-content: center; height: 100vh; }
        .card { background-color: #25252b; border: 1px solid #33333b; border-radius: 12px; padding: 40px; text-align: center; box-shadow: 0 8px 24px rgba(0,0,0,0.3); max-width: 600px; width: 100%; }
        h1 { color: #5865f2; margin-bottom: 10px; font-size: 28px; }
        p { color: #a0a0ab; font-size: 15px; line-height: 1.6; }
        .details { margin: 25px 0; padding: 15px; background-color: #1e1e24; border-radius: 6px; text-align: left; }
        .details code { color: #2ecc71; font-family: 'Consolas', monospace; font-size: 14px; }
        .btn { display: inline-block; background-color: #2ecc71; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; margin-top: 15px; transition: background 0.2s; }
        .btn:hover { background-color: #27ae60; }
        .links { margin-top: 25px; font-size: 14px; }
        .links a { color: #3498db; text-decoration: none; margin: 0 10px; }
        .links a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class=""card"" style=""margin-top: 40px;"">
        <h1>XIN CHÀO CẬU CHỦ!</h1>
        <p>Hệ thống Web Server RBW Stack rút gọn đã hoạt động hoàn toàn chính xác.</p>
        
        <div class=""details"">
            Thư mục dự án (Web Root):<br/>
            <code>" + wwwDir + @"</code><br/><br/>
            Phiên bản PHP:<br/>
            <code><?php echo PHP_VERSION; ?></code>
        </div>

        <a href=""phpmyadmin/"" class=""btn"">TRUY CẬP PHPMYADMIN</a>

        <div class=""links"">
            <a href=""index.php"">Tải Lại</a> | 
            <a href=""phpinfo.php"" target=""_blank"">Xem PHP Info</a>
        </div>
    </div>
</body>
</html>";
                File.WriteAllText(indexPath, html);
                File.WriteAllText(Path.Combine(wwwDir, "phpinfo.php"), "<?php phpinfo(); ?>");
            }
        }
    }
}
