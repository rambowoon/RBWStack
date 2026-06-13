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

    static class Program
    {
        private static System.Threading.Mutex mutex;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

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

    // Modern Flat Button Custom Control
    public class ModernButton : Button
    {
        private Color _normalColor = Color.FromArgb(255, 255, 255);
        private Color _hoverColor = Color.FromArgb(249, 250, 251);
        private Color _pressedColor = Color.FromArgb(243, 244, 246);
        private Color _borderColor = Color.FromArgb(229, 231, 235);
        private int _cornerRadius = 6;
        private string _iconGlyph = "";
        private bool _isSelected = false;
        private Color _selectedIndicatorColor = Color.FromArgb(16, 185, 129); // Emerald green

        private bool _isHovered = false;
        private bool _isPressed = false;

        public Color NormalColor
        {
            get { return _normalColor; }
            set { _normalColor = value; Invalidate(); }
        }

        public Color HoverColor
        {
            get { return _hoverColor; }
            set { _hoverColor = value; Invalidate(); }
        }

        public Color PressedColor
        {
            get { return _pressedColor; }
            set { _pressedColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        public string IconGlyph
        {
            get { return _iconGlyph; }
            set { _iconGlyph = value; Invalidate(); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; Invalidate(); }
        }

        public Color SelectedIndicatorColor
        {
            get { return _selectedIndicatorColor; }
            set { _selectedIndicatorColor = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.FromArgb(55, 65, 81); // Gray-700
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            Size = new Size(80, 28);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateBackColorFromParent();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            UpdateBackColorFromParent();
        }

        private void UpdateBackColorFromParent()
        {
            if (Parent != null)
            {
                Color parentColor = Parent.BackColor;
                Control p = Parent;
                while (parentColor == Color.Transparent && p.Parent != null)
                {
                    p = p.Parent;
                    parentColor = p.BackColor;
                }
                this.BackColor = parentColor;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            base.OnMouseUp(mevent);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Clear background with resolved parent backcolor
            using (SolidBrush parentBgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(parentBgBrush, ClientRectangle);
            }

            // Determine background color based on hover/press/selected
            Color currentBg = _normalColor;
            bool isHovered = _isHovered;
            bool isPressed = _isPressed;

            if (_isSelected)
            {
                currentBg = Color.FromArgb(243, 244, 246); // Subtle select Gray-100
            }
            else if (isPressed)
            {
                currentBg = _pressedColor;
            }
            else if (isHovered)
            {
                currentBg = _hoverColor;
            }

            if (!Enabled)
            {
                currentBg = Color.FromArgb(249, 250, 251);
            }

            // Fill rounded background
            using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, Width - 1, Height - 1), _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(currentBg))
                {
                    g.FillPath(brush, path);
                }

                // Draw border outline
                if (_borderColor != Color.Transparent && _borderColor.A > 0 && Enabled)
                {
                    using (Pen pen = new Pen(_borderColor, 1.0f))
                    {
                        pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Draw selection indicator stripe on the left edge for sidebar tabs
            if (_isSelected && _cornerRadius == 0)
            {
                using (SolidBrush indicatorBrush = new SolidBrush(_selectedIndicatorColor))
                {
                    g.FillRectangle(indicatorBrush, 0, 0, 4, Height);
                }
            }

            Color textCol = Enabled ? ForeColor : Color.FromArgb(156, 163, 175);

            // Draw icon and text
            if (!string.IsNullOrEmpty(_iconGlyph))
            {
                using (Font iconFont = new Font("Segoe MDL2 Assets", 9.5f))
                {
                    TextFormatFlags iconFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                    Rectangle iconRect = new Rectangle(14, 0, 20, Height);
                    TextRenderer.DrawText(g, _iconGlyph, iconFont, iconRect, textCol, iconFlags);
                }

                TextFormatFlags textFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                Rectangle textRect = new Rectangle(38, 0, Width - 40, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, textCol, textFlags);
            }
            else
            {
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                if (TextAlign == ContentAlignment.MiddleLeft)
                {
                    flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                    Rectangle paddedRect = new Rectangle(Padding.Left > 0 ? Padding.Left : 8, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical);
                    TextRenderer.DrawText(g, Text, Font, paddedRect, textCol, flags);
                }
                else
                {
                    TextRenderer.DrawText(g, Text, Font, ClientRectangle, textCol, flags);
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            if (radius == 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Modern Flat ComboBox with mouse wheel scroll blocked
    public class NoScrollComboBox : ComboBox
    {
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandledMouseEventArgs hme = e as HandledMouseEventArgs;
            if (hme != null) hme.Handled = true;
        }
    }

    // Modern Config Editor
    public class ConfigEditorForm : Form
    {
        private string _filePath;
        private TextBox txtContent;
        private Label lblTitle;
        private ModernButton btnSave;
        private ModernButton btnCancel;
        private Panel pnlHeader;
        private Label btnClose;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public ConfigEditorForm(string title, string filePath)
        {
            _filePath = filePath;
            
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(700, 500);
            this.BackColor = Color.FromArgb(24, 24, 28);

            // Header panel
            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 40;
            pnlHeader.BackColor = Color.FromArgb(32, 32, 38);
            pnlHeader.MouseDown += Header_MouseDown;

            lblTitle = new Label();
            lblTitle.Text = "Config Editor - " + title;
            lblTitle.ForeColor = Color.FromArgb(240, 240, 245);
            lblTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 10);
            pnlHeader.Controls.Add(lblTitle);

            btnClose = new Label();
            btnClose.Text = "X";
            btnClose.ForeColor = Color.FromArgb(150, 150, 160);
            btnClose.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Size = new Size(30, 30);
            btnClose.Location = new Point(665, 8);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 150, 160);
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            Panel pnlEditor = new Panel();
            pnlEditor.Dock = DockStyle.Fill;
            pnlEditor.Padding = new Padding(15, 55, 15, 65);
            this.Controls.Add(pnlEditor);

            txtContent = new TextBox();
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Both;
            txtContent.Dock = DockStyle.Fill;
            txtContent.BackColor = Color.FromArgb(32, 32, 38);
            txtContent.ForeColor = Color.FromArgb(220, 220, 225);
            txtContent.Font = new Font("Consolas", 10f);
            txtContent.BorderStyle = BorderStyle.None;
            txtContent.WordWrap = false;
            pnlEditor.Controls.Add(txtContent);

            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 55;
            pnlBottom.BackColor = Color.FromArgb(24, 24, 28);
            
            btnSave = new ModernButton();
            btnSave.Text = "Lưu (Save)";
            btnSave.NormalColor = Color.FromArgb(46, 204, 113);
            btnSave.HoverColor = Color.FromArgb(39, 174, 96);
            btnSave.PressedColor = Color.FromArgb(30, 132, 73);
            btnSave.Location = new Point(475, 10);
            btnSave.Click += Save_Click;
            pnlBottom.Controls.Add(btnSave);

            btnCancel = new ModernButton();
            btnCancel.Text = "Hủy (Cancel)";
            btnCancel.NormalColor = Color.FromArgb(120, 120, 130);
            btnCancel.HoverColor = Color.FromArgb(100, 100, 110);
            btnCancel.PressedColor = Color.FromArgb(80, 80, 90);
            btnCancel.Location = new Point(585, 10);
            btnCancel.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnCancel);

            this.Controls.Add(pnlBottom);
            pnlBottom.BringToFront();

            LoadConfigContent();
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void LoadConfigContent()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    txtContent.Text = File.ReadAllText(_filePath);
                }
                else
                {
                    txtContent.Text = "; File config không tồn tại ở đường dẫn:\r\n; " + _filePath;
                    btnSave.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(_filePath, txtContent.Text);
                MessageBox.Show("Đã lưu cấu hình thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ghi file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class VersionOption
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string FolderName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class DownloadRow
    {
        public string ComponentName;
        public string BaseDestFolder;
        public string ZipFileName;
        public List<VersionOption> Options;
        public Action<string> PostExtractAction;

        public ComboBox ComboSelector;
        public Label LabelDesc;
        public Panel CardPanel;
    }

    // Modern Download Center with Dynamic Scraping & Offline Fallback
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
            btnClose.Font = new Font("Segoe UI Semibold", 20f);
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
                lblDesc.Font = new Font("Segoe UI Italic", 8.25f);
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
            lblStatusText.Font = new Font("Segoe UI Semibold", 9.25f);
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
                
                MatchCollection matches = Regex.Matches(html, @"href=""(php-(\d+\.\d+\.\d+)-Win32-(?:vs\d+|vc\d+)-x64\.zip)""");
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
                for (int i = 0; i < Math.Min(temp.Count, 6); i++)
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

                string[] files = Directory.GetFiles("downloads", filter);
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
                    content = Regex.Replace(content, @"(?m)^;?\s*max_execution_time\s*=.*$", "max_execution_time = 3600", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_execution_time = 3600\r\n";
                }

                if (Regex.IsMatch(content, @"max_input_time\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*max_input_time\s*=.*$", "max_input_time = 3600", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_input_time = 3600\r\n";
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
            sb.AppendLine("ScriptAlias /php/ \"" + Path.GetFullPath(phpDir).Replace("\\", "/") + "/\"");
            sb.AppendLine("Action application/x-httpd-php \"/php/php-cgi.exe\"");
            sb.AppendLine("AddHandler application/x-httpd-php .php");
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
            if (rootPhpVer != "Mặc định (Default)")
            {
                int phpPort = rootPhpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                    ? 9000
                    : MainForm.GetPhpPortForVersion(rootPhpVer);
                sb.AppendLine("        <FilesMatch \\.php$>");
                sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", phpPort));
                sb.AppendLine("        </FilesMatch>");
            }
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
                            
                            if (phpVer != "Mặc định (Default)")
                            {
                                int phpPort = phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                                    ? 9000
                                    : MainForm.GetPhpPortForVersion(phpVer);
                                sb.AppendLine("        <FilesMatch \\.php$>");
                                sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", phpPort));
                                sb.AppendLine("        </FilesMatch>");
                            }
                            
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
                            
                            if (phpVer != "Mặc định (Default)")
                            {
                                int phpPort = phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                                    ? 9000
                                    : MainForm.GetPhpPortForVersion(phpVer);
                                sb.AppendLine("        <FilesMatch \\.php$>");
                                sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", phpPort));
                                sb.AppendLine("        </FilesMatch>");
                            }
                            
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
                                
                                if (phpVer != "Mặc định (Default)")
                                {
                                    int phpPort = phpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                                        ? 9000
                                        : MainForm.GetPhpPortForVersion(phpVer);
                                    sb.AppendLine("        <FilesMatch \\.php$>");
                                    sb.AppendLine(string.Format("            SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", phpPort));
                                    sb.AppendLine("        </FilesMatch>");
                                }
                                
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
            if (rootPhpVer != "Mặc định (Default)")
            {
                int phpPort = rootPhpVer.Equals(activePhpVer, StringComparison.OrdinalIgnoreCase)
                    ? 9000
                    : MainForm.GetPhpPortForVersion(rootPhpVer);
                sb.AppendLine("    <FilesMatch \\.php$>");
                sb.AppendLine(string.Format("        SetHandler \"proxy:fcgi://127.0.0.1:{0}//./\"", phpPort));
                sb.AppendLine("    </FilesMatch>");
            }
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

$cfg['Servers'][$i]['auth_type']     = 'cookie';
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
ini_set('session.gc_maxlifetime', '2592000'); // 30 ngày = khớp với LoginCookieStore/Validity

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

    // Main Control Dashboard
    public class MainForm : Form
    {
        public static MainForm Instance;
        private Color colorBg = Color.FromArgb(255, 255, 255); // Crisp white
        private Color colorSidebarBg = Color.FromArgb(249, 250, 251); // Soft light grey/zinc-50
        private Color colorCard = Color.FromArgb(255, 255, 255); // White cards
        private Color colorText = Color.FromArgb(17, 24, 39); // Gray-900 (almost black)
        private Color colorTextDim = Color.FromArgb(107, 114, 128); // Gray-500 (dim text)
        private Color colorGreen = Color.FromArgb(16, 185, 129); // Emerald-500 (emerald active green)
        private Color colorRed = Color.FromArgb(239, 68, 68); // Red-500
        private Color colorAccent = Color.FromArgb(243, 244, 246); // Gray-100 (selected tab)
        private Color colorBorder = Color.FromArgb(170, 175, 185); // Gray-400 (clearly visible slate border)

        private Process procWebServer = null;
        private Process procMySQL = null;
        private Process procPHP = null;
        private List<Process> procPHPList = new List<Process>();
        private Dictionary<string, Process> activeTunnels = new Dictionary<string, Process>();

        private string pathApacheExe = @"bin\apache\bin\httpd.exe";
        private string pathApacheConf = @"bin\apache\conf\httpd.conf";
        private string pathNginxExe = @"bin\nginx\nginx.exe";
        private string pathNginxConf = @"bin\nginx\conf\nginx.conf";
        private string pathMySqlExe = @"bin\mysql\bin\mysqld.exe";
        private string pathMySqlConf = @"bin\mysql\my.ini";
        public string pathPhpExe = @"bin\php\php.exe";
        private string pathPhpCgiExe = @"bin\php\php-cgi.exe";
        private string pathPhpConf = @"bin\php\php.ini";

        private string selectedWebServerType = "Apache";
        private bool isInitializing = true;

        private Label lblHeaderTitle;
        private Label lblSyncStatus;
        private Panel pnlHeader;
        private Label btnHeaderClose;
        private Label btnHeaderMin;
        private System.Windows.Forms.Timer tmrStatus;

        // --- Laravel Herd Style Controls ---
        private Panel pnlSidebar;
        private Panel pnlCardsContainer;
        private string activeFilterComponentName = "ALL";
        private bool isSyncCompleted = false;
        private bool isRealExit = false; // true khi thoát thực sự từ tray menu
        private ModernButton btnTabDashboard; // Services
        private ModernButton btnTabDownload;  // Herd Pro (Software Store)
        private ModernButton btnTabSettings;  // General
        private ModernButton btnTabAbout;     // About
        private ModernButton btnTabSites;
        private ModernButton btnTabPHP;
        private ModernButton btnTabNode;
        private ModernButton btnTabExpose;
        private ModernButton btnTabShortcuts;
        private ModernButton btnTabMail;
        private ModernButton btnTabDumps;

        private ModernButton btnTabPhpEngine;
        private ModernButton btnTabApache;
        private ModernButton btnTabNginx;
        private ModernButton btnTabDatabase;
        private ModernButton btnTabPhpMyAdmin;
        private ModernButton btnTabVcRuntime;

        private Panel pnlTabDashboard;
        private Panel pnlTabDownload;
        private Panel pnlTabSettings;
        private Panel pnlTabAbout;
        private Panel pnlTabPlaceholder;
        private Panel pnlTabSites;

        private Label lblPlaceholderTitle;
        private Label lblPlaceholderDesc;

        private Label lblWebStatusText;
        private Label lblMySqlStatusText;
        private Label lblPhpStatusText;
        private ModernButton btnPhpMyAdmin;

        // --- Dynamic 3-Column details properties ---
        private string selectedService = "MySQL";
        private Label lblDetailTitle;
        private TextBox txtEnvVars;
        private TextBox txtLogSnippets;
        private Panel pnlDetails;
        private Panel pnlRowWeb;
        private Panel pnlRowMySql;
        private Panel pnlRowPhp;

        // --- Download Center Fields inside MainForm ---
        private List<DownloadRow> rowsList = new List<DownloadRow>();
        private Panel pnlProgress;
        private ProgressBar pbDownload;
        private Label lblStatusText;
        private WebClient webClient = null;

        // Old components kept for compatibility
        private Panel cardWeb;
        private Label lblWebStatusDot;
        private Label lblWebTitle;
        private ComboBox cbWebVersions;
        private Label lblWebPort;
        private TextBox txtWebPort;
        private TextBox txtVHostSuffix;
        private ModernButton btnWebStart;
        private ModernButton btnWebStop;
        private ModernButton btnWebConfig;
        private ModernButton btnWebChangePort;

        private Panel cardMySql;
        private Label lblMySqlStatusDot;
        private Label lblMySqlTitle;
        private ComboBox cbMySqlVersions;
        private Label lblMySqlPort;
        private TextBox txtMySqlPort;
        private ModernButton btnMySqlStart;
        private ModernButton btnMySqlStop;
        private ModernButton btnMySqlConfig;
        private ModernButton btnMySqlChangePort;

        private Panel cardPhp;
        private Label lblPhpStatusDot;
        private Label lblPhpTitle;
        private ComboBox cbPhpVersions;
        private Label lblPhpPort;
        private TextBox txtPhpPort;
        private ModernButton btnPhpStart;
        private ModernButton btnPhpStop;
        private ModernButton btnPhpConfig;
        private ModernButton btnPhpChangePort;

        private Label lblWebRoot;
        private ModernButton btnOpenWebRoot;

        private ModernButton btnStartAll;
        private ModernButton btnStopAll;
        private ModernButton btnDownloadCenter;
        private ModernButton btnSettings;

        private Panel pnlSettingsOverlay;
        private TextBox txtSetApacheExe, txtSetApacheConf, txtSetNginxExe, txtSetNginxConf;
        private TextBox txtSetMySqlExe, txtSetMySqlConf, txtSetPhpExe, txtSetPhpConf, txtSetPhpCgiExe;
        private ComboBox cbWebServerType;
        private ModernButton btnSaveSettings;

        // Custom Tray Icon and Portability Settings
        private NotifyIcon trayIcon;
        private TrayPopupForm _trayPopup;
        private CheckBox chkAutoStart;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkAutoOptimizePhpIni;
        private CheckBox chkAdminVHostMode;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        private uint restoreMessage = 0;

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        public MainForm()
        {
            Instance = this;
            var forceHandle = this.Handle;
            restoreMessage = RegisterWindowMessage("RBWSTACK_RESTORE_INSTANCE");

            // Save MainWindowHandle to registry for single instance restoration
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLower().TrimEnd('\\', '/');
                string cleanPath = Regex.Replace(baseDir, @"[^a-zA-Z0-9]", "_");
                if (cleanPath.Length > 200) cleanPath = cleanPath.Substring(cleanPath.Length - 200);
                string regPath = @"SOFTWARE\RBWStack\" + cleanPath;

                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regPath))
                {
                    if (key != null)
                    {
                        key.SetValue("MainWindowHandle", this.Handle.ToInt64().ToString());
                    }
                }
            }
            catch { }

            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(960, 600); // Expanded size for 3-column Herd layout
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorBg;

            // Set tray/app icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            this.FormClosing += (s, e) => {
                if (!isRealExit)
                {
                    // Nút X → ẩn xuống tray thay vì thoát
                    e.Cancel = true;
                    this.Hide();
                    this.ShowInTaskbar = false;
                    if (trayIcon != null) trayIcon.Visible = true;
                    trayIcon.ShowBalloonTip(2000, "RBW Stack", "Ứng dụng vẫn đang chạy trong khay hệ thống.", ToolTipIcon.Info);
                    return;
                }
                StopAllTunnels();
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
            };

            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 45;
            pnlHeader.BackColor = colorSidebarBg; // cohesively matches left sidebar
            pnlHeader.MouseDown += Header_MouseDown;
            pnlHeader.Paint += (s, e) => {
                using (Pen pen = new Pen(colorBorder, 1f))
                {
                    // Draw bottom divider line
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                    // Draw outer border lines
                    e.Graphics.DrawLine(pen, 0, 0, pnlHeader.Width, 0); // Top
                    e.Graphics.DrawLine(pen, 0, 0, 0, pnlHeader.Height); // Left
                    e.Graphics.DrawLine(pen, pnlHeader.Width - 1, 0, pnlHeader.Width - 1, pnlHeader.Height); // Right
                }
            };

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "⚡  RBW STACK";
            lblHeaderTitle.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Regular);
            lblHeaderTitle.ForeColor = colorText;
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Location = new Point(15, 12);
            pnlHeader.Controls.Add(lblHeaderTitle);

            lblSyncStatus = new Label();
            lblSyncStatus.Text = "🔄  ĐANG ĐỒNG BỘ PHIÊN BẢN...";
            lblSyncStatus.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            lblSyncStatus.ForeColor = Color.FromArgb(59, 130, 246); // Modern Blue-500
            lblSyncStatus.AutoSize = true;
            lblSyncStatus.Location = new Point(140, 14); // Positioned next to Title
            lblSyncStatus.Visible = true;
            pnlHeader.Controls.Add(lblSyncStatus);

            btnHeaderClose = new Label();
            btnHeaderClose.Text = "\uE8BB"; // Close icon
            btnHeaderClose.Font = new Font("Segoe MDL2 Assets", 9.5f);
            btnHeaderClose.ForeColor = colorTextDim;
            btnHeaderClose.Cursor = Cursors.Hand;
            btnHeaderClose.Size = new Size(45, 43);
            btnHeaderClose.Location = new Point(914, 1); // Positioned for 960 width
            btnHeaderClose.TextAlign = ContentAlignment.MiddleCenter;
            btnHeaderClose.Click += (s, e) => {
                // Thu nhỏ xuống tray, không thoát app
                this.Hide();
                this.ShowInTaskbar = false;
                if (trayIcon != null) trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "RBW Stack", "Ứng dụng vẫn đang chạy trong khay hệ thống. Nhấn đúp để mở lại.", ToolTipIcon.Info);
            };
            btnHeaderClose.MouseEnter += (s, e) => { btnHeaderClose.ForeColor = Color.White; btnHeaderClose.BackColor = Color.FromArgb(239, 68, 68); };
            btnHeaderClose.MouseLeave += (s, e) => { btnHeaderClose.ForeColor = colorTextDim; btnHeaderClose.BackColor = Color.Transparent; };
            pnlHeader.Controls.Add(btnHeaderClose);

            btnHeaderMin = new Label();
            btnHeaderMin.Text = "\uE921"; // Minimize icon
            btnHeaderMin.Font = new Font("Segoe MDL2 Assets", 9.5f);
            btnHeaderMin.ForeColor = colorTextDim;
            btnHeaderMin.Cursor = Cursors.Hand;
            btnHeaderMin.Size = new Size(46, 43);
            btnHeaderMin.Location = new Point(868, 1); // Positioned for 960 width
            btnHeaderMin.TextAlign = ContentAlignment.MiddleCenter;
            btnHeaderMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnHeaderMin.MouseEnter += (s, e) => { btnHeaderMin.ForeColor = colorText; btnHeaderMin.BackColor = Color.FromArgb(229, 231, 235); };
            btnHeaderMin.MouseLeave += (s, e) => { btnHeaderMin.ForeColor = colorTextDim; btnHeaderMin.BackColor = Color.Transparent; };
            pnlHeader.Controls.Add(btnHeaderMin);

            this.Controls.Add(pnlHeader);

            InitializeHerdLayout();

            tmrStatus = new System.Windows.Forms.Timer();
            tmrStatus.Interval = 1000;
            tmrStatus.Tick += TmrStatus_Tick;
            tmrStatus.Start();

            isInitializing = true;
            AutoDetectBinaries();
            LoadActiveVersionsFromRegistry();
            ReloadAllVersionDropdowns();
            isInitializing = false;

            UpdatePortsDisplay();

            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            if (!Directory.Exists(wwwDir)) Directory.CreateDirectory(wwwDir);

            InitializeTrayIcon();

            string[] args = Environment.GetCommandLineArgs();
            bool isStartup = false;
            foreach (string arg in args)
            {
                if (arg.Equals("--startup", StringComparison.OrdinalIgnoreCase))
                {
                    isStartup = true;
                    break;
                }
            }

            if (isStartup)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }

            this.Load += (s, e) => {
                this.BeginInvoke(new MethodInvoker(delegate { 
                    if (isStartup)
                    {
                        this.Hide(); 
                    }
                    StartAll_Click(null, null);

                    System.Threading.Tasks.Task.Run(() => {
                        try
                        {
                            System.Threading.Thread.Sleep(3000);
                            var release = CheckForUpdatesCached("rambowoon/RBWStack", false);
                            if (release != null)
                            {
                                if (!release.TagName.Equals("v2.1.1", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.BeginInvoke((MethodInvoker)delegate {
                                        ShowUpdatePrompt(release);
                                    });
                                }
                            }
                        }
                        catch { }
                    });
                }));
            };
        }

        public void InvokeUpdatePaths()
        {
            isInitializing = true;
            AutoDetectBinaries();
            ReloadAllVersionDropdowns();
            isInitializing = false;
            UpdatePortsDisplay();
            RenderDownloadCards();
        }

        private void InitializeHerdLayout()
        {
            // 1. Sidebar bên trái (width 200)
            pnlSidebar = new Panel();
            pnlSidebar.Location = new Point(0, 45);
            pnlSidebar.Size = new Size(200, 555);
            pnlSidebar.BackColor = colorSidebarBg;
            pnlSidebar.Paint += (s, e) => {
                using (Pen pen = new Pen(colorBorder, 1f))
                {
                    // Right border divider
                    e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
                    // Left outer border line
                    e.Graphics.DrawLine(pen, 0, 0, 0, pnlSidebar.Height);
                    // Bottom outer border line
                    e.Graphics.DrawLine(pen, 0, pnlSidebar.Height - 1, pnlSidebar.Width, pnlSidebar.Height - 1);
                }
            };
            this.Controls.Add(pnlSidebar);

            // Large title "Settings" at top of sidebar
            Label lblSidebarTitle = new Label();
            lblSidebarTitle.Text = "Settings";
            lblSidebarTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblSidebarTitle.ForeColor = colorText;
            lblSidebarTitle.Location = new Point(20, 15);
            lblSidebarTitle.Size = new Size(160, 35);
            pnlSidebar.Controls.Add(lblSidebarTitle);

            // Creating the 12 active tabs with uniform 38px spacing and premium layout
            btnTabSettings = new ModernButton();
            btnTabSettings.Text = "General";
            btnTabSettings.IconGlyph = "\uE713"; // Settings Gear
            btnTabSettings.Size = new Size(198, 34);
            btnTabSettings.Location = new Point(1, 60);
            btnTabSettings.NormalColor = Color.Transparent;
            btnTabSettings.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabSettings.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabSettings.BorderColor = Color.Transparent;
            btnTabSettings.CornerRadius = 0;
            btnTabSettings.Click += (s, e) => SwitchToTab("settings");
            pnlSidebar.Controls.Add(btnTabSettings);

            btnTabSites = new ModernButton();
            btnTabSites.Text = "Sites";
            btnTabSites.IconGlyph = "\uE7F4"; // Home/Workspace screen
            btnTabSites.Size = new Size(198, 34);
            btnTabSites.Location = new Point(1, 98);
            btnTabSites.NormalColor = Color.Transparent;
            btnTabSites.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabSites.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabSites.BorderColor = Color.Transparent;
            btnTabSites.CornerRadius = 0;
            btnTabSites.Click += (s, e) => SwitchToTab("sites");
            pnlSidebar.Controls.Add(btnTabSites);

            btnTabPHP = new ModernButton(); // Dummy initialization for compiler/runtime compatibility
            
            btnTabDashboard = new ModernButton();
            btnTabDashboard.Text = "Services";
            btnTabDashboard.IconGlyph = "\uEC27"; // Services stack
            btnTabDashboard.Size = new Size(198, 34);
            btnTabDashboard.Location = new Point(1, 136);
            btnTabDashboard.NormalColor = Color.Transparent;
            btnTabDashboard.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabDashboard.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabDashboard.BorderColor = Color.Transparent;
            btnTabDashboard.CornerRadius = 0;
            btnTabDashboard.Click += (s, e) => SwitchToTab("dashboard");
            pnlSidebar.Controls.Add(btnTabDashboard);

            // Core Exposed Download Services
            btnTabMail = new ModernButton();
            btnTabMail.Text = "Mail Sandbox";
            btnTabMail.IconGlyph = "\uE715"; // Mail envelope icon
            btnTabMail.Size = new Size(198, 34);
            btnTabMail.Location = new Point(1, 174);
            btnTabMail.NormalColor = Color.Transparent;
            btnTabMail.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabMail.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabMail.BorderColor = Color.Transparent;
            btnTabMail.CornerRadius = 0;
            btnTabMail.Click += (s, e) => SwitchToTab("mail");
            pnlSidebar.Controls.Add(btnTabMail);

            btnTabPhpEngine = new ModernButton();
            btnTabPhpEngine.Text = "PHP Engine";
            btnTabPhpEngine.IconGlyph = "\uE943"; // Code block icon
            btnTabPhpEngine.Size = new Size(198, 34);
            btnTabPhpEngine.Location = new Point(1, 212);
            btnTabPhpEngine.NormalColor = Color.Transparent;
            btnTabPhpEngine.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabPhpEngine.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabPhpEngine.BorderColor = Color.Transparent;
            btnTabPhpEngine.CornerRadius = 0;
            btnTabPhpEngine.Click += (s, e) => FilterDownloadCards("PHP Engine (x64 Thread Safe)");
            pnlSidebar.Controls.Add(btnTabPhpEngine);

            btnTabNode = new ModernButton();
            btnTabNode.Text = "Node.js Engine";
            btnTabNode.IconGlyph = "\uE9E9"; // Connection/ethernet
            btnTabNode.Size = new Size(198, 34);
            btnTabNode.Location = new Point(1, 250);
            btnTabNode.NormalColor = Color.Transparent;
            btnTabNode.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabNode.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabNode.BorderColor = Color.Transparent;
            btnTabNode.CornerRadius = 0;
            btnTabNode.Click += (s, e) => FilterDownloadCards("Node.js Engine");
            pnlSidebar.Controls.Add(btnTabNode);

            btnTabApache = new ModernButton();
            btnTabApache.Text = "Apache Server";
            btnTabApache.IconGlyph = "\uE774"; // Globe
            btnTabApache.Size = new Size(198, 34);
            btnTabApache.Location = new Point(1, 288);
            btnTabApache.NormalColor = Color.Transparent;
            btnTabApache.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabApache.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabApache.BorderColor = Color.Transparent;
            btnTabApache.CornerRadius = 0;
            btnTabApache.Click += (s, e) => FilterDownloadCards("Apache Web Server (httpd)");
            pnlSidebar.Controls.Add(btnTabApache);

            btnTabNginx = new ModernButton();
            btnTabNginx.Text = "Nginx Server";
            btnTabNginx.IconGlyph = "\uE700"; // Globe outline
            btnTabNginx.Size = new Size(198, 34);
            btnTabNginx.Location = new Point(1, 326);
            btnTabNginx.NormalColor = Color.Transparent;
            btnTabNginx.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabNginx.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabNginx.BorderColor = Color.Transparent;
            btnTabNginx.CornerRadius = 0;
            btnTabNginx.Click += (s, e) => FilterDownloadCards("Nginx Web Server");
            pnlSidebar.Controls.Add(btnTabNginx);

            btnTabDatabase = new ModernButton();
            btnTabDatabase.Text = "Cơ sở dữ liệu";
            btnTabDatabase.IconGlyph = "\uEC27"; // Database cylinder
            btnTabDatabase.Size = new Size(198, 34);
            btnTabDatabase.Location = new Point(1, 364);
            btnTabDatabase.NormalColor = Color.Transparent;
            btnTabDatabase.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabDatabase.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabDatabase.BorderColor = Color.Transparent;
            btnTabDatabase.CornerRadius = 0;
            btnTabDatabase.Click += (s, e) => FilterDownloadCards("Cơ sở dữ liệu (MySQL / MariaDB)");
            pnlSidebar.Controls.Add(btnTabDatabase);

            btnTabPhpMyAdmin = new ModernButton();
            btnTabPhpMyAdmin.Text = "phpMyAdmin";
            btnTabPhpMyAdmin.IconGlyph = "\uE12B"; // Database/Globe table
            btnTabPhpMyAdmin.Size = new Size(198, 34);
            btnTabPhpMyAdmin.Location = new Point(1, 402);
            btnTabPhpMyAdmin.NormalColor = Color.Transparent;
            btnTabPhpMyAdmin.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabPhpMyAdmin.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabPhpMyAdmin.BorderColor = Color.Transparent;
            btnTabPhpMyAdmin.CornerRadius = 0;
            btnTabPhpMyAdmin.Click += (s, e) => FilterDownloadCards("phpMyAdmin Web Database Client");
            pnlSidebar.Controls.Add(btnTabPhpMyAdmin);

            btnTabVcRuntime = new ModernButton();
            btnTabVcRuntime.Text = "VC++ Runtime";
            btnTabVcRuntime.IconGlyph = "\uE8B7"; // Toolbox/wrench
            btnTabVcRuntime.Size = new Size(198, 34);
            btnTabVcRuntime.Location = new Point(1, 440);
            btnTabVcRuntime.NormalColor = Color.Transparent;
            btnTabVcRuntime.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabVcRuntime.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabVcRuntime.BorderColor = Color.Transparent;
            btnTabVcRuntime.CornerRadius = 0;
            btnTabVcRuntime.Click += (s, e) => FilterDownloadCards("Microsoft Visual C++ Runtime (Thư viện nền bắt buộc)");
            pnlSidebar.Controls.Add(btnTabVcRuntime);

            btnTabDownload = new ModernButton();
            btnTabDownload.Text = "RBW Pro";
            btnTabDownload.IconGlyph = "\uE734"; // Star icon for Pro
            btnTabDownload.Size = new Size(198, 34);
            btnTabDownload.Location = new Point(1, 478);
            btnTabDownload.NormalColor = Color.Transparent;
            btnTabDownload.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabDownload.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabDownload.BorderColor = Color.Transparent;
            btnTabDownload.CornerRadius = 0;
            btnTabDownload.Click += (s, e) => SwitchToTab("download");
            pnlSidebar.Controls.Add(btnTabDownload);

            btnTabAbout = new ModernButton();
            btnTabAbout.Text = "About";
            btnTabAbout.IconGlyph = "\uE946"; // Info Circle
            btnTabAbout.Size = new Size(198, 34);
            btnTabAbout.Location = new Point(1, 516);
            btnTabAbout.NormalColor = Color.Transparent;
            btnTabAbout.HoverColor = Color.FromArgb(243, 244, 246);
            btnTabAbout.PressedColor = Color.FromArgb(229, 231, 235);
            btnTabAbout.BorderColor = Color.Transparent;
            btnTabAbout.CornerRadius = 0;
            btnTabAbout.Click += (s, e) => SwitchToTab("about");
            pnlSidebar.Controls.Add(btnTabAbout);

            // Dummy initializations to prevent null reference compiler errors elsewhere
            btnTabExpose = new ModernButton();
            btnTabShortcuts = new ModernButton();
            btnTabDumps = new ModernButton();

            // 2. Tab Panels bên phải (width 760)
            int tabW = 760;
            int tabH = 555;
            Point tabLoc = new Point(200, 45);

            // TAB 1: DASHBOARD
            pnlTabDashboard = new Panel();
            pnlTabDashboard.Size = new Size(tabW, tabH);
            pnlTabDashboard.Location = tabLoc;
            pnlTabDashboard.BackColor = colorBg;
            pnlTabDashboard.Padding = new Padding(0, 0, 1, 1);
            pnlTabDashboard.Paint += DrawTabPanelBorder;
            this.Controls.Add(pnlTabDashboard);

            // SITES PANEL
            pnlTabSites = new Panel();
            pnlTabSites.Size = new Size(tabW, tabH);
            pnlTabSites.Location = tabLoc;
            pnlTabSites.BackColor = colorBg;
            pnlTabSites.Padding = new Padding(0, 0, 1, 1);
            pnlTabSites.Paint += DrawTabPanelBorder;
            pnlTabSites.Visible = false;
            this.Controls.Add(pnlTabSites);

            // Column 2: Services List Panel (docked to fill 760 width)
            Panel pnlServicesList = new Panel();
            pnlServicesList.Size = new Size(760, tabH);
            pnlServicesList.Dock = DockStyle.Fill;
            pnlServicesList.BackColor = Color.White;
            pnlTabDashboard.Controls.Add(pnlServicesList);

            Label lblServicesHeader = new Label();
            lblServicesHeader.Text = "Services";
            lblServicesHeader.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblServicesHeader.ForeColor = colorText;
            lblServicesHeader.Location = new Point(20, 20);
            lblServicesHeader.AutoSize = true;
            pnlServicesList.Controls.Add(lblServicesHeader);

            ModernButton btnAddServiceDeco = new ModernButton();
            btnAddServiceDeco.Text = "Add Service";
            btnAddServiceDeco.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnAddServiceDeco.Location = new Point(625, 18);
            btnAddServiceDeco.Size = new Size(115, 28);
            btnAddServiceDeco.NormalColor = Color.White;
            btnAddServiceDeco.BorderColor = colorBorder;
            pnlServicesList.Controls.Add(btnAddServiceDeco);

            // Group: Search/Web
            Label lblGroupWeb = new Label();
            lblGroupWeb.Text = "Web & Engine";
            lblGroupWeb.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblGroupWeb.ForeColor = colorTextDim;
            lblGroupWeb.Location = new Point(25, 65);
            lblGroupWeb.AutoSize = true;
            pnlServicesList.Controls.Add(lblGroupWeb);

            // Row 1: Web Server Row
            pnlRowWeb = new Panel();
            pnlRowWeb.Size = new Size(720, 65);
            pnlRowWeb.Location = new Point(20, 85);
            pnlRowWeb.BackColor = Color.Transparent;
            pnlRowWeb.Paint += DrawCardBorder;
            pnlServicesList.Controls.Add(pnlRowWeb);

            lblWebStatusDot = new Label();
            lblWebStatusDot.Size = new Size(12, 12);
            lblWebStatusDot.Location = new Point(24, 26);
            lblWebStatusDot.BackColor = colorRed;
            lblWebStatusDot.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(lblWebStatusDot.BackColor))
                {
                    e.Graphics.FillEllipse(brush, 1, 1, 8, 8);
                }
            };
            pnlRowWeb.Controls.Add(lblWebStatusDot);

            Label lblWebIcon = new Label();
            lblWebIcon.Text = "\uE774"; // Globe
            lblWebIcon.Font = new Font("Segoe MDL2 Assets", 11f);
            lblWebIcon.ForeColor = Color.FromArgb(56, 189, 248);
            lblWebIcon.Location = new Point(48, 22);
            lblWebIcon.Size = new Size(20, 20);
            lblWebIcon.TextAlign = ContentAlignment.MiddleCenter;
            pnlRowWeb.Controls.Add(lblWebIcon);

            lblWebTitle = new Label();
            lblWebTitle.Text = selectedWebServerType.ToUpper() + " SERVER";
            lblWebTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblWebTitle.ForeColor = colorText;
            lblWebTitle.Location = new Point(80, 12);
            lblWebTitle.AutoSize = true;
            pnlRowWeb.Controls.Add(lblWebTitle);

            lblWebStatusText = new Label();
            lblWebStatusText.Text = "Đang dừng";
            lblWebStatusText.Font = new Font("Segoe UI Semibold", 8.5f);
            lblWebStatusText.ForeColor = colorTextDim;
            lblWebStatusText.Location = new Point(80, 34);
            lblWebStatusText.Size = new Size(300, 20);
            pnlRowWeb.Controls.Add(lblWebStatusText);

            cbWebVersions = new NoScrollComboBox();
            cbWebVersions.BackColor = Color.White;
            cbWebVersions.ForeColor = Color.FromArgb(55, 65, 81);
            cbWebVersions.FlatStyle = FlatStyle.Flat;
            cbWebVersions.DropDownStyle = ComboBoxStyle.DropDownList;
            cbWebVersions.Location = new Point(420, 20);
            cbWebVersions.Size = new Size(130, 25);
            cbWebVersions.SelectedIndexChanged += WebVersionChanged;
            pnlRowWeb.Controls.Add(cbWebVersions);

            btnWebStart = new ModernButton();
            btnWebStart.Text = "Start";
            btnWebStart.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnWebStart.NormalColor = Color.White;
            btnWebStart.HoverColor = Color.FromArgb(240, 253, 244);
            btnWebStart.PressedColor = Color.FromArgb(220, 252, 231);
            btnWebStart.BorderColor = colorBorder;
            btnWebStart.ForeColor = colorGreen; // Emerald Start
            btnWebStart.Location = new Point(570, 18);
            btnWebStart.Size = new Size(68, 28);
            btnWebStart.Click += WebStart_Click;
            pnlRowWeb.Controls.Add(btnWebStart);

            btnWebStop = new ModernButton();
            btnWebStop.Text = "Stop";
            btnWebStop.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnWebStop.NormalColor = Color.White;
            btnWebStop.HoverColor = Color.FromArgb(254, 242, 242);
            btnWebStop.PressedColor = Color.FromArgb(254, 226, 226);
            btnWebStop.BorderColor = colorBorder;
            btnWebStop.ForeColor = colorRed; // Red Stop
            btnWebStop.Location = new Point(570, 18);
            btnWebStop.Size = new Size(68, 28);
            btnWebStop.Enabled = false;
            btnWebStop.Click += WebStop_Click;
            pnlRowWeb.Controls.Add(btnWebStop);

            btnWebConfig = new ModernButton();
            btnWebConfig.Text = "Setup";
            btnWebConfig.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnWebConfig.NormalColor = Color.White;
            btnWebConfig.BorderColor = colorBorder;
            btnWebConfig.ForeColor = Color.FromArgb(75, 85, 99);
            btnWebConfig.Location = new Point(642, 18);
            btnWebConfig.Size = new Size(68, 28);
            btnWebConfig.Click += WebConfig_Click;
            pnlRowWeb.Controls.Add(btnWebConfig);


            // Row 3: PHP Row
            pnlRowPhp = new Panel();
            pnlRowPhp.Size = new Size(720, 65);
            pnlRowPhp.Location = new Point(20, 160);
            pnlRowPhp.BackColor = Color.Transparent;
            pnlRowPhp.Paint += DrawCardBorder;
            pnlServicesList.Controls.Add(pnlRowPhp);

            lblPhpStatusDot = new Label();
            lblPhpStatusDot.Size = new Size(12, 12);
            lblPhpStatusDot.Location = new Point(24, 26);
            lblPhpStatusDot.BackColor = colorRed;
            lblPhpStatusDot.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(lblPhpStatusDot.BackColor))
                {
                    e.Graphics.FillEllipse(brush, 1, 1, 8, 8);
                }
            };
            pnlRowPhp.Controls.Add(lblPhpStatusDot);

            Label lblPhpIcon = new Label();
            lblPhpIcon.Text = "\uE943"; // Code
            lblPhpIcon.Font = new Font("Segoe MDL2 Assets", 11f);
            lblPhpIcon.ForeColor = Color.FromArgb(129, 140, 248);
            lblPhpIcon.Location = new Point(48, 22);
            lblPhpIcon.Size = new Size(20, 20);
            lblPhpIcon.TextAlign = ContentAlignment.MiddleCenter;
            pnlRowPhp.Controls.Add(lblPhpIcon);

            lblPhpTitle = new Label();
            lblPhpTitle.Text = "PHP ENGINE";
            lblPhpTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblPhpTitle.ForeColor = colorText;
            lblPhpTitle.Location = new Point(80, 12);
            lblPhpTitle.AutoSize = true;
            pnlRowPhp.Controls.Add(lblPhpTitle);

            lblPhpStatusText = new Label();
            lblPhpStatusText.Text = "Đang dừng";
            lblPhpStatusText.Font = new Font("Segoe UI Semibold", 8.5f);
            lblPhpStatusText.ForeColor = colorTextDim;
            lblPhpStatusText.Location = new Point(80, 34);
            lblPhpStatusText.Size = new Size(300, 20);
            pnlRowPhp.Controls.Add(lblPhpStatusText);

            cbPhpVersions = new NoScrollComboBox();
            cbPhpVersions.BackColor = Color.White;
            cbPhpVersions.ForeColor = Color.FromArgb(55, 65, 81);
            cbPhpVersions.FlatStyle = FlatStyle.Flat;
            cbPhpVersions.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPhpVersions.Location = new Point(420, 20);
            cbPhpVersions.Size = new Size(130, 25);
            cbPhpVersions.SelectedIndexChanged += PhpVersionChanged;
            pnlRowPhp.Controls.Add(cbPhpVersions);

            btnPhpStart = new ModernButton();
            btnPhpStart.Text = "Start";
            btnPhpStart.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnPhpStart.NormalColor = Color.White;
            btnPhpStart.HoverColor = Color.FromArgb(240, 253, 244);
            btnPhpStart.PressedColor = Color.FromArgb(220, 252, 231);
            btnPhpStart.BorderColor = colorBorder;
            btnPhpStart.ForeColor = colorGreen;
            btnPhpStart.Location = new Point(570, 18);
            btnPhpStart.Size = new Size(68, 28);
            btnPhpStart.Click += PhpStart_Click;
            pnlRowPhp.Controls.Add(btnPhpStart);

            btnPhpStop = new ModernButton();
            btnPhpStop.Text = "Stop";
            btnPhpStop.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnPhpStop.NormalColor = Color.White;
            btnPhpStop.HoverColor = Color.FromArgb(254, 242, 242);
            btnPhpStop.PressedColor = Color.FromArgb(254, 226, 226);
            btnPhpStop.BorderColor = colorBorder;
            btnPhpStop.ForeColor = colorRed;
            btnPhpStop.Location = new Point(570, 18);
            btnPhpStop.Size = new Size(68, 28);
            btnPhpStop.Enabled = false;
            btnPhpStop.Click += PhpStop_Click;
            pnlRowPhp.Controls.Add(btnPhpStop);

            btnPhpConfig = new ModernButton();
            btnPhpConfig.Text = "Setup";
            btnPhpConfig.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnPhpConfig.NormalColor = Color.White;
            btnPhpConfig.BorderColor = colorBorder;
            btnPhpConfig.ForeColor = Color.FromArgb(75, 85, 99);
            btnPhpConfig.Location = new Point(642, 18);
            btnPhpConfig.Size = new Size(68, 28);
            btnPhpConfig.Click += PhpConfig_Click;
            pnlRowPhp.Controls.Add(btnPhpConfig);


            // Group: Databases
            Label lblGroupDb = new Label();
            lblGroupDb.Text = "Database & Storage";
            lblGroupDb.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblGroupDb.ForeColor = colorTextDim;
            lblGroupDb.Location = new Point(25, 240);
            lblGroupDb.AutoSize = true;
            pnlServicesList.Controls.Add(lblGroupDb);

            // Row 2: MySQL Row
            pnlRowMySql = new Panel();
            pnlRowMySql.Size = new Size(720, 65);
            pnlRowMySql.Location = new Point(20, 260);
            pnlRowMySql.BackColor = Color.Transparent;
            pnlRowMySql.Paint += DrawCardBorder;
            pnlServicesList.Controls.Add(pnlRowMySql);

            lblMySqlStatusDot = new Label();
            lblMySqlStatusDot.Size = new Size(12, 12);
            lblMySqlStatusDot.Location = new Point(24, 26);
            lblMySqlStatusDot.BackColor = colorRed;
            lblMySqlStatusDot.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(lblMySqlStatusDot.BackColor))
                {
                    e.Graphics.FillEllipse(brush, 1, 1, 8, 8);
                }
            };
            pnlRowMySql.Controls.Add(lblMySqlStatusDot);

            Label lblMySqlIcon = new Label();
            lblMySqlIcon.Text = "\uEC27"; // Database cylinder
            lblMySqlIcon.Font = new Font("Segoe MDL2 Assets", 11f);
            lblMySqlIcon.ForeColor = Color.FromArgb(251, 191, 36);
            lblMySqlIcon.Location = new Point(48, 22);
            lblMySqlIcon.Size = new Size(20, 20);
            lblMySqlIcon.TextAlign = ContentAlignment.MiddleCenter;
            pnlRowMySql.Controls.Add(lblMySqlIcon);

            lblMySqlTitle = new Label();
            lblMySqlTitle.Text = "MYSQL / MARIADB";
            lblMySqlTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblMySqlTitle.ForeColor = colorText;
            lblMySqlTitle.Location = new Point(80, 12);
            lblMySqlTitle.AutoSize = true;
            pnlRowMySql.Controls.Add(lblMySqlTitle);

            lblMySqlStatusText = new Label();
            lblMySqlStatusText.Text = "Đang dừng";
            lblMySqlStatusText.Font = new Font("Segoe UI Semibold", 8.5f);
            lblMySqlStatusText.ForeColor = colorTextDim;
            lblMySqlStatusText.Location = new Point(80, 34);
            lblMySqlStatusText.Size = new Size(300, 20);
            pnlRowMySql.Controls.Add(lblMySqlStatusText);

            cbMySqlVersions = new NoScrollComboBox();
            cbMySqlVersions.BackColor = Color.White;
            cbMySqlVersions.ForeColor = Color.FromArgb(55, 65, 81);
            cbMySqlVersions.FlatStyle = FlatStyle.Flat;
            cbMySqlVersions.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMySqlVersions.Location = new Point(420, 20);
            cbMySqlVersions.Size = new Size(130, 25);
            cbMySqlVersions.SelectedIndexChanged += MySqlVersionChanged;
            pnlRowMySql.Controls.Add(cbMySqlVersions);

            btnMySqlStart = new ModernButton();
            btnMySqlStart.Text = "Start";
            btnMySqlStart.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnMySqlStart.NormalColor = Color.White;
            btnMySqlStart.HoverColor = Color.FromArgb(240, 253, 244);
            btnMySqlStart.PressedColor = Color.FromArgb(220, 252, 231);
            btnMySqlStart.BorderColor = colorBorder;
            btnMySqlStart.ForeColor = colorGreen;
            btnMySqlStart.Location = new Point(570, 18);
            btnMySqlStart.Size = new Size(68, 28);
            btnMySqlStart.Click += MySqlStart_Click;
            pnlRowMySql.Controls.Add(btnMySqlStart);

            btnMySqlStop = new ModernButton();
            btnMySqlStop.Text = "Stop";
            btnMySqlStop.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnMySqlStop.NormalColor = Color.White;
            btnMySqlStop.HoverColor = Color.FromArgb(254, 242, 242);
            btnMySqlStop.PressedColor = Color.FromArgb(254, 226, 226);
            btnMySqlStop.BorderColor = colorBorder;
            btnMySqlStop.ForeColor = colorRed;
            btnMySqlStop.Location = new Point(570, 18);
            btnMySqlStop.Size = new Size(68, 28);
            btnMySqlStop.Enabled = false;
            btnMySqlStop.Click += MySqlStop_Click;
            pnlRowMySql.Controls.Add(btnMySqlStop);

            btnMySqlConfig = new ModernButton();
            btnMySqlConfig.Text = "Setup";
            btnMySqlConfig.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnMySqlConfig.NormalColor = Color.White;
            btnMySqlConfig.BorderColor = colorBorder;
            btnMySqlConfig.ForeColor = Color.FromArgb(75, 85, 99);
            btnMySqlConfig.Location = new Point(642, 18);
            btnMySqlConfig.Size = new Size(68, 28);
            btnMySqlConfig.Click += MySqlConfig_Click;
            pnlRowMySql.Controls.Add(btnMySqlConfig);


            // Group: Global Action
            Label lblGroupGlobal = new Label();
            lblGroupGlobal.Text = "Global Controls";
            lblGroupGlobal.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblGroupGlobal.ForeColor = colorTextDim;
            lblGroupGlobal.Location = new Point(25, 340);
            lblGroupGlobal.AutoSize = true;
            pnlServicesList.Controls.Add(lblGroupGlobal);

            // Global control actions panel
            Panel pnlRowGlobal = new Panel();
            pnlRowGlobal.Size = new Size(720, 60);
            pnlRowGlobal.Location = new Point(20, 360);
            pnlRowGlobal.BackColor = Color.Transparent;
            pnlRowGlobal.Paint += DrawCardBorder;
            pnlServicesList.Controls.Add(pnlRowGlobal);

            btnStartAll = new ModernButton();
            btnStartAll.Text = "START ALL";
            btnStartAll.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnStartAll.NormalColor = colorGreen;
            btnStartAll.HoverColor = Color.FromArgb(5, 150, 105);
            btnStartAll.BorderColor = Color.Transparent;
            btnStartAll.ForeColor = Color.White;
            btnStartAll.Location = new Point(20, 10);
            btnStartAll.Size = new Size(130, 40);
            btnStartAll.Click += StartAll_Click;
            pnlRowGlobal.Controls.Add(btnStartAll);

            btnStopAll = new ModernButton();
            btnStopAll.Text = "STOP ALL";
            btnStopAll.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnStopAll.NormalColor = colorRed;
            btnStopAll.HoverColor = Color.FromArgb(220, 38, 38);
            btnStopAll.BorderColor = Color.Transparent;
            btnStopAll.ForeColor = Color.White;
            btnStopAll.Location = new Point(160, 10);
            btnStopAll.Size = new Size(130, 40);
            btnStopAll.Click += StopAll_Click;
            pnlRowGlobal.Controls.Add(btnStopAll);

            btnPhpMyAdmin = new ModernButton();
            btnPhpMyAdmin.Text = "DATABASE";
            btnPhpMyAdmin.IconGlyph = "\uE12B"; // Database/Globe
            btnPhpMyAdmin.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnPhpMyAdmin.NormalColor = Color.White;
            btnPhpMyAdmin.BorderColor = colorBorder;
            btnPhpMyAdmin.ForeColor = Color.FromArgb(55, 65, 81);
            btnPhpMyAdmin.Location = new Point(570, 10);
            btnPhpMyAdmin.Size = new Size(130, 40);
            btnPhpMyAdmin.Click += (s, e) => {
                try {
                    string port = txtWebPort.Text.Trim();
                    Process.Start("http://localhost:" + port + "/phpmyadmin");
                } catch { }
            };
            pnlRowGlobal.Controls.Add(btnPhpMyAdmin);


            // Web Root Panel
            Panel pnlRowWebRoot = new Panel();
            pnlRowWebRoot.Size = new Size(720, 50);
            pnlRowWebRoot.Location = new Point(20, 435);
            pnlRowWebRoot.BackColor = Color.Transparent;
            pnlRowWebRoot.Paint += DrawCardBorder;
            pnlServicesList.Controls.Add(pnlRowWebRoot);

            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            lblWebRoot = new Label();
            lblWebRoot.Text = "wwwRoot: " + wwwDir;
            lblWebRoot.ForeColor = colorTextDim;
            lblWebRoot.Font = new Font("Segoe UI Semibold", 8.5f);
            lblWebRoot.Location = new Point(20, 15);
            lblWebRoot.Size = new Size(400, 20);
            lblWebRoot.TextAlign = ContentAlignment.MiddleLeft;
            pnlRowWebRoot.Controls.Add(lblWebRoot);

            btnOpenWebRoot = new ModernButton();
            btnOpenWebRoot.Text = "Open Folder";
            btnOpenWebRoot.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnOpenWebRoot.NormalColor = Color.White;
            btnOpenWebRoot.BorderColor = colorBorder;
            btnOpenWebRoot.ForeColor = Color.FromArgb(75, 85, 99);
            btnOpenWebRoot.Location = new Point(570, 10);
            btnOpenWebRoot.Size = new Size(130, 30);
            btnOpenWebRoot.Click += (s, e) => {
                try { Process.Start("explorer.exe", wwwDir); } catch { }
            };
            pnlRowWebRoot.Controls.Add(btnOpenWebRoot);

            Label lblFooter = new Label();
            lblFooter.Text = "RBW Stack • Portable Server Manager";
            lblFooter.Font = new Font("Segoe UI Italic", 8f);
            lblFooter.ForeColor = colorTextDim;
            lblFooter.Size = new Size(720, 20);
            lblFooter.Location = new Point(20, 500);
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            pnlServicesList.Controls.Add(lblFooter);


            // Recursive composite click registry for services list rows
            RegisterClickRecursive(pnlRowWeb, WebRow_Click);
            RegisterClickRecursive(pnlRowMySql, MySqlRow_Click);
            RegisterClickRecursive(pnlRowPhp, PhpRow_Click);

            // Register hover recursive handlers for cards
            RegisterHoverRecursive(pnlRowWeb, pnlRowWeb);
            RegisterHoverRecursive(pnlRowMySql, pnlRowMySql);
            RegisterHoverRecursive(pnlRowPhp, pnlRowPhp);


            // TAB 2: KHO PHẦN MỀM (width 760)
            pnlTabDownload = new Panel();
            pnlTabDownload.Size = new Size(tabW, tabH);
            pnlTabDownload.Location = tabLoc;
            pnlTabDownload.BackColor = colorBg;
            pnlTabDownload.Padding = new Padding(0, 0, 1, 1);
            pnlTabDownload.Paint += DrawTabPanelBorder;
            pnlTabDownload.Visible = false;
            this.Controls.Add(pnlTabDownload);

            // Copy list of downloads setup directly here
            SetupDownloadRows();

            // Progress panel inside download store - docked at the bottom
            pnlProgress = new Panel();
            pnlProgress.Size = new Size(tabW, 75);
            pnlProgress.Dock = DockStyle.Bottom;
            pnlProgress.BackColor = Color.Transparent;
            pnlProgress.Paint += DrawCardBorder;
            pnlTabDownload.Controls.Add(pnlProgress);

            lblStatusText = new Label();
            lblStatusText.Text = "Đang đồng bộ danh sách phiên bản mới nhất trực tuyến...";
            lblStatusText.ForeColor = colorText;
            lblStatusText.Font = new Font("Segoe UI Semibold", 9f);
            lblStatusText.Location = new Point(20, 12);
            lblStatusText.Size = new Size(690, 20);
            pnlProgress.Controls.Add(lblStatusText);

            pbDownload = new ProgressBar();
            pbDownload.Location = new Point(20, 38);
            pbDownload.Size = new Size(690, 22);
            pbDownload.Style = ProgressBarStyle.Marquee;
            pbDownload.MarqueeAnimationSpeed = 50;
            pnlProgress.Controls.Add(pbDownload);

            // Create Cards Container - docked to fill with AutoScroll enabled
            pnlCardsContainer = new Panel();
            pnlCardsContainer.Dock = DockStyle.Fill;
            pnlCardsContainer.AutoScroll = true;
            pnlCardsContainer.BackColor = colorBg;
            pnlTabDownload.Controls.Add(pnlCardsContainer);

            // Initially render cards (offline fallbacks)
            RenderDownloadCards();


            // TAB 3: CÀI ĐẶT
            pnlTabSettings = new Panel();
            pnlTabSettings.Size = new Size(tabW, tabH);
            pnlTabSettings.Location = tabLoc;
            pnlTabSettings.BackColor = colorBg;
            pnlTabSettings.Padding = new Padding(0, 0, 1, 1);
            pnlTabSettings.Paint += DrawTabPanelBorder;
            pnlTabSettings.Visible = false;
            this.Controls.Add(pnlTabSettings);

            // Card 1: Cấu hình Hệ thống (System Configuration)
            Panel pnlSysBox = new Panel();
            pnlSysBox.Size = new Size(345, 415);
            pnlSysBox.Location = new Point(20, 20);
            pnlSysBox.BackColor = Color.Transparent;
            pnlTabSettings.Controls.Add(pnlSysBox);
            ApplyRoundedRegion(pnlSysBox, 12);

            pnlSysBox.Paint += (s, p) => {
                p.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Draw white background card
                using (var bgBrush = new SolidBrush(Color.White))
                    p.Graphics.FillRectangle(bgBrush, 0, 0, pnlSysBox.Width, pnlSysBox.Height);

                // Draw gray card border
                using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    p.Graphics.DrawRectangle(borderPen, 0, 0, pnlSysBox.Width - 1, pnlSysBox.Height - 1);

                // Draw accent top bar (Blue-600)
                using (var accentBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    p.Graphics.FillRectangle(accentBrush, 0, 0, pnlSysBox.Width, 4);

                // Draw a beautiful system settings icon/indicator next to title
                using (var iconBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    p.Graphics.FillRectangle(iconBrush, 20, 22, 4, 16);
            };

            Label lblSysTitle = new Label();
            lblSysTitle.Text = "CÀI ĐẶT HỆ THỐNG";
            lblSysTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblSysTitle.ForeColor = Color.FromArgb(30, 41, 59); // Slate 800
            lblSysTitle.Location = new Point(32, 20);
            lblSysTitle.Size = new Size(295, 20);
            pnlSysBox.Controls.Add(lblSysTitle);

            // Card 2: Cấu hình Demo Hosting
            Panel pnlDemoBox = new Panel();
            pnlDemoBox.Size = new Size(345, 415);
            pnlDemoBox.Location = new Point(385, 20);
            pnlDemoBox.BackColor = Color.Transparent;
            pnlTabSettings.Controls.Add(pnlDemoBox);
            ApplyRoundedRegion(pnlDemoBox, 12);

            pnlDemoBox.Paint += (s, p) => {
                p.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Draw white background card
                using (var bgBrush = new SolidBrush(Color.White))
                    p.Graphics.FillRectangle(bgBrush, 0, 0, pnlDemoBox.Width, pnlDemoBox.Height);

                // Draw gray card border
                using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    p.Graphics.DrawRectangle(borderPen, 0, 0, pnlDemoBox.Width - 1, pnlDemoBox.Height - 1);

                // Draw accent top bar (Purple-600)
                using (var accentBrush = new SolidBrush(Color.FromArgb(139, 92, 246)))
                    p.Graphics.FillRectangle(accentBrush, 0, 0, pnlDemoBox.Width, 4);

                // Draw a beautiful demo hosting icon/indicator next to title
                using (var iconBrush = new SolidBrush(Color.FromArgb(139, 92, 246)))
                    p.Graphics.FillRectangle(iconBrush, 20, 22, 4, 16);
            };

            Label lblDemoTitle = new Label();
            lblDemoTitle.Text = "DEPLOY DEMO HOSTING";
            lblDemoTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblDemoTitle.ForeColor = Color.FromArgb(30, 41, 59); // Slate 800
            lblDemoTitle.Location = new Point(32, 20);
            lblDemoTitle.Size = new Size(295, 20);
            pnlDemoBox.Controls.Add(lblDemoTitle);

            // Custom styled input helper for both cards
            Color dfNormalBorder = Color.FromArgb(210, 215, 235);
            Color dfFocusBorder  = Color.FromArgb(139, 92, 246);
            Color dfInputBg      = Color.FromArgb(250, 250, 255);
            Color dfLblColor     = Color.FromArgb(100, 110, 155);

             Func<Panel, string, TextBox, string, int, int, int, int, Action<string>, Panel> addCustomField = null;
             addCustomField = (parent, ltext, txtBox, cfgKey, dx, dy, dw, dh, setter) => {
                 var lbl2 = new Label();
                 lbl2.Text = ltext;
                 lbl2.ForeColor = dfLblColor;
                 lbl2.Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
                 lbl2.Location = new Point(dx, dy);
                 lbl2.Size = new Size(dw, 15);
                 parent.Controls.Add(lbl2);
 
                 bool dfFocused = false;
                 Color dfNB = dfNormalBorder;
                 Color dfFB = dfFocusBorder;
                 Color dfBg = dfInputBg;
 
                 var wrap2 = new Panel();
                 wrap2.Location = new Point(dx, dy + 18);
                 wrap2.Size = new Size(dw, dh);
                 wrap2.BackColor = dfBg;
                 wrap2.Paint += (sp, pp) => {
                     pp.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                     Color bc2 = dfFocused ? dfFB : dfNB;
                     using (var pen2 = new Pen(bc2, 1f))
                         pp.Graphics.DrawRectangle(pen2, 0, 0, wrap2.Width - 1, wrap2.Height - 1);
                     if (dfFocused)
                         using (var pen2 = new Pen(Color.FromArgb(35, dfFB.R, dfFB.G, dfFB.B), 3f))
                             pp.Graphics.DrawRectangle(pen2, 1, 1, wrap2.Width - 3, wrap2.Height - 3);
                 };
 
                 txtBox.Location = new Point(8, (dh - 18) / 2);
                 txtBox.Size = new Size(dw - 16, 18);
                 txtBox.BorderStyle = BorderStyle.None;
                 txtBox.BackColor = dfBg;
                 txtBox.ForeColor = Color.FromArgb(30, 40, 95);
                 txtBox.Font = new Font("Segoe UI", 9f);
                 txtBox.Enter += (se, ee) => { dfFocused = true;  wrap2.Invalidate(); };
                 txtBox.Leave += (se, ee) => { dfFocused = false; wrap2.Invalidate(); };
 
                 if (txtBox.ReadOnly)
                 {
                     wrap2.BackColor = Color.FromArgb(241, 245, 249);
                     txtBox.BackColor = Color.FromArgb(241, 245, 249);
                 }
 
                 if (!string.IsNullOrEmpty(cfgKey))
                 {
                     var gcfg = DeployDemoForm.LoadGlobalConfig();
                     if (gcfg.ContainsKey(cfgKey)) txtBox.Text = gcfg[cfgKey];
                 }
                 
                 txtBox.TextChanged += (s3, e3) => {
                     if (setter != null) setter(txtBox.Text);
                 };
 
                 wrap2.Controls.Add(txtBox);
                 wrap2.Click += (sc, ec) => txtBox.Focus();
                 parent.Controls.Add(wrap2);
                 ApplyRoundedRegion(wrap2, 6);
                 return wrap2;
             };

            // ── CARD 1 FIELDS (SYSTEM SETTINGS) ─────────────────────────
            Label lblType = new Label();
            lblType.Text = "Web Server mặc định";
            lblType.ForeColor = dfLblColor;
            lblType.Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            lblType.Location = new Point(20, 50);
            lblType.Size = new Size(305, 15);
            pnlSysBox.Controls.Add(lblType);

            Panel pnlComboWrap = new Panel();
            pnlComboWrap.Location = new Point(20, 68);
            pnlComboWrap.Size = new Size(305, 30);
            pnlComboWrap.BackColor = dfInputBg;
            pnlComboWrap.Paint += (s, p) => {
                using (var pen = new Pen(dfNormalBorder, 1f))
                    p.Graphics.DrawRectangle(pen, 0, 0, pnlComboWrap.Width - 1, pnlComboWrap.Height - 1);
            };
            cbWebServerType = new NoScrollComboBox();
            cbWebServerType.Items.AddRange(new string[] { "Apache", "Nginx" });
            cbWebServerType.SelectedItem = selectedWebServerType;
            cbWebServerType.BackColor = dfInputBg;
            cbWebServerType.ForeColor = Color.FromArgb(30, 40, 95);
            cbWebServerType.FlatStyle = FlatStyle.Flat;
            cbWebServerType.DropDownStyle = ComboBoxStyle.DropDownList;
            cbWebServerType.Font = new Font("Segoe UI", 9f);
            cbWebServerType.Location = new Point(8, 4);
            cbWebServerType.Size = new Size(289, 22);
            pnlComboWrap.Controls.Add(cbWebServerType);
            pnlSysBox.Controls.Add(pnlComboWrap);
            ApplyRoundedRegion(pnlComboWrap, 6);

            chkAutoStart = new CheckBox();
            chkAutoStart.Text = "Khởi động cùng Windows (Auto Start)";
            chkAutoStart.ForeColor = Color.FromArgb(55, 65, 81);
            chkAutoStart.Font = new Font("Segoe UI", 9f);
            chkAutoStart.Location = new Point(20, 100);
            chkAutoStart.Size = new Size(305, 22);
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.FlatStyle = FlatStyle.Flat;
            chkAutoStart.FlatAppearance.BorderSize = 1;
            chkAutoStart.FlatAppearance.BorderColor = dfNormalBorder;
            pnlSysBox.Controls.Add(chkAutoStart);

            chkMinimizeToTray = new CheckBox();
            chkMinimizeToTray.Text = "Chạy ẩn dưới khay hệ thống khi thu nhỏ";
            chkMinimizeToTray.ForeColor = Color.FromArgb(55, 65, 81);
            chkMinimizeToTray.Font = new Font("Segoe UI", 9f);
            chkMinimizeToTray.Location = new Point(20, 122);
            chkMinimizeToTray.Size = new Size(305, 22);
            chkMinimizeToTray.Checked = LoadMinimizeToTraySetting();
            chkMinimizeToTray.FlatStyle = FlatStyle.Flat;
            chkMinimizeToTray.FlatAppearance.BorderSize = 1;
            chkMinimizeToTray.FlatAppearance.BorderColor = dfNormalBorder;
            pnlSysBox.Controls.Add(chkMinimizeToTray);

            chkAutoOptimizePhpIni = new CheckBox();
            chkAutoOptimizePhpIni.Text = "Tự động tối ưu php.ini khi Start (Khuyên dùng)";
            chkAutoOptimizePhpIni.ForeColor = Color.FromArgb(55, 65, 81);
            chkAutoOptimizePhpIni.Font = new Font("Segoe UI", 9f);
            chkAutoOptimizePhpIni.Location = new Point(20, 144);
            chkAutoOptimizePhpIni.Size = new Size(305, 22);
            chkAutoOptimizePhpIni.Checked = LoadAutoOptimizePhpIniSetting();
            chkAutoOptimizePhpIni.FlatStyle = FlatStyle.Flat;
            chkAutoOptimizePhpIni.FlatAppearance.BorderSize = 1;
            chkAutoOptimizePhpIni.FlatAppearance.BorderColor = dfNormalBorder;
            pnlSysBox.Controls.Add(chkAutoOptimizePhpIni);

            chkAdminVHostMode = new CheckBox();
            chkAdminVHostMode.Text = "Chạy quyền Admin (Sửa file hosts, đuôi .local)";
            chkAdminVHostMode.ForeColor = Color.FromArgb(55, 65, 81);
            chkAdminVHostMode.Font = new Font("Segoe UI", 9f);
            chkAdminVHostMode.Location = new Point(20, 166);
            chkAdminVHostMode.Size = new Size(305, 22);
            chkAdminVHostMode.Checked = IsAdminVHostMode();
            chkAdminVHostMode.FlatStyle = FlatStyle.Flat;
            chkAdminVHostMode.FlatAppearance.BorderSize = 1;
            chkAdminVHostMode.FlatAppearance.BorderColor = dfNormalBorder;
            pnlSysBox.Controls.Add(chkAdminVHostMode);

            txtWebPort = new TextBox();
            addCustomField(pnlSysBox, "Cổng Port Web Server", txtWebPort, "", 20, 196, 145, 30, null);

            txtVHostSuffix = new TextBox();
            addCustomField(pnlSysBox, "Đuôi tên miền ảo", txtVHostSuffix, "vhost_suffix", 180, 196, 145, 30, v => {
                var c = DeployDemoForm.LoadGlobalConfig();
                c["vhost_suffix"] = v;
                DeployDemoForm.SaveGlobalConfig(c);
            });
            if (string.IsNullOrEmpty(txtVHostSuffix.Text)) txtVHostSuffix.Text = "local";

            txtMySqlPort = new TextBox();
            addCustomField(pnlSysBox, "Cổng Port MySQL / MariaDB", txtMySqlPort, "", 20, 250, 305, 30, null);

            txtPhpPort = new TextBox();
            txtPhpPort.ReadOnly = true;
            addCustomField(pnlSysBox, "Cổng Port PHP-CGI (Chỉ đọc)", txtPhpPort, "", 20, 304, 305, 30, null);

            btnSaveSettings = new ModernButton();
            btnSaveSettings.Text = "Lưu cài đặt hệ thống";
            btnSaveSettings.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            btnSaveSettings.NormalColor = Color.FromArgb(37, 99, 235);
            btnSaveSettings.HoverColor = Color.FromArgb(29, 78, 216);
            btnSaveSettings.PressedColor = Color.FromArgb(30, 64, 175);
            btnSaveSettings.BorderColor = Color.Transparent;
            btnSaveSettings.ForeColor = Color.White;
            btnSaveSettings.CornerRadius = 6;
            btnSaveSettings.Location = new Point(20, 362);
            btnSaveSettings.Size = new Size(305, 36);
            btnSaveSettings.Click += (s, e) => {
                SaveSettings_Click(null, null);
                try {
                    string webPort = txtWebPort.Text.Trim();
                    string mysqlPort = txtMySqlPort.Text.Trim();
                    if (selectedWebServerType == "Apache")
                        SavePortToFile(pathApacheConf, @"Listen\s+\d+", webPort, "Listen {0}");
                    else
                        SavePortToFile(pathNginxConf, @"listen\s+\d+;", webPort, "listen {0};");
                    SavePortToFile(pathMySqlConf, @"port\s*=\s*\d+", mysqlPort, "port = {0}");
                    UpdatePortsDisplay();
                } catch { }
            };
            pnlSysBox.Controls.Add(btnSaveSettings);


            // ── CARD 2 FIELDS (DEMO HOSTING) ────────────────────────────
            var txtFtpHost = new TextBox();
            addCustomField(pnlDemoBox, "HOST / IP (FTP)", txtFtpHost, "ftp_host", 20, 50, 305, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["ftp_host"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            var txtFtpUser = new TextBox();
            addCustomField(pnlDemoBox, "FTP USER", txtFtpUser, "ftp_user", 20, 102, 305, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["ftp_user"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            var txtFtpPass = new TextBox();
            txtFtpPass.UseSystemPasswordChar = true;
            var wrapPass = addCustomField(pnlDemoBox, "PASSWORD (FTP/DA)", txtFtpPass, "ftp_pass", 20, 154, 305, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["ftp_pass"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            // Password Toggle Button
            Button btnTogglePass = new Button();
            btnTogglePass.Text = "👁";
            btnTogglePass.Font = new Font("Segoe UI", 9f);
            btnTogglePass.Size = new Size(24, 20);
            btnTogglePass.Location = new Point(273, 5);
            btnTogglePass.FlatStyle = FlatStyle.Flat;
            btnTogglePass.FlatAppearance.BorderSize = 0;
            btnTogglePass.BackColor = dfInputBg;
            btnTogglePass.ForeColor = Color.FromArgb(139, 92, 246);
            btnTogglePass.Cursor = Cursors.Hand;
            btnTogglePass.Click += (s, e) => {
                txtFtpPass.UseSystemPasswordChar = !txtFtpPass.UseSystemPasswordChar;
                btnTogglePass.Text = txtFtpPass.UseSystemPasswordChar ? "👁" : "🙈";
            };
            txtFtpPass.Width = 305 - 16 - 28;
            wrapPass.Controls.Add(btnTogglePass);

            var txtFtpRoot = new TextBox();
            addCustomField(pnlDemoBox, "FTP ROOT PATH", txtFtpRoot, "ftp_root", 20, 206, 305, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["ftp_root"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            var txtWebDomain = new TextBox();
            addCustomField(pnlDemoBox, "WEB DOMAIN", txtWebDomain, "web_domain", 20, 258, 305, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["web_domain"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            var txtDaUser = new TextBox();
            addCustomField(pnlDemoBox, "DA USER", txtDaUser, "da_user", 20, 310, 145, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["da_user"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            var txtDaPort = new TextBox();
            addCustomField(pnlDemoBox, "DA PORT", txtDaPort, "da_port", 180, 310, 145, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["da_port"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            var txtFontSource = new TextBox();
            addCustomField(pnlDemoBox, "THƯ MỤC FONT LOCAL", txtFontSource, "font_source_path", 20, 362, 305, 30, v => { var c=DeployDemoForm.LoadGlobalConfig(); c["font_source_path"]=v; DeployDemoForm.SaveGlobalConfig(c); });

            // TAB 4: ABOUT PANEL
            pnlTabAbout = new Panel();
            pnlTabAbout.Size = new Size(tabW, tabH);
            pnlTabAbout.Location = tabLoc;
            pnlTabAbout.BackColor = colorBg;
            pnlTabAbout.Padding = new Padding(0, 0, 1, 1);
            pnlTabAbout.Paint += DrawTabPanelBorder;
            pnlTabAbout.Visible = false;
            this.Controls.Add(pnlTabAbout);

            // TAB 4.5: MAIL SANDBOX PANEL
            pnlTabMail = new Panel();
            pnlTabMail.Size = new Size(tabW, tabH);
            pnlTabMail.Location = tabLoc;
            pnlTabMail.BackColor = colorBg;
            pnlTabMail.Padding = new Padding(0, 0, 1, 1);
            pnlTabMail.Paint += DrawTabPanelBorder;
            pnlTabMail.Visible = false;
            this.Controls.Add(pnlTabMail);

            Panel pnlMailBox = new Panel();
            pnlMailBox.Size = new Size(720, 510);
            pnlMailBox.Location = new Point(20, 20);
            pnlMailBox.BackColor = Color.White;
            pnlMailBox.Paint += DrawCardBorder;
            pnlTabMail.Controls.Add(pnlMailBox);
            ApplyRoundedRegion(pnlMailBox, 12);

            // Left panel for Mail List
            Panel pnlMailLeft = new Panel();
            pnlMailLeft.Size = new Size(260, 510);
            pnlMailLeft.Location = new Point(0, 0);
            pnlMailLeft.BackColor = Color.White;
            pnlMailLeft.Paint += (s, pe) => {
                using (Pen pen = new Pen(Color.FromArgb(241, 245, 249), 1.5f))
                {
                    pe.Graphics.DrawLine(pen, 259, 0, 259, pnlMailLeft.Height);
                }
            };
            pnlMailBox.Controls.Add(pnlMailLeft);

            // Top control bar in Left Panel
            Label lblMailTitle = new Label();
            lblMailTitle.Text = "Hộp thư";
            lblMailTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblMailTitle.ForeColor = Color.FromArgb(30, 41, 59); // Slate 800
            lblMailTitle.Location = new Point(15, 12);
            lblMailTitle.AutoSize = true;
            pnlMailLeft.Controls.Add(lblMailTitle);

            ModernButton btnClearMails = new ModernButton();
            btnClearMails.Text = "Xóa hết";
            btnClearMails.Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            btnClearMails.Location = new Point(180, 12);
            btnClearMails.Size = new Size(65, 24);
            btnClearMails.NormalColor = Color.White;
            btnClearMails.BorderColor = colorBorder;
            btnClearMails.Click += (s, e) => {
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa toàn bộ email đã bắt được?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    SaveCaughtEmails(new List<CaughtEmail>());
                    RefreshMailList();
                }
            };
            pnlMailLeft.Controls.Add(btnClearMails);

            // ListBox of mails
            lstMailInbox = new ListBox();
            lstMailInbox.Location = new Point(10, 50);
            lstMailInbox.Size = new Size(240, 440);
            lstMailInbox.BorderStyle = BorderStyle.None;
            lstMailInbox.Font = new Font("Segoe UI", 9f);
            lstMailInbox.ForeColor = colorText;
            lstMailInbox.DrawMode = DrawMode.OwnerDrawFixed;
            lstMailInbox.ItemHeight = 44;
            lstMailInbox.DrawItem += LstMailInbox_DrawItem;
            lstMailInbox.SelectedIndexChanged += LstMailInbox_SelectedIndexChanged;
            pnlMailLeft.Controls.Add(lstMailInbox);

            // Right panel for Mail Details
            pnlMailDetail = new Panel();
            pnlMailDetail.Size = new Size(720 - 260, 510);
            pnlMailDetail.Location = new Point(260, 0);
            pnlMailDetail.BackColor = Color.White;
            pnlMailDetail.Visible = false;
            pnlMailBox.Controls.Add(pnlMailDetail);

            // Detail headers
            lblMailSubject = new Label();
            lblMailSubject.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblMailSubject.ForeColor = Color.FromArgb(30, 41, 59);
            lblMailSubject.Location = new Point(20, 15);
            lblMailSubject.Size = new Size(350, 20);
            pnlMailDetail.Controls.Add(lblMailSubject);

            ModernButton btnZoomMail = new ModernButton();
            btnZoomMail.Text = "Xem lớn ↗";
            btnZoomMail.Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            btnZoomMail.Location = new Point(380, 12);
            btnZoomMail.Size = new Size(65, 24);
            btnZoomMail.NormalColor = Color.White;
            btnZoomMail.BorderColor = colorBorder;
            btnZoomMail.Click += (s, e) => {
                if (lstMailInbox.SelectedIndex != -1)
                {
                    var email = lstMailInbox.SelectedItem as CaughtEmail;
                    if (email != null)
                    {
                        using (var form = new MailDetailForm(email))
                        {
                            form.ShowDialog(this);
                        }
                    }
                }
            };
            pnlMailDetail.Controls.Add(btnZoomMail);

            lblMailFrom = new Label();
            lblMailFrom.Font = new Font("Segoe UI", 8.5f);
            lblMailFrom.ForeColor = colorTextDim;
            lblMailFrom.Location = new Point(20, 40);
            lblMailFrom.Size = new Size(410, 15);
            pnlMailDetail.Controls.Add(lblMailFrom);

            lblMailTo = new Label();
            lblMailTo.Font = new Font("Segoe UI", 8.5f);
            lblMailTo.ForeColor = colorTextDim;
            lblMailTo.Location = new Point(20, 58);
            lblMailTo.Size = new Size(410, 15);
            pnlMailDetail.Controls.Add(lblMailTo);

            lblMailDate = new Label();
            lblMailDate.Font = new Font("Segoe UI", 8f);
            lblMailDate.ForeColor = colorTextDim;
            lblMailDate.Location = new Point(20, 76);
            lblMailDate.Size = new Size(410, 15);
            pnlMailDetail.Controls.Add(lblMailDate);

            // WebBrowser for Mail Body
            webMailBody = new WebBrowser();
            webMailBody.Location = new Point(20, 100);
            webMailBody.Size = new Size(720 - 260 - 40, 390);
            webMailBody.ScriptErrorsSuppressed = true;
            webMailBody.Navigating += (s, e) => {
                string urlObj = e.Url.ToString();
                if (urlObj != "about:blank")
                {
                    e.Cancel = true;
                    if (urlObj.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        urlObj.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        try { System.Diagnostics.Process.Start(urlObj); } catch { }
                    }
                }
            };
            pnlMailDetail.Controls.Add(webMailBody);

            // Empty state label for Right panel
            lblMailEmpty = new Label();
            lblMailEmpty.Text = "Chọn một email từ danh sách để xem chi tiết";
            lblMailEmpty.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            lblMailEmpty.ForeColor = colorTextDim;
            lblMailEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblMailEmpty.Size = new Size(720 - 260, 510);
            lblMailEmpty.Location = new Point(260, 0);
            pnlMailBox.Controls.Add(lblMailEmpty);

            Panel pnlAboutBox = new Panel();
            pnlAboutBox.Size = new Size(720, 420);
            pnlAboutBox.Location = new Point(20, 20);
            pnlAboutBox.BackColor = Color.Transparent;
            pnlAboutBox.Paint += DrawCardBorder;
            pnlTabAbout.Controls.Add(pnlAboutBox);
            ApplyRoundedRegion(pnlAboutBox, 12);

            Label lblAboutTitle = new Label();
            lblAboutTitle.Text = "ABOUT RBWSTACK MANAGER";
            lblAboutTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblAboutTitle.ForeColor = colorText;
            lblAboutTitle.Location = new Point(30, 30);
            lblAboutTitle.Size = new Size(400, 30);
            pnlAboutBox.Controls.Add(lblAboutTitle);

            Label lblAboutDesc = new Label();
            lblAboutDesc.Text = "RBWStack là giải pháp quản lý máy chủ PHP bỏ túi (Portable PHP Stack) siêu nhanh,\r\n" +
                               "được xây dựng dựa trên triết lý tối giản, gọn nhẹ và độ ổn định cao nhất.\r\n\r\n" +
                               "• Phiên bản: v2.1.1 (RBW Pro Edition)\r\n" +
                               "• Được tối ưu hóa cấu hình tự động (Nginx, Apache, PHP, MySQL, phpMyAdmin)\r\n" +
                               "• Hỗ trợ tải xuống và trích xuất offline tự động vượt tường lửa Akamai CDN.\r\n" +
                               "• Hệ thống quản lý Mutex thông minh ngăn chặn đụng độ tiến trình.\r\n\r\n" +
                               "Cảm ơn bạn đã lựa chọn RBWStack để đồng hành cùng các dự án Web của mình!";
            lblAboutDesc.Font = new Font("Segoe UI", 10f);
            lblAboutDesc.ForeColor = colorTextDim;
            lblAboutDesc.Location = new Point(30, 80);
            lblAboutDesc.Size = new Size(660, 220);
            pnlAboutBox.Controls.Add(lblAboutDesc);

            ModernButton btnCheckUpdate = new ModernButton();
            btnCheckUpdate.Text = "Kiểm tra cập nhật 🔄";
            btnCheckUpdate.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnCheckUpdate.Location = new Point(30, 310);
            btnCheckUpdate.Size = new Size(180, 35);
            btnCheckUpdate.NormalColor = Color.White;
            btnCheckUpdate.HoverColor = Color.FromArgb(243, 244, 246);
            btnCheckUpdate.ForeColor = Color.FromArgb(59, 130, 246);
            btnCheckUpdate.BorderColor = Color.FromArgb(59, 130, 246);
            btnCheckUpdate.CornerRadius = 6;
            btnCheckUpdate.Click += (s, e) => {
                btnCheckUpdate.Text = "Đang kiểm tra... ⏳";
                btnCheckUpdate.Enabled = false;
                
                System.Threading.Tasks.Task.Run(() => {
                    try
                    {
                        var release = CheckForUpdatesCached("rambowoon/RBWStack", true);
                        this.BeginInvoke((MethodInvoker)delegate {
                            btnCheckUpdate.Text = "Kiểm tra cập nhật 🔄";
                            btnCheckUpdate.Enabled = true;
                            
                            if (release != null)
                            {
                                if (!release.TagName.Equals("v2.1.1", StringComparison.OrdinalIgnoreCase))
                                {
                                    ShowUpdatePrompt(release);
                                }
                                else
                                {
                                    MessageBox.Show("Bạn đang sử dụng phiên bản mới nhất (v2.1.1).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Không thể kiểm tra cập nhật vào lúc này. Vui lòng kiểm tra lại kết nối mạng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        });
                    }
                    catch { }
                });
            };
            pnlAboutBox.Controls.Add(btnCheckUpdate);


            // TAB 5: PLACEHOLDER PANEL
            pnlTabPlaceholder = new Panel();
            pnlTabPlaceholder.Size = new Size(tabW, tabH);
            pnlTabPlaceholder.Location = tabLoc;
            pnlTabPlaceholder.BackColor = colorBg;
            pnlTabPlaceholder.Padding = new Padding(0, 0, 1, 1);
            pnlTabPlaceholder.Paint += DrawTabPanelBorder;
            pnlTabPlaceholder.Visible = false;
            this.Controls.Add(pnlTabPlaceholder);

            Panel pnlPlaceholderBox = new Panel();
            pnlPlaceholderBox.Size = new Size(720, 420);
            pnlPlaceholderBox.Location = new Point(20, 20);
            pnlPlaceholderBox.BackColor = Color.Transparent;
            pnlPlaceholderBox.Paint += DrawCardBorder;
            pnlTabPlaceholder.Controls.Add(pnlPlaceholderBox);
            ApplyRoundedRegion(pnlPlaceholderBox, 12);

            lblPlaceholderTitle = new Label();
            lblPlaceholderTitle.Text = "Tính năng này đang được phát triển";
            lblPlaceholderTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblPlaceholderTitle.ForeColor = colorText;
            lblPlaceholderTitle.Location = new Point(30, 30);
            lblPlaceholderTitle.Size = new Size(600, 30);
            pnlPlaceholderBox.Controls.Add(lblPlaceholderTitle);

            lblPlaceholderDesc = new Label();
            lblPlaceholderDesc.Text = "Tính năng này sẽ được tích hợp trực tiếp trong phiên bản tiếp theo của RBWStack.\r\n" +
                                      "Vui lòng nâng cấp lên phiên bản Pro hoặc theo dõi các bản cập nhật mới nhất!";
            lblPlaceholderDesc.Font = new Font("Segoe UI", 10f);
            lblPlaceholderDesc.ForeColor = colorTextDim;
            lblPlaceholderDesc.Location = new Point(30, 80);
            lblPlaceholderDesc.Size = new Size(660, 100);
            pnlPlaceholderBox.Controls.Add(lblPlaceholderDesc);

            // Default display Tab
            SwitchToTab("dashboard");

            // Fetch dynamic online dropdowns in background
            LoadLatestVersionsFromWeb();
        }

        private void SwitchToTab(string tabName)
        {
            pnlTabDashboard.Visible = (tabName == "dashboard");
            pnlTabDownload.Visible = (tabName == "download");
            pnlTabSettings.Visible = (tabName == "settings");
            pnlTabAbout.Visible = (tabName == "about");
            pnlTabPlaceholder.Visible = (tabName == "placeholder");
            pnlTabSites.Visible = (tabName == "sites");
            if (pnlTabMail != null) pnlTabMail.Visible = (tabName == "mail");

            btnTabDashboard.IsSelected = (tabName == "dashboard");
            btnTabDownload.IsSelected = (tabName == "download");
            btnTabSettings.IsSelected = (tabName == "settings");
            btnTabAbout.IsSelected = (tabName == "about");

            btnTabSites.IsSelected = (tabName == "sites");
            btnTabPHP.IsSelected = false;
            btnTabNode.IsSelected = false;
            btnTabExpose.IsSelected = false;
            btnTabShortcuts.IsSelected = false;
            btnTabMail.IsSelected = (tabName == "mail");
            btnTabDumps.IsSelected = false;

            btnTabPhpEngine.IsSelected = false;
            btnTabApache.IsSelected = false;
            btnTabNginx.IsSelected = false;
            btnTabDatabase.IsSelected = false;
            btnTabPhpMyAdmin.IsSelected = false;
            btnTabVcRuntime.IsSelected = false;

            if (tabName == "placeholder_sites") btnTabSites.IsSelected = true;
            else if (tabName == "placeholder_php") btnTabPHP.IsSelected = true;
            else if (tabName == "placeholder_node") btnTabNode.IsSelected = true;
            else if (tabName == "placeholder_expose") btnTabExpose.IsSelected = true;
            else if (tabName == "placeholder_shortcuts") btnTabShortcuts.IsSelected = true;
            else if (tabName == "placeholder_dumps") btnTabDumps.IsSelected = true;

            if (tabName == "download")
            {
                activeFilterComponentName = "ALL";
                RenderDownloadCards();
            }
            else if (tabName == "sites")
            {
                RenderSitesList();
            }
            else if (tabName == "mail")
            {
                RefreshMailList();
            }

            // Trigger updates on visible components
            if (tabName == "dashboard")
            {
                UpdatePortsDisplay();
            }

            if (pnlSidebar != null) pnlSidebar.BringToFront();
        }

        private void FilterDownloadCards(string targetComponentName)
        {
            // First show the download tab
            pnlTabDownload.Visible = true;
            
            // Turn off other tabs
            pnlTabDashboard.Visible = false;
            pnlTabSettings.Visible = false;
            pnlTabAbout.Visible = false;
            pnlTabPlaceholder.Visible = false;
            if (pnlTabSites != null) pnlTabSites.Visible = false;
            if (pnlTabMail != null) pnlTabMail.Visible = false;

            // Update selections
            btnTabDashboard.IsSelected = false;
            btnTabDownload.IsSelected = (targetComponentName == "ALL");
            btnTabSettings.IsSelected = false;
            btnTabAbout.IsSelected = false;
            btnTabSites.IsSelected = false;
            btnTabPHP.IsSelected = false;
            btnTabNode.IsSelected = (targetComponentName == "Node.js Engine");
            btnTabExpose.IsSelected = false;
            btnTabShortcuts.IsSelected = false;
            btnTabMail.IsSelected = false;
            btnTabDumps.IsSelected = false;

            btnTabPhpEngine.IsSelected = (targetComponentName == "PHP Engine (x64 Thread Safe)");
            btnTabApache.IsSelected = (targetComponentName == "Apache Web Server (httpd)");
            btnTabNginx.IsSelected = (targetComponentName == "Nginx Web Server");
            btnTabDatabase.IsSelected = (targetComponentName == "Cơ sở dữ liệu (MySQL / MariaDB)");
            btnTabPhpMyAdmin.IsSelected = (targetComponentName == "phpMyAdmin Web Database Client");
            btnTabVcRuntime.IsSelected = (targetComponentName == "Microsoft Visual C++ Runtime (Thư viện nền bắt buộc)");

            activeFilterComponentName = targetComponentName;
            RenderDownloadCards();

            if (pnlSidebar != null) pnlSidebar.BringToFront();
        }

        private void ShowPlaceholderTab(string tabTitle, string tabDesc, string activeBtnName)
        {
            SwitchToTab("placeholder");
            lblPlaceholderTitle.Text = tabTitle;
            lblPlaceholderDesc.Text = tabDesc;

            if (activeBtnName == "sites") btnTabSites.IsSelected = true;
            else if (activeBtnName == "php") btnTabPHP.IsSelected = true;
            else if (activeBtnName == "node") btnTabNode.IsSelected = true;
            else if (activeBtnName == "expose") btnTabExpose.IsSelected = true;
            else if (activeBtnName == "shortcuts") btnTabShortcuts.IsSelected = true;
            else if (activeBtnName == "mail") btnTabMail.IsSelected = true;
            else if (activeBtnName == "dumps") btnTabDumps.IsSelected = true;
        }

        public string GetWebPort()
        {
            return txtWebPort != null ? txtWebPort.Text.Trim() : "80";
        }

        public string GetMySqlPort()
        {
            return txtMySqlPort != null ? txtMySqlPort.Text.Trim() : "3306";
        }

        public void ReloadAllVersionDropdowns()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            cbPhpVersions.Items.Clear();
            string phpBinRoot = Path.Combine(baseDir, @"bin\php");
            if (Directory.Exists(phpBinRoot))
            {
                string[] phpDirs = Directory.GetDirectories(phpBinRoot, "php*");
                foreach (string dir in phpDirs)
                {
                    cbPhpVersions.Items.Add(Path.GetFileName(dir));
                }
            }

            if (cbPhpVersions.Items.Count > 0)
            {
                string activeDirName = Path.GetFileName(Path.GetDirectoryName(pathPhpExe));
                int idx = cbPhpVersions.FindStringExact(activeDirName);
                cbPhpVersions.SelectedIndex = (idx >= 0) ? idx : 0;
            }
            else
            {
                cbPhpVersions.Items.Add("Chưa cài PHP");
                cbPhpVersions.SelectedIndex = 0;
            }

            cbMySqlVersions.Items.Clear();
            string mysqlBinRoot = Path.Combine(baseDir, @"bin\mysql");
            if (Directory.Exists(mysqlBinRoot))
            {
                string[] mysqlDirs = Directory.GetDirectories(mysqlBinRoot, "*");
                foreach (string dir in mysqlDirs)
                {
                    if (Path.GetFileName(dir).Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                    cbMySqlVersions.Items.Add(Path.GetFileName(dir));
                }
            }

            if (cbMySqlVersions.Items.Count > 0)
            {
                string activeDirName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(pathMySqlExe)));
                int idx = cbMySqlVersions.FindStringExact(activeDirName);
                cbMySqlVersions.SelectedIndex = (idx >= 0) ? idx : 0;
            }
            else
            {
                cbMySqlVersions.Items.Add("Chưa cài MySQL");
                cbMySqlVersions.SelectedIndex = 0;
            }

            cbWebVersions.Items.Clear();

            string nginxBinRoot = Path.Combine(baseDir, @"bin\nginx");
            if (Directory.Exists(nginxBinRoot))
            {
                string[] nginxDirs = Directory.GetDirectories(nginxBinRoot, "nginx*");
                if (nginxDirs.Length > 0)
                {
                    foreach (string dir in nginxDirs)
                    {
                        cbWebVersions.Items.Add("Nginx: " + Path.GetFileName(dir));
                    }
                }
                else if (File.Exists(Path.Combine(nginxBinRoot, "nginx.exe")))
                {
                    cbWebVersions.Items.Add("Nginx: Default");
                }
            }
            else if (File.Exists(Path.Combine(nginxBinRoot, "nginx.exe")))
            {
                cbWebVersions.Items.Add("Nginx: Default");
            }

            string apacheBinRoot = Path.Combine(baseDir, @"bin\apache");
            if (Directory.Exists(apacheBinRoot))
            {
                string[] apacheDirs = Directory.GetDirectories(apacheBinRoot, "*");
                foreach (string dir in apacheDirs)
                {
                    cbWebVersions.Items.Add("Apache: " + Path.GetFileName(dir));
                }
            }

            if (cbWebVersions.Items.Count > 0)
            {
                string activeText = "";
                if (selectedWebServerType == "Apache")
                {
                    string dir = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(pathApacheExe)));
                    activeText = "Apache: " + dir;
                }
                else
                {
                    string dir = Path.GetFileName(Path.GetDirectoryName(pathNginxExe));
                    activeText = dir.Contains("nginx") ? "Nginx: " + dir : "Nginx: Default";
                }

                int idx = cbWebVersions.FindStringExact(activeText);
                if (idx >= 0) cbWebVersions.SelectedIndex = idx;
                else cbWebVersions.SelectedIndex = 0;
            }
            else
            {
                cbWebVersions.Items.Add("Chưa cài Server");
                cbWebVersions.SelectedIndex = 0;
            }
        }

        private void PhpVersionChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            string selected = cbPhpVersions.SelectedItem.ToString();
            if (selected == "Chưa cài PHP") return;

            SaveActiveVersionSetting("ActivePHPVersion", selected);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPhpDir = Path.Combine(baseDir, @"bin\php", selected);
            pathPhpExe = Path.Combine(fullPhpDir, "php.exe");
            pathPhpCgiExe = Path.Combine(fullPhpDir, "php-cgi.exe");
            pathPhpConf = Path.Combine(fullPhpDir, "php.ini");

            UpdateSettingsFieldsText();

            // Configure the selected PHP version once on change version setting
            try
            {
                ConfigurePHP(@"bin\php\" + selected);
            }
            catch { }

            bool wasRunning = IsProcessRunning("php-cgi") || (procPHP != null && !procPHP.HasExited);
            if (wasRunning)
            {
                PhpStop_Click(null, null);
                System.Threading.Thread.Sleep(300);
                PhpStart_Click(null, null);
            }

            if (selectedWebServerType == "Apache")
            {
                UpdateApacheConfigFileForPhp(fullPhpDir);
                bool isApacheRunning = IsProcessRunning("httpd");
                if (isApacheRunning)
                {
                    WebStop_Click(null, null);
                    System.Threading.Thread.Sleep(300);
                    WebStart_Click(null, null);
                }
            }

            MessageBox.Show("Đã chuyển đổi thành công sang phiên bản PHP: " + selected, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateApacheConfigFileForPhp(string relativePhpDir)
        {
            if (File.Exists(pathApacheConf))
            {
                try
                {
                    string content = File.ReadAllText(pathApacheConf);
                    string fullPhpPath = Path.GetFullPath(relativePhpDir).Replace("\\", "/");
                    content = Regex.Replace(content, @"ScriptAlias\s+/php/\s+"".*""", "ScriptAlias /php/ \"" + fullPhpPath + "/\"", RegexOptions.IgnoreCase);
                    File.WriteAllText(pathApacheConf, content);
                }
                catch { }
            }
        }

        private void MySqlVersionChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            string selected = cbMySqlVersions.SelectedItem.ToString();
            if (selected == "Chưa cài MySQL") return;

            SaveActiveVersionSetting("ActiveMySQLVersion", selected);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullMysqlDir = Path.Combine(baseDir, @"bin\mysql", selected);
            pathMySqlExe = Path.Combine(fullMysqlDir, @"bin\mysqld.exe");
            pathMySqlConf = Path.Combine(fullMysqlDir, "my.ini");

            UpdateSettingsFieldsText();
            UpdatePortsDisplay();

            bool wasRunning = IsProcessRunning("mysqld") || (procMySQL != null && !procMySQL.HasExited);
            if (wasRunning)
            {
                MySqlStop_Click(null, null);
                System.Threading.Thread.Sleep(500);
                MySqlStart_Click(null, null);
            }

            MessageBox.Show("Đã chuyển đổi thành công sang phiên bản CSDL: " + selected, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void WebVersionChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            string selected = cbWebVersions.SelectedItem.ToString();
            if (selected == "Chưa cài Server") return;

            SaveActiveVersionSetting("ActiveWebServerType", selectedWebServerType);
            string verName = selected.Replace("Apache: ", "").Replace("Nginx: ", "");
            SaveActiveVersionSetting("ActiveWebServerVersion", verName);

            bool wasRunning = IsProcessRunning(selectedWebServerType == "Apache" ? "httpd" : "nginx") || (procWebServer != null && !procWebServer.HasExited);
            if (wasRunning)
            {
                WebStop_Click(null, null);
                System.Threading.Thread.Sleep(300);
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (selected.StartsWith("Nginx"))
            {
                selectedWebServerType = "Nginx";
                string dirName = selected.Replace("Nginx: ", "");
                if (dirName == "Default")
                {
                    pathNginxExe = Path.Combine(baseDir, @"bin\nginx\nginx.exe");
                    pathNginxConf = Path.Combine(baseDir, @"bin\nginx\conf\nginx.conf");
                }
                else
                {
                    pathNginxExe = Path.Combine(baseDir, @"bin\nginx", dirName, "nginx.exe");
                    pathNginxConf = Path.Combine(baseDir, @"bin\nginx", dirName, @"conf\nginx.conf");
                }
            }
            else
            {
                selectedWebServerType = "Apache";
                string dirName = selected.Replace("Apache: ", "");
                pathApacheExe = Path.Combine(baseDir, @"bin\apache", dirName, @"bin\httpd.exe");
                pathApacheConf = Path.Combine(baseDir, @"bin\apache", dirName, @"conf\httpd.conf");

                string currentPhpDir = cbPhpVersions.SelectedItem.ToString();
                if (currentPhpDir != "Chưa cài PHP")
                {
                    UpdateApacheConfigFileForPhp(Path.Combine(baseDir, @"bin\php", currentPhpDir));
                }
            }

            lblWebTitle.Text = selectedWebServerType.ToUpper() + " SERVER";
            UpdateSettingsFieldsText();
            UpdatePortsDisplay();

            if (wasRunning)
            {
                WebStart_Click(null, null);
            }

            MessageBox.Show("Đã chuyển đổi thành công sang: " + selected, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AutoDetectBinaries()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // php paths detection
            string phpBinRoot = Path.Combine(baseDir, @"bin\php");
            if (Directory.Exists(phpBinRoot))
            {
                string[] phpDirs = Directory.GetDirectories(phpBinRoot, "php*");
                if (phpDirs.Length > 0)
                {
                    Array.Sort(phpDirs);
                    string bestPhpDir = phpDirs[phpDirs.Length - 1];
                    pathPhpExe = Path.Combine(bestPhpDir, "php.exe");
                    pathPhpCgiExe = Path.Combine(bestPhpDir, "php-cgi.exe");
                    pathPhpConf = Path.Combine(bestPhpDir, "php.ini");
                }
            }
            
            if (!File.Exists(pathPhpExe))
            {
                pathPhpExe = Path.Combine(baseDir, @"bin\php\php.exe");
                pathPhpCgiExe = Path.Combine(baseDir, @"bin\php\php-cgi.exe");
                pathPhpConf = Path.Combine(baseDir, @"bin\php\php.ini");
            }

            // Apache paths detection
            string apacheBinRoot = Path.Combine(baseDir, @"bin\apache");
            if (Directory.Exists(apacheBinRoot))
            {
                string[] apacheDirs = Directory.GetDirectories(apacheBinRoot, "httpd*");
                if (apacheDirs.Length == 0) apacheDirs = Directory.GetDirectories(apacheBinRoot, "apache*");
                if (apacheDirs.Length > 0)
                {
                    Array.Sort(apacheDirs);
                    string bestApacheDir = apacheDirs[apacheDirs.Length - 1];
                    pathApacheExe = Path.Combine(bestApacheDir, @"bin\httpd.exe");
                    pathApacheConf = Path.Combine(bestApacheDir, @"conf\httpd.conf");
                }
            }

            if (!File.Exists(pathApacheExe))
            {
                pathApacheExe = Path.Combine(baseDir, @"bin\apache\bin\httpd.exe");
                pathApacheConf = Path.Combine(baseDir, @"bin\apache\conf\httpd.conf");
            }

            // Nginx paths detection
            string nginxBinRoot = Path.Combine(baseDir, @"bin\nginx");
            if (Directory.Exists(nginxBinRoot))
            {
                string[] nginxDirs = Directory.GetDirectories(nginxBinRoot, "nginx*");
                if (nginxDirs.Length > 0)
                {
                    Array.Sort(nginxDirs);
                    string bestNginxDir = nginxDirs[nginxDirs.Length - 1];
                    pathNginxExe = Path.Combine(bestNginxDir, "nginx.exe");
                    pathNginxConf = Path.Combine(bestNginxDir, @"conf\nginx.conf");
                }
            }

            if (!File.Exists(pathNginxExe))
            {
                pathNginxExe = Path.Combine(baseDir, @"bin\nginx\nginx.exe");
                pathNginxConf = Path.Combine(baseDir, @"bin\nginx\conf\nginx.conf");
            }

            // MySQL paths detection
            string mysqlBinRoot = Path.Combine(baseDir, @"bin\mysql");
            if (Directory.Exists(mysqlBinRoot))
            {
                string[] mysqlDirs = Directory.GetDirectories(mysqlBinRoot, "*");
                List<string> validMysqlDirs = new List<string>();
                foreach (string d in mysqlDirs)
                {
                    if (Path.GetFileName(d).Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                    validMysqlDirs.Add(d);
                }

                if (validMysqlDirs.Count > 0)
                {
                    validMysqlDirs.Sort();
                    string bestMySqlDir = validMysqlDirs[validMysqlDirs.Count - 1];
                    pathMySqlExe = Path.Combine(bestMySqlDir, @"bin\mysqld.exe");
                    pathMySqlConf = Path.Combine(bestMySqlDir, "my.ini");
                }
            }

            if (!File.Exists(pathMySqlExe))
            {
                pathMySqlExe = Path.Combine(baseDir, @"bin\mysql\bin\mysqld.exe");
                pathMySqlConf = Path.Combine(baseDir, @"bin\mysql\my.ini");
            }

            UpdateSettingsFieldsText();
        }

        private void UpdateSettingsFieldsText()
        {
            if (txtSetApacheExe != null) txtSetApacheExe.Text = pathApacheExe;
            if (txtSetApacheConf != null) txtSetApacheConf.Text = pathApacheConf;
            if (txtSetNginxExe != null) txtSetNginxExe.Text = pathNginxExe;
            if (txtSetNginxConf != null) txtSetNginxConf.Text = pathNginxConf;
            if (txtSetMySqlExe != null) txtSetMySqlExe.Text = pathMySqlExe;
            if (txtSetMySqlConf != null) txtSetMySqlConf.Text = pathMySqlConf;
            if (txtSetPhpExe != null) txtSetPhpExe.Text = pathPhpExe;
            if (txtSetPhpCgiExe != null) txtSetPhpCgiExe.Text = pathPhpCgiExe;
            if (txtSetPhpConf != null) txtSetPhpConf.Text = pathPhpConf;
        }

        private void SaveSettings_Click(object sender, EventArgs e)
        {
            if (cbWebServerType != null && cbWebServerType.SelectedItem != null)
            {
                selectedWebServerType = cbWebServerType.SelectedItem.ToString();
            }
            
            if (lblWebTitle != null)
            {
                lblWebTitle.Text = selectedWebServerType.ToUpper() + " SERVER";
            }
            
            isInitializing = true;
            AutoDetectBinaries();
            ReloadAllVersionDropdowns();
            isInitializing = false;

            UpdatePortsDisplay();

            // Lưu cấu hình chạy ẩn và khởi động cùng Windows
            if (chkAutoStart != null)
            {
                SetAutoStart(chkAutoStart.Checked);
            }
            if (chkMinimizeToTray != null)
            {
                SaveMinimizeToTraySetting(chkMinimizeToTray.Checked);
            }
            if (chkAutoOptimizePhpIni != null)
            {
                SaveAutoOptimizePhpIniSetting(chkAutoOptimizePhpIni.Checked);
            }
            if (chkAdminVHostMode != null)
            {
                var c = DeployDemoForm.LoadGlobalConfig();
                c["vhost_mode"] = chkAdminVHostMode.Checked ? "admin" : "normal";
                DeployDemoForm.SaveGlobalConfig(c);
            }

            if (pnlSettingsOverlay != null)
            {
                pnlSettingsOverlay.Visible = false;
            }
            MessageBox.Show("Cấu hình hệ thống đã được cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TmrStatus_Tick(object sender, EventArgs e)
        {
            bool isWebRunning = IsProcessRunning(selectedWebServerType == "Apache" ? "httpd" : "nginx") || (procWebServer != null && !procWebServer.HasExited);
            lblWebStatusDot.BackColor = isWebRunning ? colorGreen : colorRed;
            btnWebStart.Visible = !isWebRunning;
            btnWebStop.Visible = isWebRunning;
            btnWebStart.Enabled = !isWebRunning;
            btnWebStop.Enabled = isWebRunning;

            string webPort = txtWebPort != null ? txtWebPort.Text.Trim() : "80";
            lblWebStatusText.Text = isWebRunning ? "Đang chạy • Port: " + webPort : "Đang dừng";

            bool isMySqlRunning = IsProcessRunning("mysqld") || (procMySQL != null && !procMySQL.HasExited);
            lblMySqlStatusDot.BackColor = isMySqlRunning ? colorGreen : colorRed;
            btnMySqlStart.Visible = !isMySqlRunning;
            btnMySqlStop.Visible = isMySqlRunning;
            btnMySqlStart.Enabled = !isMySqlRunning;
            btnMySqlStop.Enabled = isMySqlRunning;

            string mysqlPort = txtMySqlPort != null ? txtMySqlPort.Text.Trim() : "3306";
            lblMySqlStatusText.Text = isMySqlRunning ? "Đang chạy • Port: " + mysqlPort : "Đang dừng";

            bool isPhpRunning = IsProcessRunning("php-cgi") || (procPHP != null && !procPHP.HasExited);
            lblPhpStatusDot.BackColor = isPhpRunning ? colorGreen : colorRed;
            btnPhpStart.Visible = !isPhpRunning;
            btnPhpStop.Visible = isPhpRunning;
            btnPhpStart.Enabled = !isPhpRunning;
            btnPhpStop.Enabled = isPhpRunning;

            lblPhpStatusText.Text = isPhpRunning ? "Đang chạy • Port: 9000" : "Đang dừng";

            if (pnlTabDashboard.Visible)
            {
                UpdateDetailsPane();
            }
        }

        private bool IsProcessRunning(string procName)
        {
            Process[] processes = Process.GetProcessesByName(procName);
            return processes.Length > 0;
        }

        private void UpdatePortsDisplay()
        {
            if (selectedWebServerType == "Apache")
            {
                string port = ParsePortFromFile(pathApacheConf, @"Listen\s+(\d+)");
                if (lblWebPort != null) lblWebPort.Text = "Port hiện tại: " + (string.IsNullOrEmpty(port) ? "80" : port);
                if (txtWebPort != null) txtWebPort.Text = string.IsNullOrEmpty(port) ? "80" : port;
            }
            else
            {
                string port = ParsePortFromFile(pathNginxConf, @"listen\s+(\d+);");
                if (lblWebPort != null) lblWebPort.Text = "Port hiện tại: " + (string.IsNullOrEmpty(port) ? "80" : port);
                if (txtWebPort != null) txtWebPort.Text = string.IsNullOrEmpty(port) ? "80" : port;
            }

            string mysqlPort = ParsePortFromFile(pathMySqlConf, @"port\s*=\s*(\d+)");
            if (lblMySqlPort != null) lblMySqlPort.Text = "Port hiện tại: " + (string.IsNullOrEmpty(mysqlPort) ? "3306" : mysqlPort);
            if (txtMySqlPort != null) txtMySqlPort.Text = string.IsNullOrEmpty(mysqlPort) ? "3306" : mysqlPort;

            if (lblPhpPort != null) lblPhpPort.Text = "Port PHP-CGI: 9000";
            if (txtPhpPort != null) txtPhpPort.Text = "9000";

            UpdateDetailsPane();
        }

        private string ParsePortFromFile(string filePath, string regexPattern)
        {
            if (string.IsNullOrEmpty(filePath)) return "";
            try
            {
                if (File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);
                    Match m = Regex.Match(content, regexPattern, RegexOptions.IgnoreCase);
                    if (m.Success && m.Groups.Count > 1)
                    {
                        return m.Groups[1].Value;
                    }
                }
            }
            catch { }
            return "";
        }

        private void SavePortToFile(string filePath, string regexPattern, string newPort, string formatReplacement)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File cấu hình không tồn tại để đổi port:\r\n" + filePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                if (Regex.IsMatch(content, regexPattern, RegexOptions.IgnoreCase))
                {
                    string replaced = Regex.Replace(content, regexPattern, string.Format(formatReplacement, newPort), RegexOptions.IgnoreCase);
                    File.WriteAllText(filePath, replaced);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy dòng cấu hình port tiêu chuẩn trong file config. Hãy chỉnh sửa thủ công bằng nút Cấu Hình.", "Chú ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ghi đè port: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebStart_Click(object sender, EventArgs e)
        {
            string exe = (selectedWebServerType == "Apache") ? pathApacheExe : pathNginxExe;
            if (!File.Exists(exe))
            {
                MessageBox.Show("Không tìm thấy file thực thi của Web Server:\r\n" + exe, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Tự động làm mới cấu hình với đường dẫn hiện tại của ứng dụng và port trên giao diện
            try
            {
                string currentPort = txtWebPort != null ? txtWebPort.Text.Trim() : "80";
                string mysqlPort = txtMySqlPort != null ? txtMySqlPort.Text.Trim() : "3306";
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (selectedWebServerType == "Apache")
                {
                    string fullApacheDir = Path.GetDirectoryName(Path.GetDirectoryName(pathApacheExe));
                    string relativeApacheDir = fullApacheDir;
                    if (relativeApacheDir.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        relativeApacheDir = relativeApacheDir.Substring(baseDir.Length).TrimStart('\\', '/');
                    }
                    DownloadCenterForm.ConfigureApache(relativeApacheDir, currentPort, Path.GetDirectoryName(pathPhpExe));
                }
                else
                {
                    string fullNginxDir = Path.GetDirectoryName(pathNginxExe);
                    string relativeNginxDir = fullNginxDir;
                    if (relativeNginxDir.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        relativeNginxDir = relativeNginxDir.Substring(baseDir.Length).TrimStart('\\', '/');
                    }
                    DownloadCenterForm.ConfigureNginx(relativeNginxDir, currentPort, Path.GetDirectoryName(pathPhpExe));
                }

                // Tự động cập nhật config.inc.php của phpMyAdmin theo Port của MySQL đang hiển thị trên giao diện
                DownloadCenterForm.ConfigurePhpMyAdmin("phpmyadmin", mysqlPort);
            }
            catch { }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exe;
                psi.WorkingDirectory = Path.GetDirectoryName(exe);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                procWebServer = Process.Start(psi);
                
                // Wait briefly to check if it crashed immediately (e.g. VC++ missing, port occupied)
                if (procWebServer != null)
                {
                    procWebServer.WaitForExit(1500);
                    if (procWebServer.HasExited && procWebServer.ExitCode != 0)
                    {
                        MessageBox.Show("Web Server đã dừng đột ngột ngay sau khi khởi động (Exit Code: " + procWebServer.ExitCode + ").\r\n\r\n" +
                                        "LƯU Ý: Đây thường là do thiếu thư viện nền Microsoft Visual C++ Runtime (x64) trên máy tính của bạn.\r\n" +
                                        "Vui lòng nhấn nút 'KHO TẢI PHẦN MỀM' và tải cài đặt 'Microsoft Visual C++ Runtime' để khắc phục!", 
                                        "Cảnh báo khởi động", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                
                TmrStatus_Tick(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động Web Server: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebStop_Click(object sender, EventArgs e)
        {
            string procName = (selectedWebServerType == "Apache") ? "httpd" : "nginx";
            KillProcessesByName(procName);
            if (procWebServer != null && !procWebServer.HasExited)
            {
                try { procWebServer.Kill(); } catch { }
            }
            procWebServer = null;
            TmrStatus_Tick(null, null);
        }

        private void WebConfig_Click(object sender, EventArgs e)
        {
            string file = (selectedWebServerType == "Apache") ? pathApacheConf : pathNginxConf;
            try
            {
                if (File.Exists(file))
                {
                    Process.Start("notepad.exe", file);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy file cấu hình web server!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở file cấu hình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebChangePort_Click(object sender, EventArgs e)
        {
            string newPort = txtWebPort.Text.Trim();
            int p;
            if (!int.TryParse(newPort, out p) || p <= 0 || p > 65535)
            {
                MessageBox.Show("Port phải là một số nguyên từ 1 đến 65535!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool wasRunning = IsProcessRunning(selectedWebServerType == "Apache" ? "httpd" : "nginx");
            if (wasRunning)
            {
                WebStop_Click(null, null);
                System.Threading.Thread.Sleep(500);
            }

            if (selectedWebServerType == "Apache")
            {
                SavePortToFile(pathApacheConf, @"Listen\s+\d+", newPort, "Listen {0}");
            }
            else
            {
                SavePortToFile(pathNginxConf, @"listen\s+\d+;", newPort, "listen {0};");
            }

            UpdatePortsDisplay();

            if (wasRunning)
            {
                WebStart_Click(null, null);
            }
            MessageBox.Show("Đã đổi cổng Web Server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MySqlStart_Click(object sender, EventArgs e)
        {
            if (!File.Exists(pathMySqlExe))
            {
                MessageBox.Show("Không tìm thấy file thực thi MySQL:\r\n" + pathMySqlExe, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Tự động làm mới cấu hình với đường dẫn hiện tại và port trên giao diện
            try
            {
                string mysqlPort = txtMySqlPort != null ? txtMySqlPort.Text.Trim() : "3306";
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullMysqlDir = Path.GetDirectoryName(Path.GetDirectoryName(pathMySqlExe));
                string relativeMysqlDir = fullMysqlDir;
                if (relativeMysqlDir.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    relativeMysqlDir = relativeMysqlDir.Substring(baseDir.Length).TrimStart('\\', '/');
                }
                DownloadCenterForm.ConfigureMySQL(relativeMysqlDir, mysqlPort);
            }
            catch { }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = pathMySqlExe;
                psi.WorkingDirectory = Path.GetDirectoryName(pathMySqlExe);
                psi.Arguments = "--defaults-file=\"" + Path.GetFullPath(pathMySqlConf) + "\" --standalone";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                procMySQL = Process.Start(psi);
                
                // Wait briefly to check if it crashed immediately (e.g. port conflict or missing tables)
                if (procMySQL != null)
                {
                    procMySQL.WaitForExit(1500);
                    if (procMySQL.HasExited && procMySQL.ExitCode != 0)
                    {
                        MessageBox.Show("MySQL CSDL đã dừng đột ngột ngay sau khi khởi động (Exit Code: " + procMySQL.ExitCode + ").\r\n\r\n" +
                                        "LƯU Ý: Vui lòng kiểm tra xem cổng Port (mặc định: 3306) có bị trùng với ứng dụng khác (Laragon, XAMPP, MySQL Server) đang chạy hay không.\r\n" +
                                        "Ngoài ra, hãy đảm bảo thư viện nền VC++ Runtime đã được cài đặt.", 
                                        "Cảnh báo khởi động", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                TmrStatus_Tick(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động MySQL: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MySqlStop_Click(object sender, EventArgs e)
        {
            KillProcessesByName("mysqld");
            if (procMySQL != null && !procMySQL.HasExited)
            {
                try { procMySQL.Kill(); } catch { }
            }
            procMySQL = null;
            TmrStatus_Tick(null, null);
        }

        private void MySqlConfig_Click(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(pathMySqlConf))
                {
                    Process.Start("notepad.exe", pathMySqlConf);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy file cấu hình my.ini!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở file cấu hình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MySqlChangePort_Click(object sender, EventArgs e)
        {
            string newPort = txtMySqlPort.Text.Trim();
            int p;
            if (!int.TryParse(newPort, out p) || p <= 0 || p > 65535)
            {
                MessageBox.Show("Port phải là một số nguyên từ 1 đến 65535!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool wasRunning = IsProcessRunning("mysqld");
            if (wasRunning)
            {
                MySqlStop_Click(null, null);
                System.Threading.Thread.Sleep(500);
            }

            SavePortToFile(pathMySqlConf, @"port\s*=\s*\d+", newPort, "port = {0}");
            UpdatePortsDisplay();

            if (wasRunning)
            {
                MySqlStart_Click(null, null);
            }
            MessageBox.Show("Đã đổi cổng MySQL thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConfigurePHPIni(string phpConfPath)
        {
            if (!File.Exists(phpConfPath)) return;

            try
            {
                string content = File.ReadAllText(phpConfPath);
                bool modified = false;

                // Fix upload_tmp_dir & sys_temp_dir → avoid system temp directory dependency (Portability)
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string tempPathFwd = tempDir.Replace("\\", "/");

                if (LoadAutoOptimizePhpIniSetting())
                {
                    // ── FULL OPTIMIZATION MODE ──
                    
                    // Enable extensions on run
                    content = Regex.Replace(content, @";\s*extension_dir\s*=\s*""ext""", "extension_dir = \"ext\"", RegexOptions.IgnoreCase);
                    string[] exts = { "curl", "fileinfo", "gd", "mbstring", "mysqli", "openssl", "pdo_mysql", "ftp", "zip" };
                    foreach (var ext in exts)
                    {
                        string targetExt = ext;
                        if (ext == "gd")
                        {
                            string phpDir = Path.GetDirectoryName(phpConfPath);
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

                        string pattern = @"(?m)^\s*extension\s*=\s*(?:php_)?" + targetExt + @"(?:\.dll)?\s*$";
                        bool alreadyEnabled = Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase);
                        if (!alreadyEnabled)
                        {
                            string uncommentPattern = @"(?m)^;+\s*extension\s*=\s*((?:php_)?" + targetExt + @"(?:\.dll)?)\s*$";
                            string uncommented = Regex.Replace(content, uncommentPattern, "extension = $1", RegexOptions.IgnoreCase);
                            if (uncommented != content)
                            {
                                content = uncommented;
                            }
                            else
                            {
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

                    // Suppress display_errors to prevent Bad header / 500 errors in CGI mode
                    if (!Regex.IsMatch(content, @"(?m)^\s*display_errors\s*=\s*Off", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                    {
                        content = Regex.Replace(content, @"(?m)^\s*display_errors\s*=.*$", "display_errors = Off", RegexOptions.IgnoreCase);
                    }
                    if (!Regex.IsMatch(content, @"(?m)^\s*log_errors\s*=\s*On", RegexOptions.IgnoreCase | RegexOptions.Multiline))
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
                        content = Regex.Replace(content, @"(?m)^;?\s*max_execution_time\s*=.*$", "max_execution_time = 3600", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        content += "\r\nmax_execution_time = 3600\r\n";
                    }

                    if (Regex.IsMatch(content, @"max_input_time\s*=", RegexOptions.IgnoreCase))
                    {
                        content = Regex.Replace(content, @"(?m)^;?\s*max_input_time\s*=.*$", "max_input_time = 3600", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        content += "\r\nmax_input_time = 3600\r\n";
                    }

                    if (Regex.IsMatch(content, @"session\.gc_maxlifetime\s*=", RegexOptions.IgnoreCase))
                    {
                        content = Regex.Replace(content, @"(?m)^;?\s*session\.gc_maxlifetime\s*=.*$", "session.gc_maxlifetime = 2592000", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        content += "\r\nsession.gc_maxlifetime = 2592000\r\n";
                    }

                    // Always overwrite/optimize in full mode
                    content = Regex.Replace(content, @"(?m)^;?\s*upload_tmp_dir\s*=.*$", "upload_tmp_dir = \"" + tempPathFwd + "\"", RegexOptions.IgnoreCase);
                    content = Regex.Replace(content, @"(?m)^;?\s*sys_temp_dir\s*=.*$", "sys_temp_dir = \"" + tempPathFwd + "\"", RegexOptions.IgnoreCase);
                    
                    string sessionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "sessions");
                    if (!Directory.Exists(sessionDir)) Directory.CreateDirectory(sessionDir);
                    string sessionPathFwd = sessionDir.Replace("\\", "/");
                    bool replaced = false;
                    content = Regex.Replace(content, @"(?m)^;?\s*session\.save_path\s*=.*$", m => {
                        if (!replaced) { replaced = true; return "session.save_path = \"" + sessionPathFwd + "\""; }
                        return "";
                    }, RegexOptions.IgnoreCase);

                    modified = true;
                }
                else
                {
                    // ── FAST PORTABLE MODE ──
                    
                    if (Regex.IsMatch(content, @"upload_tmp_dir\s*=", RegexOptions.IgnoreCase))
                    {
                        string oldTmp = Regex.Match(content, @"(?m)^;?\s*upload_tmp_dir\s*=\s*(.*)$", RegexOptions.IgnoreCase).Groups[1].Value.Trim(' ', '"', '\r');
                        if (oldTmp != tempPathFwd)
                        {
                            content = Regex.Replace(content, @"(?m)^;?\s*upload_tmp_dir\s*=.*$", "upload_tmp_dir = \"" + tempPathFwd + "\"", RegexOptions.IgnoreCase);
                            modified = true;
                        }
                    }
                    else
                    {
                        content += "\r\nupload_tmp_dir = \"" + tempPathFwd + "\"\r\n";
                        modified = true;
                    }

                    if (Regex.IsMatch(content, @"sys_temp_dir\s*=", RegexOptions.IgnoreCase))
                    {
                        string oldSys = Regex.Match(content, @"(?m)^;?\s*sys_temp_dir\s*=\s*(.*)$", RegexOptions.IgnoreCase).Groups[1].Value.Trim(' ', '"', '\r');
                        if (oldSys != tempPathFwd)
                        {
                            content = Regex.Replace(content, @"(?m)^;?\s*sys_temp_dir\s*=.*$", "sys_temp_dir = \"" + tempPathFwd + "\"", RegexOptions.IgnoreCase);
                            modified = true;
                        }
                    }
                    else
                    {
                        content += "\r\nsys_temp_dir = \"" + tempPathFwd + "\"\r\n";
                        modified = true;
                    }

                    string sessionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "sessions");
                    if (!Directory.Exists(sessionDir)) Directory.CreateDirectory(sessionDir);
                    string sessionPathFwd = sessionDir.Replace("\\", "/");
                    
                    if (Regex.IsMatch(content, @"session\.save_path\s*=", RegexOptions.IgnoreCase))
                    {
                        string oldSess = Regex.Match(content, @"(?m)^;?\s*session\.save_path\s*=\s*(.*)$", RegexOptions.IgnoreCase).Groups[1].Value.Trim(' ', '"', '\r');
                        if (oldSess != sessionPathFwd)
                        {
                            bool replaced = false;
                            content = Regex.Replace(content, @"(?m)^;?\s*session\.save_path\s*=.*$", m => {
                                if (!replaced) { replaced = true; return "session.save_path = \"" + sessionPathFwd + "\""; }
                                return "";
                            }, RegexOptions.IgnoreCase);
                            modified = true;
                        }
                    }
                    else
                    {
                        content += "\r\nsession.save_path = \"" + sessionPathFwd + "\"\r\n";
                        modified = true;
                    }
                }

                // Enforce SMTP redirection to Mail Sandbox
                bool smtpChanged = false;
                if (Regex.IsMatch(content, @"SMTP\s*=", RegexOptions.IgnoreCase))
                {
                    string oldSMTP = Regex.Match(content, @"(?m)^;?\s*SMTP\s*=\s*(.*)$", RegexOptions.IgnoreCase).Groups[1].Value.Trim(' ', '"', '\r');
                    if (oldSMTP != "127.0.0.1")
                    {
                        content = Regex.Replace(content, @"(?m)^;?\s*SMTP\s*=.*$", "SMTP = 127.0.0.1", RegexOptions.IgnoreCase);
                        smtpChanged = true;
                    }
                }
                else
                {
                    content += "\r\nSMTP = 127.0.0.1\r\n";
                    smtpChanged = true;
                }

                if (Regex.IsMatch(content, @"smtp_port\s*=", RegexOptions.IgnoreCase))
                {
                    string oldPort = Regex.Match(content, @"(?m)^;?\s*smtp_port\s*=\s*(.*)$", RegexOptions.IgnoreCase).Groups[1].Value.Trim(' ', '"', '\r');
                    if (oldPort != "1025")
                    {
                        content = Regex.Replace(content, @"(?m)^;?\s*smtp_port\s*=.*$", "smtp_port = 1025", RegexOptions.IgnoreCase);
                        smtpChanged = true;
                    }
                }
                else
                {
                    content += "\r\nsmtp_port = 1025\r\n";
                    smtpChanged = true;
                }

                if (modified || smtpChanged)
                {
                    File.WriteAllText(phpConfPath, content);
                }
            }
            catch { }
        }

        private void PhpStart_Click(object sender, EventArgs e)
        {
            if (!File.Exists(pathPhpCgiExe))
            {
                MessageBox.Show("Không tìm thấy file thực thi php-cgi.exe:\r\n" + pathPhpCgiExe, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Tắt bất kỳ php-cgi nào đang chạy trước để dọn dẹp cổng
            PhpStop_Click(null, null);

            // Tự động làm mới session save path và tối ưu hóa php.ini cho CGI trước khi chạy
            ConfigurePHPIni(pathPhpConf);

            try
            {
                // 1. Khởi động PHP mặc định (Port 9000 hoặc port trên giao diện)
                string port = txtPhpPort != null ? txtPhpPort.Text.Trim() : "9000";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = pathPhpCgiExe;
                psi.WorkingDirectory = Path.GetDirectoryName(pathPhpCgiExe);
                psi.Arguments = "-b 127.0.0.1:" + port + " -c \"" + Path.GetFullPath(pathPhpConf) + "\"";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                procPHP = Process.Start(psi);
                if (procPHP != null) procPHPList.Add(procPHP);

                // 2. Đọc các dự án cần PHP phiên bản khác và khởi động tương ứng
                Dictionary<string, string> sitesConfig = LoadSitesConfig();
                HashSet<string> startedVersions = new HashSet<string>();
                
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string activePhpDirName = Path.GetFileName(Path.GetDirectoryName(pathPhpExe));
                
                foreach (var kvp in sitesConfig)
                {
                    string phpVerDir = kvp.Value;
                    if (phpVerDir.Equals(activePhpDirName, StringComparison.OrdinalIgnoreCase)) continue; // Đã chạy ở port 9000
                    if (startedVersions.Contains(phpVerDir)) continue; // Đã khởi động phiên bản này trên port riêng rồi
                    
                    startedVersions.Add(phpVerDir);

                    string customPhpExe = Path.Combine(baseDir, @"bin\php", phpVerDir, "php-cgi.exe");
                    string customPhpConf = Path.Combine(baseDir, @"bin\php", phpVerDir, "php.ini");

                    if (File.Exists(customPhpExe))
                    {
                        ConfigurePHPIni(customPhpConf);
                        int customPort = GetPhpPortForVersion(phpVerDir);

                        ProcessStartInfo psiCustom = new ProcessStartInfo();
                        psiCustom.FileName = customPhpExe;
                        psiCustom.WorkingDirectory = Path.GetDirectoryName(customPhpExe);
                        psiCustom.Arguments = "-b 127.0.0.1:" + customPort + " -c \"" + Path.GetFullPath(customPhpConf) + "\"";
                        psiCustom.CreateNoWindow = true;
                        psiCustom.UseShellExecute = false;
                        psiCustom.WindowStyle = ProcessWindowStyle.Hidden;

                        Process customProc = Process.Start(psiCustom);
                        if (customProc != null)
                        {
                            procPHPList.Add(customProc);
                        }
                    }
                }

                TmrStatus_Tick(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động PHP-CGI: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PhpStop_Click(object sender, EventArgs e)
        {
            KillProcessesByName("php-cgi");
            foreach (var p in procPHPList)
            {
                try { if (p != null && !p.HasExited) p.Kill(); } catch { }
            }
            procPHPList.Clear();
            procPHP = null;
            TmrStatus_Tick(null, null);
        }

        private void PhpConfig_Click(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(pathPhpConf))
                {
                    Process.Start("notepad.exe", pathPhpConf);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy file cấu hình php.ini!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở file cấu hình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PhpChangePort_Click(object sender, EventArgs e)
        {
            string newPort = txtPhpPort.Text.Trim();
            int p;
            if (!int.TryParse(newPort, out p) || p <= 0 || p > 65535)
            {
                MessageBox.Show("Port phải là một số nguyên từ 1 đến 65535!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool wasRunning = IsProcessRunning("php-cgi");
            if (wasRunning)
            {
                PhpStop_Click(null, null);
                System.Threading.Thread.Sleep(500);
                PhpStart_Click(null, null);
            }
            else
            {
                if (lblPhpPort != null) lblPhpPort.Text = "Port PHP-CGI: " + newPort;
            }
            MessageBox.Show("Đã đổi cổng PHP-CGI thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StartAll_Click(object sender, EventArgs e)
        {
            lblHeaderTitle.Text = "RBW STACK CORE MANAGER (STARTING ALL...)";
            
            if (!IsProcessRunning("php-cgi") && File.Exists(pathPhpCgiExe))
            {
                PhpStart_Click(null, null);
                System.Threading.Thread.Sleep(200);
            }

            if (!IsProcessRunning("mysqld") && File.Exists(pathMySqlExe))
            {
                MySqlStart_Click(null, null);
                System.Threading.Thread.Sleep(200);
            }

            string webProc = (selectedWebServerType == "Apache") ? "httpd" : "nginx";
            if (!IsProcessRunning(webProc))
            {
                WebStart_Click(null, null);
            }

            StartSmtpServer();

            lblHeaderTitle.Text = "RBW STACK CORE MANAGER";
            TmrStatus_Tick(null, null);
        }

        private void StopAll_Click(object sender, EventArgs e)
        {
            lblHeaderTitle.Text = "RBW STACK CORE MANAGER (STOPPING ALL...)";
            
            StopAllTunnels();
            WebStop_Click(null, null);
            MySqlStop_Click(null, null);
            PhpStop_Click(null, null);

            StopSmtpServer();

            lblHeaderTitle.Text = "RBW STACK CORE MANAGER";
            TmrStatus_Tick(null, null);
        }

        private void StopAllTunnels()
        {
            try
            {
                foreach (var kvp in activeTunnels)
                {
                    Process p = kvp.Value;
                    if (p != null && !p.HasExited)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                activeTunnels.Clear();

                // Set all active states in config back to inactive "0"
                Dictionary<string, string> config = LoadTunnelsConfig();
                Dictionary<string, string> updatedConfig = new Dictionary<string, string>();
                foreach (var kvp in config)
                {
                    string[] parts = kvp.Value.Split('|');
                    if (parts.Length >= 3)
                    {
                        updatedConfig[kvp.Key] = string.Format("{0}|{1}|0", parts[0], parts[1]);
                    }
                }
                SaveTunnelsConfig(updatedConfig);
            }
            catch { }

            KillOurNodeProcessesOnly();
            KillOurCloudflareProcessesOnly();
        }

        private void KillOurCloudflareProcessesOnly()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLower();
                Process[] processes = Process.GetProcessesByName("cloudflared");
                foreach (Process p in processes)
                {
                    try
                    {
                        string path = p.MainModule.FileName.ToLower();
                        if (path.StartsWith(baseDir))
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void KillOurNodeProcessesOnly()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLower();
                Process[] processes = Process.GetProcessesByName("node");
                foreach (Process p in processes)
                {
                    try
                    {
                        string path = p.MainModule.FileName.ToLower();
                        if (path.StartsWith(baseDir))
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void StartTunnelProcess(string sitePath)
        {
            try
            {
                if (activeTunnels.ContainsKey(sitePath))
                {
                    try
                    {
                        Process oldP = activeTunnels[sitePath];
                        if (oldP != null && !oldP.HasExited) oldP.Kill();
                    }
                    catch { }
                    activeTunnels.Remove(sitePath);
                }

                string cfExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\cloudflared\cloudflared.exe");
                if (!File.Exists(cfExe))
                {
                    var dr = MessageBox.Show(
                        "Bạn chưa cài đặt Cloudflare Tunnel (cloudflared).\r\nBạn có muốn mở Bộ cài đặt để tải về tự động ngay lập tức không?",
                        "Chưa cài đặt Cloudflared",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );
                    if (dr == DialogResult.Yes)
                    {
                        this.BeginInvoke((MethodInvoker)delegate {
                            FilterDownloadCards("Cloudflare Tunnel");
                        });
                    }
                    return;
                }

                string webPort = GetWebPort();

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = cfExe;
                psi.Arguments = string.Format("tunnel --protocol http2 --url http://localhost:{0}", webPort);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;

                Process p = new Process();
                p.StartInfo = psi;
                p.EnableRaisingEvents = true;

                p.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        if (e.Data.Contains(".trycloudflare.com"))
                        {
                            var match = Regex.Match(e.Data, @"https://[a-zA-Z0-9\-]+\.trycloudflare\.com");
                            if (match.Success)
                            {
                                string url = match.Value;
                                string subdomain = url.Replace("https://", "").Replace(".trycloudflare.com", "");

                                // Update tunnels config
                                Dictionary<string, string> currTunnels = LoadTunnelsConfig();
                                currTunnels[sitePath] = string.Format("{0}|https|1", subdomain);
                                SaveTunnelsConfig(currTunnels);

                                // Refresh web services and UI on main thread
                                this.BeginInvoke((MethodInvoker)delegate {
                                    RestartWebServicesAndPhp();
                                    RenderSitesList();
                                });
                            }
                        }
                    }
                };

                p.Start();
                p.BeginErrorReadLine();
                activeTunnels[sitePath] = p;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi chạy Cloudflare Tunnel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopTunnelProcess(string sitePath)
        {
            try
            {
                if (activeTunnels.ContainsKey(sitePath))
                {
                    Process p = activeTunnels[sitePath];
                    if (p != null && !p.HasExited)
                    {
                        p.Kill();
                    }
                    activeTunnels.Remove(sitePath);
                }
            }
            catch { }
        }

        private void KillProcessesByName(string name)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(name);
                foreach (Process p in processes)
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }

        // Custom methods for Tray Icon, Size Changed override, Registry and Settings
        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "RBW Stack";
            
            try
            {
                trayIcon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                    if (File.Exists(iconPath))
                        trayIcon.Icon = new Icon(iconPath);
                    else
                        trayIcon.Icon = SystemIcons.Application;
                }
                catch { trayIcon.Icon = SystemIcons.Application; }
            }

            // Left click: mở giao diện | Right click: hiện popup đẹp
            trayIcon.MouseClick += (s, e) => {
                MouseEventArgs me = (MouseEventArgs)e;
                if (me.Button == MouseButtons.Left)
                    ShowMainForm();
                else if (me.Button == MouseButtons.Right)
                    ShowTrayPopup();
            };
            trayIcon.DoubleClick += (s, e) => ShowMainForm();
            trayIcon.Visible = true;
        }

        private void ShowTrayPopup()
        {
            if (_trayPopup != null && !_trayPopup.IsDisposed)
            {
                _trayPopup.Close();
                _trayPopup = null;
                return;
            }

            bool isWebRunning = IsProcessRunning(selectedWebServerType == "Apache" ? "httpd" : "nginx")
                || (procWebServer != null && !procWebServer.HasExited);
            bool isPhpRunning = IsProcessRunning("php-cgi") || (procPHP != null && !procPHP.HasExited);
            bool isMysqlRunning = IsProcessRunning("mysqld") || (procMySQL != null && !procMySQL.HasExited);
            string phpVer = (cbPhpVersions != null && cbPhpVersions.SelectedItem != null)
                ? cbPhpVersions.SelectedItem.ToString() : "";

            bool isRealExitRef = false;
            _trayPopup = new TrayPopupForm(
                selectedWebServerType, isWebRunning,
                phpVer, isPhpRunning,
                isMysqlRunning,
                () => StartAll_Click(null, null),
                () => StopAll_Click(null, null),
                ShowMainForm,
                () => { isRealExit = true; trayIcon.Visible = false; Application.Exit(); }
            );

            // Vị trí: góc dưới phải ngay trên taskbar
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            _trayPopup.StartPosition = FormStartPosition.Manual;
            _trayPopup.Location = new Point(
                workArea.Right - _trayPopup.Width - 12,
                workArea.Bottom - _trayPopup.Height - 12
            );

            _trayPopup.FormClosed += (s, e) => { _trayPopup = null; };
            _trayPopup.Show();
            _trayPopup.BringToFront();
            _trayPopup.Activate();
        }

        private void ShowMainForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            this.BringToFront();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                if (chkMinimizeToTray != null && chkMinimizeToTray.Checked)
                {
                    this.Hide();
                }
            }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RBWStack.exe");
                            key.SetValue("RBWStack", "\"" + exePath + "\" --startup");
                        }
                        else
                        {
                            key.DeleteValue("RBWStack", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thay đổi cài đặt khởi động cùng Windows: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("RBWStack");
                        return val != null;
                    }
                }
            }
            catch { }
            return false;
        }

        private string RegPath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLower().TrimEnd('\\', '/');
                string cleanPath = Regex.Replace(baseDir, @"[^a-zA-Z0-9]", "_");
                if (cleanPath.Length > 200) cleanPath = cleanPath.Substring(cleanPath.Length - 200);
                return @"SOFTWARE\RBWStack\" + cleanPath;
            }
        }

        private void SaveAutoOptimizePhpIniSetting(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    if (key != null)
                    {
                        key.SetValue("AutoOptimizePhpIni", enable ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private bool LoadAutoOptimizePhpIniSetting()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegPath))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AutoOptimizePhpIni");
                        if (val != null)
                        {
                            return (int)val == 1;
                        }
                    }
                }
            }
            catch { }
            return true; // Default to true!
        }

        private void SaveMinimizeToTraySetting(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    if (key != null)
                    {
                        key.SetValue("MinimizeToTray", enable ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private bool LoadMinimizeToTraySetting()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegPath))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("MinimizeToTray");
                        if (val != null)
                        {
                            return (int)val == 1;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private void SaveActiveVersionSetting(string keyName, string value)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    if (key != null) key.SetValue(keyName, value);
                }
            }
            catch { }
        }

        private void LoadActiveVersionsFromRegistry()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegPath))
                {
                    if (key != null)
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        object valType = key.GetValue("ActiveWebServerType");
                        if (valType != null) selectedWebServerType = valType.ToString();

                        object valPhp = key.GetValue("ActivePHPVersion");
                        if (valPhp != null)
                        {
                            string phpVer = valPhp.ToString();
                            string fullPhpDir = Path.Combine(baseDir, @"bin\php", phpVer);
                            if (Directory.Exists(fullPhpDir))
                            {
                                pathPhpExe = Path.Combine(fullPhpDir, "php.exe");
                                pathPhpCgiExe = Path.Combine(fullPhpDir, "php-cgi.exe");
                                pathPhpConf = Path.Combine(fullPhpDir, "php.ini");
                            }
                        }

                        object valMysql = key.GetValue("ActiveMySQLVersion");
                        if (valMysql != null)
                        {
                            string mysqlVer = valMysql.ToString();
                            string fullMysqlDir = Path.Combine(baseDir, @"bin\mysql", mysqlVer);
                            if (Directory.Exists(fullMysqlDir))
                            {
                                pathMySqlExe = Path.Combine(fullMysqlDir, @"bin\mysqld.exe");
                                pathMySqlConf = Path.Combine(fullMysqlDir, "my.ini");
                            }
                        }

                        object valWebVer = key.GetValue("ActiveWebServerVersion");
                        if (valWebVer != null)
                        {
                            string webVer = valWebVer.ToString();
                            if (selectedWebServerType == "Apache")
                            {
                                string fullApacheDir = Path.Combine(baseDir, @"bin\apache", webVer);
                                if (Directory.Exists(fullApacheDir))
                                {
                                    pathApacheExe = Path.Combine(fullApacheDir, @"bin\httpd.exe");
                                    pathApacheConf = Path.Combine(fullApacheDir, @"conf\httpd.conf");
                                }
                            }
                            else
                            {
                                string fullNginxDir = Path.Combine(baseDir, @"bin\nginx", webVer);
                                if (Directory.Exists(fullNginxDir) || webVer == "Default")
                                {
                                    if (webVer == "Default")
                                    {
                                        pathNginxExe = Path.Combine(baseDir, @"bin\nginx\nginx.exe");
                                        pathNginxConf = Path.Combine(baseDir, @"bin\nginx\conf\nginx.conf");
                                    }
                                    else
                                    {
                                        pathNginxExe = Path.Combine(baseDir, @"bin\nginx", webVer, "nginx.exe");
                                        pathNginxConf = Path.Combine(baseDir, @"bin\nginx", webVer, @"conf\nginx.conf");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // ==========================================
        // DOWNLOAD CENTER & DRAWING LOGIC FOR MAIN DISPLAY
        // ==========================================
        private void DrawCardBorder(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            if (p != null)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radius = 8;

                // Clear background with parent's backcolor to prevent double-drawn smudged outlines
                if (p.Parent != null)
                {
                    using (SolidBrush parentBrush = new SolidBrush(p.Parent.BackColor))
                    {
                        e.Graphics.FillRectangle(parentBrush, p.ClientRectangle);
                    }
                }

                bool isSelected = false;
                if (p == pnlRowWeb && selectedService == "Web") isSelected = true;
                else if (p == pnlRowMySql && selectedService == "MySQL") isSelected = true;
                else if (p == pnlRowPhp && selectedService == "PHP") isSelected = true;

                // 1. Fill card background smoothly (selected Gray-100, hover uses panel's BackColor, default is White)
                Color bg = isSelected ? Color.FromArgb(243, 244, 246) : (p.BackColor == Color.Transparent ? Color.White : p.BackColor);
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), radius))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                // 2. Draw card border
                Color borderCol = isSelected ? Color.FromArgb(16, 185, 129) : Color.FromArgb(229, 231, 235);
                float borderWidth = isSelected ? 1.5f : 1.0f;
                using (Pen pen = new Pen(borderCol, borderWidth))
                {
                    pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                    using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), radius))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
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
            // Fully disabled to prevent Access Violations / GDI+ Region crashes
        }

        private void SetupDownloadRows()
        {
            // php
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

            // Apache
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
                    string currentPort = GetWebPort();
                    DownloadCenterForm.ConfigureApache(destFolder, currentPort, Path.GetDirectoryName(pathPhpExe));
                }
            };
            rowsList.Add(apacheRow);

            // Nginx
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
                    string currentPort = GetWebPort();
                    DownloadCenterForm.ConfigureNginx(destFolder, currentPort, Path.GetDirectoryName(pathPhpExe));
                }
            };
            rowsList.Add(nginxRow);

            // MySQL
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
                    string currentPort = GetMySqlPort();
                    DownloadCenterForm.ConfigureMySQL(destFolder, currentPort);
                }
            };
            rowsList.Add(mysqlRow);

            // phpMyAdmin
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
                PostExtractAction = (destFolder) => DownloadCenterForm.ConfigurePhpMyAdmin(destFolder)
            };
            rowsList.Add(pmaRow);

            // VC++
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

            // Node.js
            var nodeRow = new DownloadRow
            {
                ComponentName = "Node.js Engine",
                BaseDestFolder = @"bin\node",
                ZipFileName = "node_temp.zip",
                Options = new List<VersionOption>
                {
                    new VersionOption { DisplayName = "Node.js v20.12.2 (LTS)", Url = "https://nodejs.org/dist/v20.12.2/node-v20.12.2-win-x64.zip", FolderName = "node-v20.12.2" },
                    new VersionOption { DisplayName = "Node.js v22.1.0 (Mới nhất)", Url = "https://nodejs.org/dist/v22.1.0/node-v22.1.0-win-x64.zip", FolderName = "node-v22.1.0" },
                    new VersionOption { DisplayName = "Node.js v18.20.2 (LTS cũ)", Url = "https://nodejs.org/dist/v18.20.2/node-v18.20.2-win-x64.zip", FolderName = "node-v18.20.2" }
                },
                PostExtractAction = null
            };
            rowsList.Add(nodeRow);

            // Cloudflare Tunnel
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

        private void LoadLatestVersionsFromWeb()
        {
            ThreadPool.QueueUserWorkItem(state => {
                try
                {
                    // Enforce TLS 1.2
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                    List<VersionOption> webPhp = ScrapePhp();
                    List<VersionOption> webApache = ScrapeApache();
                    List<VersionOption> webNginx = ScrapeNginx();
                    List<VersionOption> webMaria = ScrapeMariaDB();
                    List<VersionOption> webPma = ScrapePma();

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
                            isSyncCompleted = true;
                            RenderDownloadCards();
                            lblStatusText.Text = "Không có kết nối Internet / Trang chủ đổi định dạng. Sử dụng danh sách phiên bản offline có sẵn.";
                            if (lblSyncStatus != null)
                            {
                                lblSyncStatus.Text = "⚠️  SỬ DỤNG PHIÊN BẢN OFFLINE";
                                lblSyncStatus.ForeColor = Color.FromArgb(245, 158, 11); // Amber
                                System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer();
                                fadeTimer.Interval = 5000;
                                fadeTimer.Tick += (senderTimer, eTimer) => {
                                    lblSyncStatus.Visible = false;
                                    fadeTimer.Stop();
                                    fadeTimer.Dispose();
                                };
                                fadeTimer.Start();
                            }
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
                string html = wc.DownloadString("https://windows.php.net/downloads/releases/");
                
                MatchCollection matches = Regex.Matches(html, @"href=""(php-(\d+\.\d+\.\d+)-Win32-(?:vs\d+|vc\d+)-x64\.zip)""");
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

                for (int i = 0; i < Math.Min(temp.Count, 6); i++)
                {
                    list.Add(temp[i]);
                }

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
            if (phps.Count > 0) rowsList[0].Options = phps;
            if (apaches.Count > 0) rowsList[1].Options = apaches;
            if (nginxes.Count > 0) rowsList[2].Options = nginxes;
            if (marias.Count > 0) rowsList[3].Options = marias;
            if (pmas.Count > 0) rowsList[4].Options = pmas;

            isSyncCompleted = true;
            RenderDownloadCards();

            if (lblSyncStatus != null)
            {
                lblSyncStatus.Text = "✨  ĐÃ ĐỒNG BỘ PHIÊN BẢN";
                lblSyncStatus.ForeColor = Color.FromArgb(16, 185, 129); // Emerald green
                System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer();
                fadeTimer.Interval = 5000;
                fadeTimer.Tick += (senderTimer, eTimer) => {
                    lblSyncStatus.Visible = false;
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                };
                fadeTimer.Start();
            }
        }

        private void UpdateCombo(ComboBox cb, List<VersionOption> options)
        {
            if (cb == null) return;
            cb.Items.Clear();
            foreach (var opt in options)
            {
                cb.Items.Add(opt);
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private bool IsVCRuntimeInstalled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Installed");
                        if (val != null && val.ToString() == "1")
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private bool IsActiveDownloadingOrInstalling()
        {
            if (webClient != null && webClient.IsBusy) return true;
            if (lblStatusText != null && (
                lblStatusText.Text.Contains("Đang tải") ||
                lblStatusText.Text.Contains("Đang kết nối") ||
                lblStatusText.Text.Contains("Đang tự động giải nén") ||
                lblStatusText.Text.Contains("Đang khởi chạy trình cài đặt")
            )) return true;
            return false;
        }

        private void RenderDownloadCards()
        {
            if (pnlCardsContainer == null) return;

            pnlCardsContainer.SuspendLayout();
            pnlCardsContainer.Controls.Clear();

            // Set progress bar to marquee when syncing in background
            if (pbDownload != null)
            {
                if (!isSyncCompleted)
                {
                    pbDownload.Style = ProgressBarStyle.Marquee;
                    pbDownload.MarqueeAnimationSpeed = 50;
                    lblStatusText.Text = "Đang đồng bộ danh sách phiên bản mới nhất trực tuyến...";
                }
                else
                {
                    pbDownload.Style = ProgressBarStyle.Blocks;
                    pbDownload.Value = 100;
                }
            }

            if (pnlProgress != null)
            {
                if (!isSyncCompleted || IsActiveDownloadingOrInstalling())
                {
                    pnlProgress.Visible = true;
                }
                else
                {
                    pnlProgress.Visible = false;
                }
            }

            if (!isSyncCompleted)
            {
                // Create a beautiful loading card in the center of the list
                Panel pnlLoadingCard = new Panel();
                pnlLoadingCard.Size = new Size(710, 80);
                pnlLoadingCard.Location = new Point(10, 15);
                pnlLoadingCard.BackColor = Color.Transparent;
                pnlLoadingCard.Paint += DrawCardBorder;
                pnlCardsContainer.Controls.Add(pnlLoadingCard);
                ApplyRoundedRegion(pnlLoadingCard, 10);

                Label lblLoading = new Label();
                lblLoading.Text = "⏳  ĐANG ĐỒNG BỘ PHIÊN BẢN MỚI NHẤT...";
                lblLoading.ForeColor = colorText;
                lblLoading.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
                lblLoading.Location = new Point(20, 18);
                lblLoading.AutoSize = true;
                pnlLoadingCard.Controls.Add(lblLoading);

                Label lblLoadingDesc = new Label();
                lblLoadingDesc.Text = "Hệ thống đang quét và đồng bộ dữ liệu phiên bản từ trang chủ dịch vụ, vui lòng chờ giây lát...";
                lblLoadingDesc.ForeColor = colorTextDim;
                lblLoadingDesc.Font = new Font("Segoe UI Italic", 8.5f);
                lblLoadingDesc.Location = new Point(20, 42);
                lblLoadingDesc.AutoSize = true;
                pnlLoadingCard.Controls.Add(lblLoadingDesc);

                pnlCardsContainer.ResumeLayout();
                pnlCardsContainer.Invalidate();
                return;
            }

            int currentY = 15;
            foreach (var row in rowsList)
            {
                if (activeFilterComponentName != "ALL" && row.ComponentName != activeFilterComponentName)
                {
                    continue;
                }

                foreach (var opt in row.Options)
                {
                    Panel pnlCard = new Panel();
                    pnlCard.Size = new Size(710, 60); // Prevents horizontal scrollbar in container!
                    pnlCard.Location = new Point(10, currentY);
                    pnlCard.BackColor = Color.Transparent;
                    pnlCard.Paint += DrawCardBorder;
                    pnlCardsContainer.Controls.Add(pnlCard);
                    ApplyRoundedRegion(pnlCard, 10);

                    Label lblName = new Label();
                    lblName.Text = opt.DisplayName;
                    lblName.ForeColor = colorText;
                    lblName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    lblName.Location = new Point(15, 8);
                    lblName.AutoSize = true;
                    pnlCard.Controls.Add(lblName);

                    Label lblDesc = new Label();
                    if (string.IsNullOrEmpty(opt.FolderName))
                    {
                        lblDesc.Text = "Lưu tại: .\\" + row.BaseDestFolder;
                    }
                    else
                    {
                        lblDesc.Text = "Lưu tại: .\\" + row.BaseDestFolder + "\\" + opt.FolderName;
                    }
                    lblDesc.ForeColor = colorTextDim;
                    lblDesc.Font = new Font("Segoe UI Italic", 8.25f);
                    lblDesc.Location = new Point(15, 30);
                    lblDesc.AutoSize = true;
                    pnlCard.Controls.Add(lblDesc);

                    // Check if already installed
                    bool isInstalled = false;
                    if (row.ComponentName.Contains("Visual C++"))
                    {
                        isInstalled = IsVCRuntimeInstalled();
                    }
                    else if (row.ComponentName.Contains("phpMyAdmin"))
                    {
                        isInstalled = Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.IsNullOrEmpty(opt.FolderName) ? "phpmyadmin" : opt.FolderName));
                    }
                    else
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, row.BaseDestFolder, opt.FolderName);
                        isInstalled = Directory.Exists(fullPath);
                    }

                    ModernButton btnAction = new ModernButton();
                    btnAction.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    btnAction.Location = new Point(585, 14);
                    btnAction.Size = new Size(110, 32);

                    if (isInstalled)
                    {
                        btnAction.Text = "✓ ĐÃ CÀI ĐẶT";
                        btnAction.NormalColor = Color.FromArgb(240, 253, 244); // light green
                        btnAction.BorderColor = Color.FromArgb(16, 185, 129); // emerald
                        btnAction.ForeColor = Color.FromArgb(16, 185, 129); // emerald
                        btnAction.Enabled = false;
                    }
                    else
                    {
                        btnAction.Text = "CÀI ĐẶT";
                        btnAction.IconGlyph = "\uE896"; // Download
                        btnAction.NormalColor = Color.White;
                        btnAction.BorderColor = colorBorder;
                        btnAction.ForeColor = Color.FromArgb(55, 65, 81);
                        
                        DownloadRow capturedRow = row;
                        VersionOption capturedOpt = opt;
                        btnAction.Click += (s, ev) => StartDownloadFlowForOption(capturedRow, capturedOpt);
                    }

                    pnlCard.Controls.Add(btnAction);

                    currentY += 66;
                }
            }

            pnlCardsContainer.ResumeLayout();
            pnlCardsContainer.Invalidate();
        }

        private void StartDownloadFlowForOption(DownloadRow row, VersionOption selectedOpt)
        {
            if (webClient != null && webClient.IsBusy)
            {
                MessageBox.Show("Có tiến trình tải khác đang chạy. Vui lòng chờ đợi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedOpt == null) return;

            if (!Directory.Exists("downloads")) Directory.CreateDirectory("downloads");
            if (!Directory.Exists("www")) Directory.CreateDirectory("www");

            string zipPath = Path.Combine("downloads", row.ZipFileName);
            string finalTargetFolder = Path.Combine(row.BaseDestFolder, selectedOpt.FolderName);

            string foundZip = null;
            try
            {
                string filter = "*.zip";
                if (row.BaseDestFolder.Contains("php")) filter = "*php*.zip";
                else if (row.BaseDestFolder.Contains("mysql")) filter = "*mysql*.zip";
                else if (row.BaseDestFolder.Contains("apache")) filter = "*httpd*.zip";
                else if (row.BaseDestFolder.Contains("nginx")) filter = "*nginx*.zip";
                else if (row.ComponentName.Contains("phpMyAdmin")) filter = "*phpmyadmin*.zip";

                string[] files = Directory.GetFiles("downloads", filter);
                if (files.Length > 0)
                {
                    long maxLen = 0;
                    foreach (var f in files)
                    {
                        long len = new FileInfo(f).Length;
                        if (len > 5 * 1024 * 1024 && len > maxLen) // > 5MB
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
                    if (pnlProgress != null) pnlProgress.Visible = true;
                    ExtractAndInstall(foundZip, finalTargetFolder, row, selectedOpt.DisplayName);
                    return;
                }
            }

            if (pnlProgress != null) pnlProgress.Visible = true;
            lblStatusText.Text = "Đang kết nối để tải " + selectedOpt.DisplayName + "...";
            pbDownload.Style = ProgressBarStyle.Marquee;
            pbDownload.Value = 0;

            webClient = new WebClient();
            webClient.Proxy = null;
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

                    try
                    {
                        if (File.Exists(zipPath))
                        {
                            long fileSize = new FileInfo(zipPath).Length;
                            if (fileSize < 100 * 1024)
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

        private void StartDownloadFlow(DownloadRow row)
        {
            if (row.ComboSelector == null) return;
            
            if (webClient != null && webClient.IsBusy)
            {
                MessageBox.Show("Có tiến trình tải khác đang chạy. Vui lòng chờ đợi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            VersionOption selectedOpt = row.ComboSelector.SelectedItem as VersionOption;
            if (selectedOpt == null) return;
            
            StartDownloadFlowForOption(row, selectedOpt);
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
                    
                    InvokeUpdatePaths();
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
                    string iniContent = "[PHP]\r\nengine = On\r\nshort_open_tag = Off\r\nmax_execution_time = 3600\r\nmax_input_time = 3600\r\nmemory_limit = 1024M\r\nerror_reporting = E_ALL\r\ndisplay_errors = Off\r\npost_max_size = 2048M\r\nupload_max_filesize = 2048M\r\nmax_file_uploads = 200\r\nupload_tmp_dir = \"" + tempPathFwd + "\"\r\nsys_temp_dir = \"" + tempPathFwd + "\"\r\nextension_dir = \"ext\"\r\nextension = curl\r\nextension = fileinfo\r\nextension = gd\r\nextension = mbstring\r\nextension = mysqli\r\nextension = openssl\r\nextension = pdo_mysql\r\nextension = ftp\r\nextension = zip\r\nsession.gc_maxlifetime = 2592000\r\ndate.timezone = Asia/Ho_Chi_Minh\r\n";
                    File.WriteAllText(phpIniPath, iniContent);
                    return;
                }
            }

            try
            {
                string content = File.ReadAllText(phpIniPath);
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

                    string pattern = @"(?m)^\s*extension\s*=\s*(?:php_)?" + targetExt + @"(?:\.dll)?\s*$";
                    bool alreadyEnabled = Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase);
                    if (!alreadyEnabled)
                    {
                        string uncommentPattern = @"(?m)^;+\s*extension\s*=\s*((?:php_)?" + targetExt + @"(?:\.dll)?)\s*$";
                        string uncommented = Regex.Replace(content, uncommentPattern, "extension = $1", RegexOptions.IgnoreCase);
                        if (uncommented != content)
                        {
                            content = uncommented;
                        }
                        else
                        {
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

                if (Regex.IsMatch(content, @"date\.timezone\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @";?\s*date\.timezone\s*=.*", "date.timezone = Asia/Ho_Chi_Minh");
                }
                else
                {
                    content += "\r\n[Date]\r\ndate.timezone = Asia/Ho_Chi_Minh\r\n";
                }

                if (!Regex.IsMatch(content, @"^\s*display_errors\s*=\s*Off", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    content = Regex.Replace(content, @"(?m)^\s*display_errors\s*=.*$", "display_errors = Off", RegexOptions.IgnoreCase);
                }
                if (!Regex.IsMatch(content, @"^\s*log_errors\s*=\s*On", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    content = Regex.Replace(content, @"(?m)^\s*log_errors\s*=.*$", "log_errors = On", RegexOptions.IgnoreCase);
                }

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
                    content = Regex.Replace(content, @"(?m)^;?\s*max_execution_time\s*=.*$", "max_execution_time = 3600", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_execution_time = 3600\r\n";
                }

                if (Regex.IsMatch(content, @"max_input_time\s*=", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"(?m)^;?\s*max_input_time\s*=.*$", "max_input_time = 3600", RegexOptions.IgnoreCase);
                }
                else
                {
                    content += "\r\nmax_input_time = 3600\r\n";
                }

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

                string sessionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "sessions");
                if (!Directory.Exists(sessionDir)) Directory.CreateDirectory(sessionDir);
                string sessionPathFwd = sessionDir.Replace("\\", "/");
                if (Regex.IsMatch(content, @"session\.save_path\s*=", RegexOptions.IgnoreCase))
                {
                    bool replaced = false;
                    content = Regex.Replace(content, @"(?m)^;?\s*session\.save_path\s*=.*$", m => {
                        if (!replaced) { replaced = true; return "session.save_path = \"" + sessionPathFwd + "\""; }
                        return "";
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

                File.WriteAllText(phpIniPath, content);
            }
            catch { }
        }

        private void UpdateDetailsPane()
        {
            if (lblDetailTitle == null || txtEnvVars == null || txtLogSnippets == null) return;

            if (selectedService == "Web")
            {
                lblDetailTitle.Text = selectedWebServerType.ToUpper();
                string envText = string.Format(
                    "APP_URL=http://localhost:{0}\r\n" +
                    "SERVER_SOFTWARE={1}\r\n" +
                    "WEB_PORT={0}\r\n" +
                    "DOCUMENT_ROOT=./www\r\n" +
                    "INDEX_PAGE=index.php\r\n",
                    GetWebPort(), selectedWebServerType
                );
                SetTextBoxTextIfChanged(txtEnvVars, envText);

                string logPath = (selectedWebServerType == "Apache") 
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\apache\logs\error.log") 
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\nginx\logs\error.log");
                
                SetTextBoxTextIfChanged(txtLogSnippets, GetLastLogLines(logPath, 15));
            }
            else if (selectedService == "MySQL")
            {
                lblDetailTitle.Text = "MySQL / MariaDB";
                string envText = string.Format(
                    "DB_CONNECTION=mysql\r\n" +
                    "DB_HOST=127.0.0.1\r\n" +
                    "DB_PORT={0}\r\n" +
                    "DB_DATABASE=laravel\r\n" +
                    "DB_USERNAME=root\r\n" +
                    "DB_PASSWORD=\r\n",
                    GetMySqlPort()
                );
                SetTextBoxTextIfChanged(txtEnvVars, envText);

                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\mysql\data\mysql.err");
                if (!File.Exists(logPath))
                {
                    try
                    {
                        string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\mysql\data");
                        if (Directory.Exists(dataDir))
                        {
                            string[] errFiles = Directory.GetFiles(dataDir, "*.err");
                            if (errFiles.Length > 0) logPath = errFiles[0];
                        }
                    }
                    catch { }
                }
                SetTextBoxTextIfChanged(txtLogSnippets, GetLastLogLines(logPath, 15));
            }
            else if (selectedService == "PHP")
            {
                lblDetailTitle.Text = "PHP Engine";
                string envText = 
                    "PHP_VERSION=" + (cbPhpVersions.SelectedItem != null ? cbPhpVersions.SelectedItem.ToString() : "8.3.6") + "\r\n" +
                    "PHP_FCGI_MAX_REQUESTS=500\r\n" +
                    "PHP_INI_SCAN_DIR=\r\n" +
                    "PHPRC=./bin/php/php.ini\r\n" +
                    "PHP_FCGI_CHILDREN=4\r\n";
                SetTextBoxTextIfChanged(txtEnvVars, envText);

                string logText = "[PHP Engine Live Logs]\r\n" +
                                 "[System] Starting php-cgi process...\r\n" +
                                 "[System] Binding to 127.0.0.1:9000...\r\n" +
                                 "[System] Session save path: ./tmp/sessions\r\n" +
                                 "[System] Engine initialized successfully.";
                SetTextBoxTextIfChanged(txtLogSnippets, logText);
            }
        }

        private void SetTextBoxTextIfChanged(TextBox tb, string newText)
        {
            if (tb.Text != newText)
            {
                tb.Text = newText;
                tb.SelectionStart = tb.Text.Length;
                tb.ScrollToCaret();
            }
        }

        private string GetLastLogLines(string filePath, int lineCount)
        {
            if (!File.Exists(filePath)) return "[Trạng thái] Chưa có dữ liệu logs / Dịch vụ đang dừng.";
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length <= lineCount) return string.Join("\r\n", lines);
                string[] lastLines = new string[lineCount];
                Array.Copy(lines, lines.Length - lineCount, lastLines, 0, lineCount);
                return string.Join("\r\n", lastLines);
            }
            catch
            {
                return "[Lỗi] Không thể đọc tệp tin logs.";
            }
        }

        private void RegisterClickRecursive(Control parent, EventHandler handler)
        {
            if (parent is Button || parent is ComboBox) return;
            parent.Click += handler;
            foreach (Control c in parent.Controls)
            {
                RegisterClickRecursive(c, handler);
            }
        }

        private void RegisterHoverRecursive(Control control, Panel targetPanel)
        {
            if (control is Button || control is ComboBox) return;
            
            control.Cursor = Cursors.Hand;
            
            control.MouseEnter += (s, e) => {
                targetPanel.BackColor = Color.FromArgb(249, 250, 251);
                targetPanel.Invalidate();
            };
            
            control.MouseLeave += (s, e) => {
                targetPanel.BackColor = Color.Transparent;
                targetPanel.Invalidate();
            };
            
            foreach (Control c in control.Controls)
            {
                RegisterHoverRecursive(c, targetPanel);
            }
        }

        private void WebRow_Click(object sender, EventArgs e)
        {
            selectedService = "Web";
            HighlightActiveRow();
            UpdateDetailsPane();
        }

        private void MySqlRow_Click(object sender, EventArgs e)
        {
            selectedService = "MySQL";
            HighlightActiveRow();
            UpdateDetailsPane();
        }

        private void PhpRow_Click(object sender, EventArgs e)
        {
            selectedService = "PHP";
            HighlightActiveRow();
            UpdateDetailsPane();
        }

        private void HighlightActiveRow()
        {
            if (pnlRowWeb != null) pnlRowWeb.Invalidate();
            if (pnlRowMySql != null) pnlRowMySql.Invalidate();
            if (pnlRowPhp != null) pnlRowPhp.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(colorBorder, 1.0f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private void DrawTabPanelBorder(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            if (p != null)
            {
                using (Pen pen = new Pen(colorBorder, 1.0f))
                {
                    // Draw outer right border line
                    e.Graphics.DrawLine(pen, p.Width - 1, 0, p.Width - 1, p.Height);
                    // Draw outer bottom border line
                    e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
                }
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Enable taskbar click minimizing/restoring for FormBorderStyle = None
                const int WS_MINIMIZEBOX = 0x20000;
                const int CS_DBLCLKS = 0x8;
                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }

        public static void AddHostsEntry(string hostname)
        {
            if (!IsAdminVHostMode()) return;
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (File.Exists(hostsPath))
                {
                    string[] lines = File.ReadAllLines(hostsPath);
                    List<string> newLines = new List<string>();
                    bool exists = false;
                    bool corruptedFixed = false;

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\b" + System.Text.RegularExpressions.Regex.Escape(hostname) + @"\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            if (trimmed.Contains("127.0.0.1") && !trimmed.StartsWith("127.0.0.1") && !trimmed.StartsWith("#"))
                            {
                                int idx = trimmed.IndexOf("127.0.0.1");
                                string part1 = trimmed.Substring(0, idx).Trim();
                                string part2 = trimmed.Substring(idx).Trim();
                                newLines.Add(part1);
                                newLines.Add(part2);
                                corruptedFixed = true;
                                exists = true;
                                continue;
                            }
                            exists = true;
                        }
                        newLines.Add(line);
                    }

                    if (!exists)
                    {
                        string lastLine = lines.Length > 0 ? lines[lines.Length - 1] : "";
                        bool needNewLine = lines.Length > 0 && !lastLine.EndsWith("\n") && !lastLine.EndsWith("\r") && !string.IsNullOrEmpty(lastLine.Trim());
                        
                        try
                        {
                            using (StreamWriter sw = File.AppendText(hostsPath))
                            {
                                if (needNewLine) sw.WriteLine();
                                sw.WriteLine(string.Format("127.0.0.1 {0}", hostname));
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            string appendCmd = string.Format("127.0.0.1 {0}", hostname);
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = "cmd.exe";
                            psi.Arguments = needNewLine 
                                ? string.Format("/c echo. >> \"{0}\" && echo {1} >> \"{0}\"", hostsPath, appendCmd)
                                : string.Format("/c echo {1} >> \"{0}\"", hostsPath, appendCmd);
                            psi.Verb = "runas";
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = true;
                            Process p = Process.Start(psi);
                            p.WaitForExit();
                        }
                    }
                    else if (corruptedFixed)
                    {
                        try
                        {
                            File.WriteAllLines(hostsPath, newLines.ToArray());
                        }
                        catch (UnauthorizedAccessException)
                        {
                            string tempFile = Path.Combine(Path.GetTempPath(), "hosts_temp");
                            File.WriteAllLines(tempFile, newLines.ToArray());
                            
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = "cmd.exe";
                            psi.Arguments = string.Format("/c copy /y \"{0}\" \"{1}\" && del /f /q \"{0}\"", tempFile, hostsPath);
                            psi.Verb = "runas";
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = true;
                            Process p = Process.Start(psi);
                            p.WaitForExit();
                        }
                    }
                }
            }
            catch { }
        }

        public static string GetSitesParentDirectory()
        {
            try
            {
                string pathFile = ConfigHelper.GetDataFilePath("sites_root.txt");
                if (File.Exists(pathFile))
                {
                    string savedPath = File.ReadAllText(pathFile).Trim();
                    if (Directory.Exists(savedPath))
                    {
                        return savedPath;
                    }
                }
            }
            catch { }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
        }

        public static Dictionary<string, string> LoadSitesConfig()
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            try
            {
                string path = ConfigHelper.GetDataFilePath("sites.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    MatchCollection matches = Regex.Matches(json, @"""([^""]+)""\s*:\s*""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        config[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                }
            }
            catch { }
            return config;
        }

        public static void SaveSitesConfig(Dictionary<string, string> config)
        {
            try
            {
                string path = ConfigHelper.GetDataFilePath("sites.json");
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                int count = 0;
                foreach (var kvp in config)
                {
                    sb.AppendFormat("  \"{0}\": \"{1}\"", kvp.Key, kvp.Value);
                    if (++count < config.Count) sb.AppendLine(",");
                    else sb.AppendLine("");
                }
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }

        public static Dictionary<string, string> LoadSslConfig()
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            try
            {
                string path = ConfigHelper.GetDataFilePath("ssl.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    MatchCollection matches = Regex.Matches(json, @"""([^""]+)""\s*:\s*""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        config[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                }
            }
            catch { }
            return config;
        }

        public static void SaveSslConfig(Dictionary<string, string> config)
        {
            try
            {
                string path = ConfigHelper.GetDataFilePath("ssl.json");
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                int count = 0;
                foreach (var kvp in config)
                {
                    sb.AppendFormat("  \"{0}\": \"{1}\"", kvp.Key, kvp.Value);
                    if (++count < config.Count) sb.AppendLine(",");
                    else sb.AppendLine("");
                }
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }

        public static string LoadRootProjectConfig()
        {
            try
            {
                string pathFile = ConfigHelper.GetDataFilePath("root_project.txt");
                if (File.Exists(pathFile))
                {
                    return File.ReadAllText(pathFile).Trim();
                }
            }
            catch { }
            return "";
        }

        public static void SaveRootProjectConfig(string relativeSitePath)
        {
            try
            {
                string pathFile = ConfigHelper.GetDataFilePath("root_project.txt");
                File.WriteAllText(pathFile, relativeSitePath.Trim());
                UpdateAllProjectsEnvFiles(relativeSitePath.Trim());
            }
            catch { }
        }

        public static string GetActiveWebPort()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f is MainForm)
                    {
                        return ((MainForm)f).GetWebPort();
                    }
                }
            }
            catch { }
            return "80";
        }

        public static void UpdateAllProjectsEnvFiles(string activeRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(activeRoot))
                {
                    activeRoot = LoadRootProjectConfig();
                }

                string sitesParent = GetSitesParentDirectory();
                string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
                if (Directory.Exists(sitesParent))
                {
                    List<string> envFiles = new List<string>();
                    FindEnvFiles(sitesParent, envFiles, 0);

                    foreach (string envPath in envFiles)
                    {
                        string projectDir = Path.GetDirectoryName(envPath);
                        string folderName = Path.GetFileName(projectDir);
                        
                        string relativeSitePath = "";
                        if (projectDir.StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase))
                        {
                            relativeSitePath = projectDir.Substring(wwwDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                        }
                        else
                        {
                            relativeSitePath = folderName;
                        }

                        if (string.IsNullOrEmpty(relativeSitePath)) continue;

                        try
                        {
                            bool isRoot = relativeSitePath.Equals(activeRoot, StringComparison.OrdinalIgnoreCase);
                            bool vhostEnabled;
                            string vhostDomain;
                            bool vhostUseSsl;
                            GetVHostConfig(relativeSitePath, folderName, out vhostEnabled, out vhostDomain, out vhostUseSsl);

                            bool isSiteRoot = isRoot || vhostEnabled;
                            string expectedVal = isSiteRoot ? "/" : "/" + relativeSitePath.TrimEnd('/') + "/";

                            string[] lines = File.ReadAllLines(envPath);
                            string currentUrl = "";
                            bool foundUrl = false;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                var match = Regex.Match(lines[i], @"^\s*APP_URL\s*=\s*(.*)");
                                if (match.Success)
                                {
                                    currentUrl = match.Groups[1].Value.Trim();
                                    foundUrl = true;
                                    break;
                                }
                            }

                            string expectedUrl = "";

                            if (foundUrl && !string.IsNullOrEmpty(currentUrl))
                            {
                                if (vhostEnabled)
                                {
                                    if (currentUrl.Contains("localhost"))
                                    {
                                        expectedUrl = Regex.Replace(currentUrl, @"localhost", vhostDomain, RegexOptions.IgnoreCase);
                                    }
                                    else
                                    {
                                        expectedUrl = currentUrl;
                                    }
                                }
                                else
                                {
                                    if (currentUrl.Contains(vhostDomain))
                                    {
                                        expectedUrl = Regex.Replace(currentUrl, Regex.Escape(vhostDomain), "localhost", RegexOptions.IgnoreCase);
                                    }
                                    else
                                    {
                                        expectedUrl = currentUrl;
                                    }
                                }
                            }
                            else
                            {
                                if (vhostEnabled)
                                {
                                    expectedUrl = "\"https://" + vhostDomain + "${SITE_PATH}\"";
                                }
                                else
                                {
                                    expectedUrl = "\"https://localhost${SITE_PATH}\"";
                                }
                            }
                            
                            bool foundPath = false;
                            bool foundUrlLine = false;
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (Regex.IsMatch(lines[i], @"^\s*SITE_PATH\s*="))
                                {
                                    lines[i] = "SITE_PATH=" + expectedVal;
                                    foundPath = true;
                                }
                                if (Regex.IsMatch(lines[i], @"^\s*APP_URL\s*="))
                                {
                                    lines[i] = "APP_URL=" + expectedUrl;
                                    foundUrlLine = true;
                                }
                            }

                            if (!foundPath || !foundUrlLine)
                            {
                                List<string> list = new List<string>(lines);
                                if (!foundPath) list.Add("SITE_PATH=" + expectedVal);
                                if (!foundUrlLine) list.Add("APP_URL=" + expectedUrl);
                                lines = list.ToArray();
                            }

                            File.WriteAllLines(envPath, lines);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void FindEnvFiles(string currentDir, List<string> envFiles, int depth)
        {
            if (depth > 3) return;
            try
            {
                string envPath = Path.Combine(currentDir, ".env");
                if (File.Exists(envPath))
                {
                    envFiles.Add(envPath);
                }

                foreach (string subDir in Directory.GetDirectories(currentDir))
                {
                    string name = Path.GetFileName(subDir);
                    if (name.StartsWith(".")) continue;
                    FindEnvFiles(subDir, envFiles, depth + 1);
                }
            }
            catch { }
        }

        public class GithubReleaseInfo
        {
            public string TagName { get; set; }
            public string DownloadUrl { get; set; }
            public string BridgeDownloadUrl { get; set; }
            public string ReleaseNotes { get; set; }
        }

        public static string LoadUpdateCache(out DateTime lastCheck, out string tagName, out string downloadUrl, out string bridgeDownloadUrl, out string body)
        {
            lastCheck = DateTime.MinValue;
            tagName = "";
            downloadUrl = "";
            bridgeDownloadUrl = "";
            body = "";
            try
            {
                string path = ConfigHelper.GetDataFilePath("update_cache.txt");
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    string[] parts = content.Split(new char[] { '|' }, 5);
                    if (parts.Length >= 5)
                    {
                        long ticks;
                        if (long.TryParse(parts[0], out ticks))
                        {
                            lastCheck = new DateTime(ticks);
                        }
                        tagName = parts[1];
                        downloadUrl = parts[2];
                        bridgeDownloadUrl = parts[3];
                        body = parts[4];
                    }
                }
            }
            catch { }
            return tagName;
        }

        public static void SaveUpdateCache(DateTime lastCheck, string tagName, string downloadUrl, string bridgeDownloadUrl, string body)
        {
            try
            {
                string path = ConfigHelper.GetDataFilePath("update_cache.txt");
                string content = string.Format("{0}|{1}|{2}|{3}|{4}", lastCheck.Ticks, tagName, downloadUrl, bridgeDownloadUrl, body);
                File.WriteAllText(path, content);
            }
            catch { }
        }

        public static GithubReleaseInfo CheckForUpdatesCached(string repo, bool forceCheck)
        {
            DateTime lastCheck;
            string cachedTag, cachedUrl, cachedBridgeUrl, cachedBody;
            LoadUpdateCache(out lastCheck, out cachedTag, out cachedUrl, out cachedBridgeUrl, out cachedBody);

            if (!forceCheck && (DateTime.Now - lastCheck).TotalHours < 24.0)
            {
                if (!string.IsNullOrEmpty(cachedTag))
                {
                    return new GithubReleaseInfo { TagName = cachedTag, DownloadUrl = cachedUrl, BridgeDownloadUrl = cachedBridgeUrl, ReleaseNotes = cachedBody };
                }
            }

            var release = CheckForUpdates(repo);
            if (release != null)
            {
                SaveUpdateCache(DateTime.Now, release.TagName, release.DownloadUrl, release.BridgeDownloadUrl, release.ReleaseNotes);
            }
            return release;
        }

        public static GithubReleaseInfo CheckForUpdates(string repo)
        {
            try
            {
                string url = string.Format("https://api.github.com/repos/{0}/releases/latest", repo);
                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    wc.Encoding = System.Text.Encoding.UTF8;
                    string json = wc.DownloadString(url);

                    string tagName = "";
                    string downloadUrl = "";
                    string bridgeDownloadUrl = "";
                    string body = "";

                    Match mTag = Regex.Match(json, @"""tag_name""\s*:\s*""([^""]+)""");
                    if (mTag.Success) tagName = mTag.Groups[1].Value;

                    MatchCollection mUrls = Regex.Matches(json, @"""browser_download_url""\s*:\s*""([^""]+\.exe)""");
                    if (mUrls.Count > 0)
                    {
                        downloadUrl = mUrls[0].Groups[1].Value;
                    }
                    else
                    {
                        MatchCollection mUrlsFallback = Regex.Matches(json, @"""browser_download_url""\s*:\s*""([^""]+)""");
                        if (mUrlsFallback.Count > 0) downloadUrl = mUrlsFallback[0].Groups[1].Value;
                    }

                    MatchCollection mBridgeUrls = Regex.Matches(json, @"""browser_download_url""\s*:\s*""([^""]+bridge\.php)""");
                    if (mBridgeUrls.Count > 0)
                    {
                        bridgeDownloadUrl = mBridgeUrls[0].Groups[1].Value;
                    }

                    Match mBody = Regex.Match(json, @"""body""\s*:\s*""((?:[^""\\]|\\.)*)""");
                    if (mBody.Success)
                    {
                        body = mBody.Groups[1].Value;
                        try
                        {
                            body = Regex.Unescape(body);
                        }
                        catch { }
                        body = body.Replace("\\r\\n", "\r\n").Replace("\\n", "\n").Replace("\\r", "\r");
                    }

                    if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(downloadUrl))
                    {
                        return new GithubReleaseInfo { TagName = tagName, DownloadUrl = downloadUrl, BridgeDownloadUrl = bridgeDownloadUrl, ReleaseNotes = body };
                    }
                }
            }
            catch { }
            return null;
        }

        public void ShowUpdatePrompt(GithubReleaseInfo release)
        {
            Form f = new Form();
            f.FormBorderStyle = FormBorderStyle.None;
            f.Size = new Size(420, 320);
            f.StartPosition = FormStartPosition.CenterParent;
            f.BackColor = colorBg;
            f.ShowInTaskbar = false;
            
            f.Paint += (s, e) => {
                using (Pen pen = new Pen(colorBorder, 2f))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, f.Width - 2, f.Height - 2);
                }
            };
            
            ApplyRoundedRegion(f, 12);

            Label lblTitle = new Label();
            lblTitle.Text = "🚀  CÓ PHIÊN BẢN CẬP NHẬT MỚI!";
            lblTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(59, 130, 246);
            lblTitle.Location = new Point(20, 25);
            lblTitle.Size = new Size(380, 25);
            f.Controls.Add(lblTitle);

            Label lblVersion = new Label();
            lblVersion.Text = string.Format("Phiên bản hiện tại: v2.1.1  →  Phiên bản mới: {0}", release.TagName);
            lblVersion.Font = new Font("Segoe UI Semibold", 9.5f);
            lblVersion.ForeColor = colorText;
            lblVersion.Location = new Point(20, 60);
            lblVersion.Size = new Size(380, 20);
            f.Controls.Add(lblVersion);

            Label lblNotesTitle = new Label();
            lblNotesTitle.Text = "Nhật ký thay đổi:";
            lblNotesTitle.Font = new Font("Segoe UI Italic", 9f);
            lblNotesTitle.ForeColor = colorTextDim;
            lblNotesTitle.Location = new Point(20, 95);
            lblNotesTitle.Size = new Size(380, 18);
            f.Controls.Add(lblNotesTitle);

            TextBox txtNotes = new TextBox();
            txtNotes.Multiline = true;
            txtNotes.ReadOnly = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            txtNotes.BackColor = Color.White;
            txtNotes.ForeColor = Color.FromArgb(55, 65, 81);
            txtNotes.BorderStyle = BorderStyle.FixedSingle;
            txtNotes.Font = new Font("Segoe UI", 9f);
            txtNotes.Text = string.IsNullOrEmpty(release.ReleaseNotes) ? "Cập nhật hiệu năng và sửa lỗi hệ thống." : release.ReleaseNotes;
            txtNotes.Location = new Point(20, 115);
            txtNotes.Size = new Size(380, 120);
            f.Controls.Add(txtNotes);

            ModernButton btnUpdate = new ModernButton();
            btnUpdate.Text = "Cập nhật ngay ⚡";
            btnUpdate.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnUpdate.Location = new Point(20, 255);
            btnUpdate.Size = new Size(180, 38);
            btnUpdate.NormalColor = Color.FromArgb(16, 185, 129); // Emerald Green
            btnUpdate.HoverColor = Color.FromArgb(5, 150, 105);
            btnUpdate.ForeColor = Color.White;
            btnUpdate.CornerRadius = 6;
            btnUpdate.Click += (s, e) => {
                f.DialogResult = DialogResult.Yes;
                f.Close();
            };
            f.Controls.Add(btnUpdate);

            ModernButton btnCancel = new ModernButton();
            btnCancel.Text = "Để sau ⏳";
            btnCancel.Font = new Font("Segoe UI", 9.5f);
            btnCancel.Location = new Point(220, 255);
            btnCancel.Size = new Size(180, 38);
            btnCancel.NormalColor = Color.White;
            btnCancel.HoverColor = Color.FromArgb(243, 244, 246);
            btnCancel.ForeColor = Color.FromArgb(107, 114, 128);
            btnCancel.BorderColor = colorBorder;
            btnCancel.CornerRadius = 6;
            btnCancel.Click += (s, e) => {
                f.DialogResult = DialogResult.No;
                f.Close();
            };
            f.Controls.Add(btnCancel);

            if (f.ShowDialog(this) == DialogResult.Yes)
            {
                StartSelfUpdateProcess(release);
            }
        }

        private void StartSelfUpdateProcess(GithubReleaseInfo release)
        {
            Form progressDlg = new Form();
            progressDlg.FormBorderStyle = FormBorderStyle.None;
            progressDlg.Size = new Size(350, 120);
            progressDlg.StartPosition = FormStartPosition.CenterParent;
            progressDlg.BackColor = colorBg;
            progressDlg.ShowInTaskbar = false;

            progressDlg.Paint += (s, e) => {
                using (Pen pen = new Pen(colorBorder, 2f))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, progressDlg.Width - 2, progressDlg.Height - 2);
                }
            };
            ApplyRoundedRegion(progressDlg, 10);

            Label lblProgress = new Label();
            lblProgress.Text = "🔄 Đang tải bản cập nhật mới...";
            lblProgress.Font = new Font("Segoe UI Semibold", 10f);
            lblProgress.ForeColor = colorText;
            lblProgress.Location = new Point(20, 20);
            lblProgress.Size = new Size(310, 20);
            progressDlg.Controls.Add(lblProgress);

            ProgressBar pb = new ProgressBar();
            pb.Location = new Point(20, 50);
            pb.Size = new Size(310, 20);
            pb.Style = ProgressBarStyle.Marquee;
            progressDlg.Controls.Add(pb);

            System.Threading.Tasks.Task.Run(() => {
                try
                {
                    string tempFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RBWStack_update.exe");
                    
                    using (var wc = new System.Net.WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        wc.DownloadFile(release.DownloadUrl, tempFile);

                        if (!string.IsNullOrEmpty(release.BridgeDownloadUrl))
                        {
                            string bridgePath = ConfigHelper.GetDataFilePath("bridge.php");
                            wc.DownloadFile(release.BridgeDownloadUrl, bridgePath);
                        }
                    }

                    if (File.Exists(tempFile))
                    {
                        string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.bat");
                        string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                        
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        sb.AppendLine("@echo off");
                        sb.AppendLine("timeout /t 1 /nobreak > nul");
                        sb.AppendLine(string.Format("del /f /q \"{0}\"", currentExe));
                        sb.AppendLine(string.Format("copy /y \"{0}\" \"{1}\"", tempFile, currentExe));
                        sb.AppendLine(string.Format("del /f /q \"{0}\"", tempFile));
                        sb.AppendLine(string.Format("start \"\" \"{0}\"", currentExe));
                        sb.AppendLine("del /f /q \"%~f0\"");
                        
                        File.WriteAllText(batPath, sb.ToString());

                        ProcessStartInfo psi = new ProcessStartInfo(batPath);
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        psi.WindowStyle = ProcessWindowStyle.Hidden;
                        
                        Process.Start(psi);
                        
                        this.BeginInvoke((MethodInvoker)delegate {
                            progressDlg.Close();
                            isRealExit = true;
                            Application.Exit();
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate {
                        progressDlg.Close();
                        MessageBox.Show("Lỗi tải bản cập nhật: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });

            progressDlg.ShowDialog(this);
        }

        public static string GetActiveDocumentRoot(string fallbackWwwDir)
        {
            string rootProj = LoadRootProjectConfig();
            if (!string.IsNullOrEmpty(rootProj))
            {
                string sitesParent = GetSitesParentDirectory();
                string projectDir = sitesParent.StartsWith(fallbackWwwDir, StringComparison.OrdinalIgnoreCase)
                    ? Path.Combine(fallbackWwwDir, rootProj)
                    : Path.Combine(sitesParent, rootProj);
                if (Directory.Exists(projectDir))
                {
                    return projectDir.Replace("\\", "/");
                }
            }
            return fallbackWwwDir.Replace("\\", "/");
        }

        public static void EnsureSslCertificate()
        {
            try
            {
                string sslDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ssl");
                string crtFile = Path.Combine(sslDir, "localhost.crt");
                string keyFile = Path.Combine(sslDir, "localhost.key");

                if (!Directory.Exists(sslDir)) Directory.CreateDirectory(sslDir);

                if (!File.Exists(crtFile) || !File.Exists(keyFile))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string apacheBinRoot = Path.Combine(baseDir, @"bin\apache");
                    string opensslExe = "";
                    string opensslCnf = "";
                    if (Directory.Exists(apacheBinRoot))
                    {
                        string[] apacheDirs = Directory.GetDirectories(apacheBinRoot);
                        if (apacheDirs.Length > 0)
                        {
                            Array.Sort(apacheDirs);
                            string newestApache = apacheDirs[apacheDirs.Length - 1];
                            opensslExe = Path.Combine(newestApache, @"bin\openssl.exe");
                            opensslCnf = Path.Combine(newestApache, @"conf\openssl.cnf");
                        }
                    }

                    if (File.Exists(opensslExe) && File.Exists(opensslCnf))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = opensslExe;
                        psi.Arguments = string.Format(
                            "req -x509 -nodes -days 3650 -newkey rsa:2048 -keyout \"{0}\" -out \"{1}\" -subj \"/CN=localhost\" -config \"{2}\" -addext \"subjectAltName = DNS:localhost, IP:127.0.0.1\"",
                            keyFile, crtFile, opensslCnf
                        );
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process p = Process.Start(psi);
                        p.WaitForExit();
                    }
                }

                // Automatically trust the certificate in Windows Certificate Store
                if (File.Exists(crtFile))
                {
                    try
                    {
                        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(crtFile);
                        bool alreadyTrusted = false;

                        // Check if already in CurrentUser Root store by thumbprint
                        using (var store = new System.Security.Cryptography.X509Certificates.X509Store(
                            System.Security.Cryptography.X509Certificates.StoreName.Root, 
                            System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser))
                        {
                            store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
                            var found = store.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, cert.Thumbprint, false);
                            if (found.Count > 0)
                            {
                                alreadyTrusted = true;
                            }
                        }

                        if (!alreadyTrusted)
                        {
                            // Try CurrentUser (does not require administrator rights)
                            using (var store = new System.Security.Cryptography.X509Certificates.X509Store(
                                System.Security.Cryptography.X509Certificates.StoreName.Root, 
                                System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser))
                            {
                                store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite);
                                var oldCerts = store.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindBySubjectName, "localhost", false);
                                foreach (var oldCert in oldCerts)
                                {
                                    try { store.Remove(oldCert); } catch { }
                                }
                                store.Add(cert);
                            }

                            // Try LocalMachine (requires admin, but good if running as admin)
                            try
                            {
                                using (var store = new System.Security.Cryptography.X509Certificates.X509Store(
                                    System.Security.Cryptography.X509Certificates.StoreName.Root, 
                                    System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine))
                                {
                                    store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite);
                                    var oldCerts = store.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindBySubjectName, "localhost", false);
                                    foreach (var oldCert in oldCerts)
                                    {
                                        try { store.Remove(oldCert); } catch { }
                                    }
                                    store.Add(cert);
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void EnsureSslCertificateForDomain(string domain)
        {
            try
            {
                string sslDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ssl");
                string domainsDir = Path.Combine(sslDir, "domains");
                if (!Directory.Exists(domainsDir)) Directory.CreateDirectory(domainsDir);

                string crtFile = Path.Combine(domainsDir, domain + ".crt");
                string keyFile = Path.Combine(domainsDir, domain + ".key");

                if (!File.Exists(crtFile) || !File.Exists(keyFile))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string apacheBinRoot = Path.Combine(baseDir, @"bin\apache");
                    string opensslExe = "";
                    string opensslCnf = "";
                    if (Directory.Exists(apacheBinRoot))
                    {
                        string[] apacheDirs = Directory.GetDirectories(apacheBinRoot);
                        if (apacheDirs.Length > 0)
                        {
                            Array.Sort(apacheDirs);
                            string newestApache = apacheDirs[apacheDirs.Length - 1];
                            opensslExe = Path.Combine(newestApache, @"bin\openssl.exe");
                            opensslCnf = Path.Combine(newestApache, @"conf\openssl.cnf");
                        }
                    }

                    if (File.Exists(opensslExe) && File.Exists(opensslCnf))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = opensslExe;
                        psi.Arguments = string.Format(
                            "req -x509 -nodes -days 3650 -newkey rsa:2048 -keyout \"{0}\" -out \"{1}\" -subj \"/CN={3}\" -config \"{2}\" -addext \"subjectAltName = DNS:{3}\"",
                            keyFile, crtFile, opensslCnf, domain
                        );
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process p = Process.Start(psi);
                        p.WaitForExit();

                        if (File.Exists(crtFile))
                        {
                            try
                            {
                                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(crtFile);
                                using (var store = new System.Security.Cryptography.X509Certificates.X509Store(
                                    System.Security.Cryptography.X509Certificates.StoreName.Root, 
                                    System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser))
                                {
                                    store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite);
                                    var oldCerts = store.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindBySubjectName, domain, false);
                                    foreach (var oldCert in oldCerts)
                                    {
                                        try { store.Remove(oldCert); } catch { }
                                    }
                                    store.Add(cert);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public static Dictionary<string, string> LoadTunnelsConfig()
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            try
            {
                string path = ConfigHelper.GetDataFilePath("tunnels.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    MatchCollection matches = Regex.Matches(json, @"""([^""]+)""\s*:\s*""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        config[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                }
            }
            catch { }
            return config;
        }

        public static void SaveTunnelsConfig(Dictionary<string, string> config)
        {
            try
            {
                string path = ConfigHelper.GetDataFilePath("tunnels.json");
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                int count = 0;
                foreach (var kvp in config)
                {
                    sb.AppendFormat("  \"{0}\": \"{1}\"", kvp.Key, kvp.Value);
                    if (++count < config.Count) sb.AppendLine(",");
                    else sb.AppendLine("");
                }
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }

        public static string SanitizeSubdomain(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string output = input.ToLower();
            output = Regex.Replace(output, @"[^a-z0-9\-]", "-");
            output = Regex.Replace(output, @"-+", "-");
            return output.Trim('-');
        }

        public static int GetPhpPortForVersion(string phpVersionDir)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string phpBinRoot = Path.Combine(baseDir, @"bin\php");
            if (Directory.Exists(phpBinRoot))
            {
                string[] phpDirs = Directory.GetDirectories(phpBinRoot, "php*");
                Array.Sort(phpDirs);
                for (int i = 0; i < phpDirs.Length; i++)
                {
                    if (Path.GetFileName(phpDirs[i]).Equals(phpVersionDir, StringComparison.OrdinalIgnoreCase))
                    {
                        return 9001 + i;
                    }
                }
            }
            return 9000; // Fallback
        }

        private void RestartWebServicesAndPhp()
        {
            bool isWebRunning = IsProcessRunning(selectedWebServerType == "Apache" ? "httpd" : "nginx");
            bool isPhpRunning = IsProcessRunning("php-cgi");

            if (isWebRunning)
            {
                WebStop_Click(null, null);
                System.Threading.Thread.Sleep(300);
            }
            if (isPhpRunning)
            {
                PhpStop_Click(null, null);
                System.Threading.Thread.Sleep(300);
            }

            try
            {
                string currentPort = txtWebPort != null ? txtWebPort.Text.Trim() : "80";
                if (selectedWebServerType == "Apache")
                {
                    string relativeApacheDir = Path.GetDirectoryName(Path.GetDirectoryName(pathApacheExe));
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (relativeApacheDir.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        relativeApacheDir = relativeApacheDir.Substring(baseDir.Length).TrimStart('\\', '/');
                    }
                    DownloadCenterForm.ConfigureApache(relativeApacheDir, currentPort, Path.GetDirectoryName(pathPhpExe));
                }
                else
                {
                    string relativeNginxDir = Path.GetDirectoryName(pathNginxExe);
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (relativeNginxDir.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        relativeNginxDir = relativeNginxDir.Substring(baseDir.Length).TrimStart('\\', '/');
                    }
                    DownloadCenterForm.ConfigureNginx(relativeNginxDir, currentPort, Path.GetDirectoryName(pathPhpExe));
                }
            }
            catch { }

            if (isPhpRunning)
            {
                PhpStart_Click(null, null);
                System.Threading.Thread.Sleep(300);
            }
            if (isWebRunning)
            {
                WebStart_Click(null, null);
            }
        }

        private void RenderSitesList()
        {
            if (pnlTabSites == null) return;

            Panel pnlSitesContainer = null;
            foreach (Control c in pnlTabSites.Controls)
            {
                if (c.Name == "pnlSitesContainer")
                {
                    pnlSitesContainer = c as Panel;
                    break;
                }
            }

            if (pnlSitesContainer == null)
            {
                Label lblSitesTitle = new Label();
                lblSitesTitle.Text = "QUẢN LÝ DỰ ÁN (SITES)";
                lblSitesTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                lblSitesTitle.ForeColor = colorText;
                lblSitesTitle.Location = new Point(20, 15);
                lblSitesTitle.AutoSize = true;
                pnlTabSites.Controls.Add(lblSitesTitle);

                Label lblSitesDir = new Label();
                lblSitesDir.Name = "lblSitesDir";
                lblSitesDir.Font = new Font("Segoe UI Semibold", 8.5f);
                lblSitesDir.ForeColor = Color.FromArgb(59, 130, 246);
                lblSitesDir.Location = new Point(220, 18);
                lblSitesDir.Size = new Size(320, 20);
                lblSitesDir.AutoEllipsis = true;
                pnlTabSites.Controls.Add(lblSitesDir);

                ModernButton btnChooseDir = new ModernButton();
                btnChooseDir.Text = "Chọn thư mục...";
                btnChooseDir.Font = new Font("Segoe UI", 8.5f);
                btnChooseDir.NormalColor = Color.White;
                btnChooseDir.BorderColor = colorBorder;
                btnChooseDir.ForeColor = Color.FromArgb(75, 85, 99);
                btnChooseDir.Location = new Point(550, 12);
                btnChooseDir.Size = new Size(130, 28);
                btnChooseDir.Click += (s, e) => {
                    using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    {
                        fbd.SelectedPath = GetSitesParentDirectory();
                        fbd.Description = "Chọn thư mục cha chứa các dự án PHP của bạn (khuyến nghị nằm trong thư mục www)";
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                string pathFile = ConfigHelper.GetDataFilePath("sites_root.txt");
                                File.WriteAllText(pathFile, fbd.SelectedPath);
                                RenderSitesList();
                                RestartWebServicesAndPhp();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Không thể lưu cấu hình thư mục: " + ex.Message);
                            }
                        }
                    }
                };
                pnlTabSites.Controls.Add(btnChooseDir);

                Label lblSitesDesc = new Label();
                lblSitesDesc.Text = "Tất cả thư mục dự án PHP trong thư mục được chọn. Bạn có thể chọn phiên bản PHP riêng biệt cho từng dự án.";
                lblSitesDesc.Font = new Font("Segoe UI Italic", 8.5f);
                lblSitesDesc.ForeColor = colorTextDim;
                lblSitesDesc.Location = new Point(20, 40);
                lblSitesDesc.Size = new Size(680, 20);
                pnlTabSites.Controls.Add(lblSitesDesc);

                pnlSitesContainer = new Panel();
                pnlSitesContainer.Name = "pnlSitesContainer";
                pnlSitesContainer.Size = new Size(720, 480);
                pnlSitesContainer.Location = new Point(20, 65);
                pnlSitesContainer.BackColor = Color.Transparent;
                pnlSitesContainer.AutoScroll = true;
                pnlTabSites.Controls.Add(pnlSitesContainer);
            }

            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            string currentSitesDir = GetSitesParentDirectory();
            if (!Directory.Exists(currentSitesDir)) Directory.CreateDirectory(currentSitesDir);

            // Update label path text
            foreach (Control c in pnlTabSites.Controls)
            {
                if (c.Name == "lblSitesDir")
                {
                    string displayPath = currentSitesDir;
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (displayPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        displayPath = displayPath.Substring(baseDir.Length).TrimStart('\\', '/');
                    }
                    c.Text = "Đang quét: " + displayPath;
                    break;
                }
            }

            pnlSitesContainer.SuspendLayout();
            pnlSitesContainer.Controls.Clear();

            string[] subDirs = Directory.GetDirectories(currentSitesDir);
            Dictionary<string, string> sitesConfig = LoadSitesConfig();
            string activeRootProj = LoadRootProjectConfig();

            List<string> phpVersions = new List<string>();
            phpVersions.Add("Mặc định (Default)");
            string phpBinRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\php");
            if (Directory.Exists(phpBinRoot))
            {
                string[] phpDirs = Directory.GetDirectories(phpBinRoot, "php*");
                foreach (string dir in phpDirs)
                {
                    phpVersions.Add(Path.GetFileName(dir));
                }
            }

            if (subDirs.Length == 0)
            {
                Label lblEmpty = new Label();
                lblEmpty.Text = "Chưa có dự án nào trong thư mục đang chọn.\r\nHãy tạo một thư mục con hoặc chọn thư mục khác!";
                lblEmpty.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                lblEmpty.ForeColor = colorTextDim;
                lblEmpty.Location = new Point(20, 40);
                lblEmpty.Size = new Size(600, 50);
                pnlSitesContainer.Controls.Add(lblEmpty);
            }
            else
            {
                int currentY = 10;
                foreach (string dir in subDirs)
                {
                    string folderName = Path.GetFileName(dir);

                    // Compute relative site path from www folder
                    string relativeSitePath = "";
                    if (dir.StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase))
                    {
                        relativeSitePath = dir.Substring(wwwDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    }
                    else
                    {
                        relativeSitePath = folderName;
                    }
                    bool isRoot = activeRootProj.Equals(relativeSitePath, StringComparison.OrdinalIgnoreCase);

                    Panel pnlCard = new Panel();
                    pnlCard.Size = new Size(700, 48);
                    pnlCard.Location = new Point(10, currentY);
                    pnlCard.BackColor = Color.White;
                    pnlCard.Paint += DrawCardBorder;
                    pnlSitesContainer.Controls.Add(pnlCard);
                    ApplyRoundedRegion(pnlCard, 6);

                    // Load tunnels configuration
                    Dictionary<string, string> tunnelsConfig = LoadTunnelsConfig();
                    string currentSubdomain = SanitizeSubdomain(folderName);
                    bool isTunnelActive = false;

                    string mappedTunnel = "";
                    if (tunnelsConfig.TryGetValue(relativeSitePath, out mappedTunnel))
                    {
                        string[] parts = mappedTunnel.Split('|');
                        if (parts.Length >= 3)
                        {
                            if (!string.IsNullOrEmpty(parts[0])) currentSubdomain = parts[0];
                            isTunnelActive = (parts[2] == "1");
                        }
                    }
                    else
                    {
                        tunnelsConfig[relativeSitePath] = string.Format("{0}|https|0", currentSubdomain);
                        SaveTunnelsConfig(tunnelsConfig);
                    }

                    // Dynamically calculate control positions based on tunnel state to maximize project name space
                    int btnDeployX = 658;
                    int btnUrlX, btnOpenX, btnTunnelX, btnTunnelUrlX, cbPhpX, lblNameWidth, btnSetRootX, btnVHostX;
                    if (isTunnelActive)
                    {
                        btnTunnelUrlX = 622;
                        btnTunnelX = 585;
                        btnOpenX = 548;
                        btnUrlX = 511;
                        btnSetRootX = 474;
                        btnVHostX = 437;
                        cbPhpX = 303;
                        lblNameWidth = 283;
                    }
                    else
                    {
                        btnTunnelUrlX = 0; // Hidden
                        btnTunnelX = 622;
                        btnOpenX = 585;
                        btnUrlX = 548;
                        btnSetRootX = 511;
                        btnVHostX = 474;
                        cbPhpX = 341;
                        lblNameWidth = 320;
                    }

                    ToolTip toolTip = new ToolTip();

                    Label lblName = new Label();
                    lblName.Text = relativeSitePath;
                    lblName.ForeColor = colorText;
                    lblName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    lblName.Location = new Point(12, 14);
                    lblName.Size = new Size(lblNameWidth, 20);
                    lblName.AutoEllipsis = true;
                    pnlCard.Controls.Add(lblName);

                    string webPort = GetWebPort();
                    bool vhostEnabled;
                    string vhostDomain;
                    bool vhostUseSsl;
                    GetVHostConfig(relativeSitePath, folderName, out vhostEnabled, out vhostDomain, out vhostUseSsl);

                    string siteUrl = "";
                    if (vhostEnabled)
                    {
                        siteUrl = "https://" + vhostDomain;
                    }
                    else
                    {
                        siteUrl = isRoot
                            ? "https://" + "localhost" + (webPort == "80" ? "" : ":" + webPort)
                            : "https://" + "localhost" + (webPort == "80" ? "" : ":" + webPort) + "/" + relativeSitePath;
                    }

                    string captureRelativePath = relativeSitePath;

                    ModernButton btnUrl = new ModernButton();
                    btnUrl.Text = "🔗";
                    btnUrl.Font = new Font("Segoe UI", 10f);
                    btnUrl.NormalColor = Color.White;
                    btnUrl.BorderColor = colorBorder;
                    btnUrl.ForeColor = Color.FromArgb(59, 130, 246);
                    btnUrl.Location = new Point(btnUrlX, 10);
                    btnUrl.Size = new Size(32, 28);
                    btnUrl.Click += (s, e) => {
                        try { Process.Start(siteUrl); } catch { }
                    };
                    pnlCard.Controls.Add(btnUrl);

                    ContextMenuStrip menuUrl = new ContextMenuStrip();
                    menuUrl.Items.Add("Mở trang web", null, (s, e) => {
                        try { Process.Start(siteUrl); } catch { }
                    });
                    menuUrl.Items.Add("Sao chép liên kết", null, (s, e) => {
                        try { Clipboard.SetText(siteUrl); } catch { }
                    });
                    btnUrl.ContextMenuStrip = menuUrl;
                    btnUrl.MouseUp += (s, me) => {
                        if (me.Button == MouseButtons.Right) { menuUrl.Show(btnUrl, me.Location); }
                    };

                    ModernButton btnVHost = new ModernButton();
                    btnVHost.Text = "🌐";
                    btnVHost.Font = new Font("Segoe UI", 10f);
                    btnVHost.NormalColor = Color.White;
                    btnVHost.BorderColor = colorBorder;
                    if (vhostEnabled)
                    {
                        btnVHost.ForeColor = Color.FromArgb(16, 185, 129); // Green
                        toolTip.SetToolTip(btnVHost, "Host ảo đang BẬT: " + siteUrl + ". Click để cấu hình.");
                    }
                    else
                    {
                        btnVHost.ForeColor = Color.FromArgb(107, 114, 128); // Gray
                        toolTip.SetToolTip(btnVHost, "Host ảo đang TẮT. Click để cấu hình.");
                    }
                    btnVHost.Location = new Point(btnVHostX, 10);
                    btnVHost.Size = new Size(32, 28);
                    btnVHost.Click += (s, e) => {
                        var vhostForm = new VirtualHostForm(relativeSitePath, folderName);
                        if (vhostForm.ShowDialog(this) == DialogResult.OK)
                        {
                            RestartWebServicesAndPhp();
                            RenderSitesList();
                        }
                    };
                    pnlCard.Controls.Add(btnVHost);

                    ContextMenuStrip menuVHost = new ContextMenuStrip();
                    menuVHost.Items.Add("Cấu hình Host ảo...", null, (s, e) => {
                        var vhostForm = new VirtualHostForm(captureRelativePath, folderName);
                        if (vhostForm.ShowDialog(this) == DialogResult.OK)
                        {
                            RestartWebServicesAndPhp();
                            RenderSitesList();
                        }
                    });
                    string toggleText = vhostEnabled ? "Tắt nhanh Host ảo" : "Bật nhanh Host ảo";
                    menuVHost.Items.Add(toggleText, null, (s, e) => {
                        SaveVHostConfig(captureRelativePath, !vhostEnabled, folderName + GetVHostSuffix(), vhostUseSsl);
                        RestartWebServicesAndPhp();
                        RenderSitesList();
                    });
                    menuVHost.Items.Add("Mở file hosts", null, (s, e) => {
                        try { Process.Start("notepad.exe", @"C:\Windows\System32\drivers\etc\hosts"); } catch { }
                    });
                    btnVHost.ContextMenuStrip = menuVHost;
                    btnVHost.MouseUp += (s, me) => {
                        if (me.Button == MouseButtons.Right) { menuVHost.Show(btnVHost, me.Location); }
                    };

                    ModernButton btnOpen = new ModernButton();
                    btnOpen.Text = "📂";
                    btnOpen.Font = new Font("Segoe UI", 10f);
                    btnOpen.NormalColor = Color.White;
                    btnOpen.BorderColor = colorBorder;
                    btnOpen.ForeColor = Color.FromArgb(245, 158, 11);
                    btnOpen.Location = new Point(btnOpenX, 10);
                    btnOpen.Size = new Size(32, 28);
                    btnOpen.Click += (s, e) => {
                        try { Process.Start("explorer.exe", dir); } catch { }
                    };
                    pnlCard.Controls.Add(btnOpen);

                    ContextMenuStrip menuOpen = new ContextMenuStrip();
                    menuOpen.Items.Add("Mở thư mục dự án", null, (s, e) => {
                        try { Process.Start("explorer.exe", dir); } catch { }
                    });
                    menuOpen.Items.Add("Mở bằng VS Code", null, (s, e) => {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c code .")
                            {
                                WorkingDirectory = dir,
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            Process.Start(psi);
                        }
                        catch { }
                    });
                    menuOpen.Items.Add("Mở bằng Command Prompt", null, (s, e) => {
                        try { Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = dir }); } catch { }
                    });
                    menuOpen.Items.Add("Mở bằng PowerShell", null, (s, e) => {
                        try { Process.Start(new ProcessStartInfo("powershell.exe") { WorkingDirectory = dir }); } catch { }
                    });
                    menuOpen.Items.Add(new ToolStripSeparator());
                    menuOpen.Items.Add("Cài đặt Fonts...", null, (s, e) => {
                        var fontForm = new FontInstallerForm(dir, relativeSitePath);
                        fontForm.ShowDialog(this);
                    });
                    btnOpen.ContextMenuStrip = menuOpen;
                    btnOpen.MouseUp += (s, me) => {
                        if (me.Button == MouseButtons.Right) { menuOpen.Show(btnOpen, me.Location); }
                    };

                    toolTip.SetToolTip(btnUrl, siteUrl);
                    toolTip.SetToolTip(btnOpen, "Mở thư mục dự án");

                    ModernButton btnSetRoot = new ModernButton();
                    btnSetRoot.Size = new Size(32, 28);
                    btnSetRoot.Location = new Point(btnSetRootX, 10);
                    btnSetRoot.Font = new Font("Segoe UI", 10f);
                    btnSetRoot.CornerRadius = 4;
                    btnSetRoot.BorderColor = colorBorder;

                    if (isRoot)
                    {
                        btnSetRoot.Text = "🏠";
                        btnSetRoot.NormalColor = Color.FromArgb(16, 185, 129); // Green background
                        btnSetRoot.HoverColor = Color.FromArgb(5, 150, 105);
                        btnSetRoot.ForeColor = Color.White;
                        toolTip.SetToolTip(btnSetRoot, "Dự án đang chạy tại https://localhost (Root). Click để hủy set root.");
                    }
                    else
                    {
                        btnSetRoot.Text = "🏠";
                        btnSetRoot.NormalColor = Color.White;
                        btnSetRoot.HoverColor = Color.FromArgb(243, 244, 246);
                        btnSetRoot.ForeColor = Color.FromArgb(107, 114, 128); // Gray color
                        toolTip.SetToolTip(btnSetRoot, "Set dự án này chạy trực tiếp tại https://localhost (Root)");
                    }

                    btnSetRoot.Click += (s, e) => {
                        string currentRoot = LoadRootProjectConfig();
                        if (currentRoot.Equals(captureRelativePath, StringComparison.OrdinalIgnoreCase))
                        {
                            SaveRootProjectConfig("");
                        }
                        else
                        {
                            SaveRootProjectConfig(captureRelativePath);
                        }
                        RestartWebServicesAndPhp();
                        RenderSitesList();
                    };
                    pnlCard.Controls.Add(btnSetRoot);

                    ContextMenuStrip menuRoot = new ContextMenuStrip();
                    if (isRoot)
                    {
                        menuRoot.Items.Add("Hủy đặt làm Root Project", null, (s, e) => {
                            SaveRootProjectConfig("");
                            RestartWebServicesAndPhp();
                            RenderSitesList();
                        });
                    }
                    else
                    {
                        menuRoot.Items.Add("Đặt làm Root Project", null, (s, e) => {
                            SaveRootProjectConfig(captureRelativePath);
                            RestartWebServicesAndPhp();
                            RenderSitesList();
                        });
                    }
                    btnSetRoot.ContextMenuStrip = menuRoot;
                    btnSetRoot.MouseUp += (s, me) => {
                        if (me.Button == MouseButtons.Right) { menuRoot.Show(btnSetRoot, me.Location); }
                    };

                    ComboBox cbPhp = new NoScrollComboBox();
                    cbPhp.BackColor = Color.White;
                    cbPhp.ForeColor = Color.FromArgb(55, 65, 81);
                    cbPhp.FlatStyle = FlatStyle.Flat;
                    cbPhp.DropDownStyle = ComboBoxStyle.DropDownList;
                    cbPhp.Size = new Size(125, 25);
                    cbPhp.Location = new Point(cbPhpX, 11);

                    foreach (var ver in phpVersions)
                    {
                        cbPhp.Items.Add(ver);
                    }

                    string mappedVer = "";
                    if (sitesConfig.TryGetValue(relativeSitePath, out mappedVer))
                    {
                        int idx = cbPhp.FindStringExact(mappedVer);
                        cbPhp.SelectedIndex = (idx >= 0) ? idx : 0;
                    }
                    else
                    {
                        cbPhp.SelectedIndex = 0;
                    }

                    string captureSitePath = relativeSitePath;
                    cbPhp.SelectedIndexChanged += (s, e) => {
                        string selected = cbPhp.SelectedItem.ToString();
                        Dictionary<string, string> currentConfig = LoadSitesConfig();
                        if (selected == "Mặc định (Default)")
                        {
                            currentConfig.Remove(captureSitePath);
                        }
                        else
                        {
                            currentConfig[captureSitePath] = selected;
                        }
                        SaveSitesConfig(currentConfig);
                        RestartWebServicesAndPhp();
                    };
                    pnlCard.Controls.Add(cbPhp);

                    ModernButton btnTunnel = new ModernButton();
                    btnTunnel.Size = new Size(32, 28);
                    btnTunnel.Location = new Point(btnTunnelX, 10);
                    btnTunnel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                    btnTunnel.CornerRadius = 4;
                    btnTunnel.BorderColor = colorBorder;

                    ModernButton btnTunnelUrl = new ModernButton();
                    btnTunnelUrl.Size = new Size(32, 28);
                    btnTunnelUrl.Location = new Point(btnTunnelUrlX, 10);
                    btnTunnelUrl.Text = "🔗";
                    btnTunnelUrl.Font = new Font("Segoe UI", 10f);
                    btnTunnelUrl.NormalColor = Color.White;
                    btnTunnelUrl.BorderColor = colorBorder;
                    btnTunnelUrl.ForeColor = Color.FromArgb(16, 185, 129);
                    btnTunnelUrl.Visible = false;

                    if (isTunnelActive)
                    {
                        btnTunnel.Text = "⏹";
                        btnTunnel.NormalColor = Color.FromArgb(239, 68, 68);
                        btnTunnel.HoverColor = Color.FromArgb(220, 38, 38);
                        btnTunnel.ForeColor = Color.White;

                        string tunnelLink = string.Format("https://{0}.trycloudflare.com", currentSubdomain);
                        btnTunnelUrl.Visible = true;
                        btnTunnelUrl.Click += (s, e) => {
                            try { Process.Start(tunnelLink); } catch { }
                        };
                        toolTip.SetToolTip(btnTunnel, "Dừng Cloudflare Tunnel");
                        toolTip.SetToolTip(btnTunnelUrl, "Mở Cloudflare Tunnel link: " + tunnelLink);
                    }
                    else
                    {
                        btnTunnel.Text = "☁";
                        btnTunnel.NormalColor = Color.White;
                        btnTunnel.HoverColor = Color.FromArgb(243, 244, 246);
                        btnTunnel.ForeColor = Color.FromArgb(59, 130, 246);
                        btnTunnelUrl.Visible = false;
                        toolTip.SetToolTip(btnTunnel, "Kích hoạt Cloudflare Tunnel");
                    }

                    btnTunnel.Click += (s, e) => {
                        Dictionary<string, string> currTunnels = LoadTunnelsConfig();
                        string mapped = "";
                        bool isActive = false;
                        if (currTunnels.TryGetValue(captureSitePath, out mapped))
                        {
                            string[] p = mapped.Split('|');
                            if (p.Length >= 3) isActive = (p[2] == "1");
                        }

                        if (isActive)
                        {
                            StopTunnelProcess(captureSitePath);
                            currTunnels[captureSitePath] = string.Format("{0}|https|0", currentSubdomain);
                            SaveTunnelsConfig(currTunnels);
                            RestartWebServicesAndPhp();
                            RenderSitesList();
                        }
                        else
                        {
                            btnTunnel.Text = "⏳";
                            btnTunnel.Enabled = false;
                            StartTunnelProcess(captureSitePath);
                        }
                    };
                    pnlCard.Controls.Add(btnTunnel);
                    pnlCard.Controls.Add(btnTunnelUrl);

                    ContextMenuStrip menuTunnel = new ContextMenuStrip();
                    string tunnelToggle = isTunnelActive ? "Dừng Cloudflare Tunnel" : "Kích hoạt Cloudflare Tunnel";
                    menuTunnel.Items.Add(tunnelToggle, null, (s, e) => {
                        Dictionary<string, string> currTunnels = LoadTunnelsConfig();
                        string mapped = "";
                        bool isActive = false;
                        if (currTunnels.TryGetValue(captureSitePath, out mapped))
                        {
                            string[] p = mapped.Split('|');
                            if (p.Length >= 3) isActive = (p[2] == "1");
                        }

                        if (isActive)
                        {
                            StopTunnelProcess(captureSitePath);
                            currTunnels[captureSitePath] = string.Format("{0}|https|0", currentSubdomain);
                            SaveTunnelsConfig(currTunnels);
                            RestartWebServicesAndPhp();
                            RenderSitesList();
                        }
                        else
                        {
                            btnTunnel.Text = "⏳";
                            btnTunnel.Enabled = false;
                            StartTunnelProcess(captureSitePath);
                        }
                    });
                    if (isTunnelActive)
                    {
                        string tunnelLink = string.Format("https://{0}.trycloudflare.com", currentSubdomain);
                        menuTunnel.Items.Add("Mở liên kết Tunnel", null, (s, e) => {
                            try { Process.Start(tunnelLink); } catch { }
                        });
                        menuTunnel.Items.Add("Sao chép liên kết Tunnel", null, (s, e) => {
                            try { Clipboard.SetText(tunnelLink); } catch { }
                        });
                    }
                    btnTunnel.ContextMenuStrip = menuTunnel;
                    btnTunnel.MouseUp += (s, me) => {
                        if (me.Button == MouseButtons.Right) { menuTunnel.Show(btnTunnel, me.Location); }
                    };

                    // --- Nut Deploy Demo ---
                    string captureDir2 = dir;
                    string captureSiteDeploy2 = captureSitePath;
                    string deployStatusKey = captureDir2;
                    bool alreadyDeployed = DeployDemoForm.IsDeployed(deployStatusKey);

                    ModernButton btnDeploy = new ModernButton();
                    btnDeploy.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    btnDeploy.CornerRadius = 4;
                    btnDeploy.Size = new Size(32, 28);
                    btnDeploy.Location = new Point(btnDeployX, 10);
                    toolTip.SetToolTip(btnDeploy, alreadyDeployed ? "Đã deploy - click để mở khóa deploy lại" : "Deploy Demo lên hosting");

                    if (alreadyDeployed)
                    {
                        btnDeploy.Text = "⚡";
                        btnDeploy.NormalColor = Color.FromArgb(240, 253, 244);
                        btnDeploy.HoverColor = Color.FromArgb(220, 252, 231);
                        btnDeploy.BorderColor = Color.FromArgb(74, 222, 128);
                        btnDeploy.ForeColor = Color.FromArgb(21, 128, 61);
                    }
                    else
                    {
                        btnDeploy.Text = "⚡";
                        btnDeploy.NormalColor = Color.White;
                        btnDeploy.HoverColor = Color.FromArgb(245, 243, 255);
                        btnDeploy.BorderColor = colorBorder;
                        btnDeploy.ForeColor = Color.FromArgb(139, 92, 246);
                    }
                    btnDeploy.Click += (s, e) => {
                        var deployForm = new DeployDemoForm(captureDir2, captureSiteDeploy2);
                        deployForm.ShowDialog(this);
                        RenderSitesList(); // refresh card after deploy
                    };
                    pnlCard.Controls.Add(btnDeploy);

                    ContextMenuStrip menuDeploy = new ContextMenuStrip();
                    menuDeploy.Items.Add(alreadyDeployed ? "Mở khóa & Deploy lại..." : "Deploy Demo lên hosting...", null, (s, e) => {
                        var deployForm = new DeployDemoForm(captureDir2, captureSiteDeploy2);
                        deployForm.ShowDialog(this);
                        RenderSitesList();
                    });
                    menuDeploy.Items.Add("Mở phpMyAdmin", null, (s, e) => {
                        try { Process.Start("http://localhost/phpmyadmin"); } catch { }
                    });
                    menuDeploy.Items.Add(new ToolStripSeparator());
                    menuDeploy.Items.Add("Cài đặt Fonts...", null, (s, e) => {
                        var fontForm = new FontInstallerForm(captureDir2, captureSiteDeploy2);
                        fontForm.ShowDialog(this);
                    });
                    btnDeploy.ContextMenuStrip = menuDeploy;
                    btnDeploy.MouseUp += (s, me) => {
                        if (me.Button == MouseButtons.Right) { menuDeploy.Show(btnDeploy, me.Location); }
                    };

                    currentY += 56;
                }
            }

            pnlSitesContainer.ResumeLayout();
            pnlSitesContainer.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (restoreMessage != 0 && m.Msg == restoreMessage)
            {
                ShowMainForm();
            }
            base.WndProc(ref m);
        }

        // ── VIRTUAL HOST CONFIGURATION ──────────────────────────────
        public static string VHostsConfigPath
        {
            get { return ConfigHelper.GetDataFilePath("sites_vhosts.json"); }
        }

        public static Dictionary<string, string> LoadVHostsConfig()
        {
            if (!File.Exists(VHostsConfigPath)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                return DeployDemoForm.ParseJson(File.ReadAllText(VHostsConfigPath));
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void SaveVHostsConfig(Dictionary<string, string> dict)
        {
            try
            {
                File.WriteAllText(VHostsConfigPath, DeployDemoForm.SerializeJson(dict));
            }
            catch { }
        }

        public static bool IsAdminVHostMode()
        {
            try
            {
                var c = DeployDemoForm.LoadGlobalConfig();
                if (c.ContainsKey("vhost_mode") && !string.IsNullOrEmpty(c["vhost_mode"]))
                {
                    return c["vhost_mode"] == "admin";
                }
            }
            catch { }
            return true; // Default to admin
        }

        public static string GetVHostSuffix()
        {
            if (!IsAdminVHostMode()) return "localhost";
            var c = DeployDemoForm.LoadGlobalConfig();
            if (c.ContainsKey("vhost_suffix") && !string.IsNullOrEmpty(c["vhost_suffix"]))
            {
                return c["vhost_suffix"].Trim().TrimStart('.').ToLower();
            }
            return "local";
        }

        public static void GetVHostConfig(string sitePath, string folderName, out bool enabled, out string domain, out bool useSsl)
        {
            enabled = false;
            domain = folderName.ToLower() + "." + GetVHostSuffix();
            useSsl = true; // Always true to generate both HTTP + HTTPS automatically!

            var config = LoadVHostsConfig();
            string val;
            if (config.TryGetValue(sitePath, out val) && !string.IsNullOrEmpty(val))
            {
                string[] parts = val.Split('|');
                if (parts.Length >= 2)
                {
                    enabled = (parts[0] == "1");
                    domain = parts[1];
                    if (!IsAdminVHostMode())
                    {
                        int lastDot = domain.LastIndexOf('.');
                        if (lastDot >= 0)
                        {
                            domain = domain.Substring(0, lastDot) + ".localhost";
                        }
                        else
                        {
                            domain = domain + ".localhost";
                        }
                    }
                    if (parts.Length >= 3)
                    {
                        useSsl = (parts[2] == "1");
                    }
                }
            }
        }

        public static void SaveVHostConfig(string sitePath, bool enabled, string domain, bool useSsl)
        {
            var config = LoadVHostsConfig();
            config[sitePath] = string.Format("{0}|{1}|{2}", enabled ? "1" : "0", domain.Trim().ToLower(), useSsl ? "1" : "0");
            SaveVHostsConfig(config);
        }

        public static void RemoveHostsEntry(string hostname)
        {
            if (!IsAdminVHostMode()) return;
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (File.Exists(hostsPath))
                {
                    string[] lines = File.ReadAllLines(hostsPath);
                    List<string> newLines = new List<string>();
                    bool changed = false;
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\b" + System.Text.RegularExpressions.Regex.Escape(hostname) + @"\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            if (trimmed.Contains("127.0.0.1") && !trimmed.StartsWith("127.0.0.1") && !trimmed.StartsWith("#"))
                            {
                                int idx = trimmed.IndexOf("127.0.0.1");
                                string part1 = trimmed.Substring(0, idx).Trim();
                                newLines.Add(part1);
                                changed = true;
                                continue;
                            }
                            changed = true;
                            continue;
                        }
                        newLines.Add(line);
                    }
                    if (changed)
                    {
                        try
                        {
                            File.WriteAllLines(hostsPath, newLines.ToArray());
                        }
                        catch (UnauthorizedAccessException)
                        {
                            string tempFile = Path.Combine(Path.GetTempPath(), "hosts_temp");
                            File.WriteAllLines(tempFile, newLines.ToArray());
                            
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = "cmd.exe";
                            psi.Arguments = string.Format("/c copy /y \"{0}\" \"{1}\" && del /f /q \"{0}\"", tempFile, hostsPath);
                            psi.Verb = "runas";
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = true;
                            Process p = Process.Start(psi);
                            p.WaitForExit();
                        }
                    }
                }
            }
            catch { }
        }

        // ── MAIL SANDBOX PROPERTIES & CONTROLS ─────────────────────
        private Panel pnlTabMail;
        private ListBox lstMailInbox;
        private Panel pnlMailDetail;
        private Label lblMailFrom;
        private Label lblMailTo;
        private Label lblMailSubject;
        private Label lblMailDate;
        private WebBrowser webMailBody;
        private Label lblMailEmpty;

        private static TcpListener _smtpListener;
        private static Thread _smtpThread;
        private static bool _smtpRunning = false;

        public static int GetSmtpPort() { return 1025; }

        public static void StartSmtpServer()
        {
            if (_smtpRunning) return;
            try
            {
                int port = GetSmtpPort();
                _smtpListener = new TcpListener(IPAddress.Loopback, port);
                _smtpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _smtpListener.Start();
                _smtpRunning = true;
                _smtpThread = new Thread(SmtpServerLoop);
                _smtpThread.IsBackground = true;
                _smtpThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Smtp start error: " + ex.Message);
            }
        }

        public static void StopSmtpServer()
        {
            _smtpRunning = false;
            try
            {
                if (_smtpListener != null)
                {
                    _smtpListener.Stop();
                }
            }
            catch { }
        }

        private static void SmtpServerLoop()
        {
            while (_smtpRunning)
            {
                try
                {
                    TcpClient client = _smtpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleSmtpClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch
                {
                    break;
                }
            }
        }

        private static void HandleSmtpClient(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
                {
                    writer.WriteLine("220 RBWStack Mail Sandbox Server");
                    string from = "";
                    List<string> to = new List<string>();
                    StringBuilder dataBuilder = new StringBuilder();
                    bool inData = false;

                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        if (inData)
                        {
                            if (line == ".")
                            {
                                inData = false;
                                writer.WriteLine("250 OK");
                                ProcessReceivedEmail(from, to, dataBuilder.ToString());
                            }
                            else
                            {
                                if (line.StartsWith("."))
                                {
                                    line = line.Substring(1);
                                }
                                dataBuilder.AppendLine(line);
                            }
                        }
                        else
                        {
                            string cmd = line.Trim();
                            string cmdUpper = cmd.ToUpper();
                            if (cmdUpper.StartsWith("HELO") || cmdUpper.StartsWith("EHLO"))
                            {
                                writer.WriteLine("250-localhost Hello");
                                writer.WriteLine("250-SIZE 37748736");
                                writer.WriteLine("250-AUTH LOGIN PLAIN");
                                writer.WriteLine("250 PIPELINING");
                            }
                            else if (cmdUpper.StartsWith("AUTH "))
                            {
                                if (cmdUpper.Contains("LOGIN"))
                                {
                                    writer.WriteLine("334 VXNlcm5hbWU6");
                                    reader.ReadLine();
                                    writer.WriteLine("334 UGFzc3dvcmQ6");
                                    reader.ReadLine();
                                    writer.WriteLine("235 2.7.0 Authentication successful");
                                }
                                else
                                {
                                    writer.WriteLine("235 2.7.0 Authentication successful");
                                }
                            }
                            else if (cmdUpper.StartsWith("MAIL FROM:"))
                            {
                                from = cmd.Substring(10).Trim('<', '>', ' ');
                                writer.WriteLine("250 OK");
                            }
                            else if (cmdUpper.StartsWith("RCPT TO:"))
                            {
                                string rcpt = cmd.Substring(8).Trim('<', '>', ' ');
                                to.Add(rcpt);
                                writer.WriteLine("250 OK");
                            }
                            else if (cmdUpper == "DATA")
                            {
                                inData = true;
                                dataBuilder.Clear();
                                writer.WriteLine("354 Start mail input; end with <CR><LF>.<CR><LF>");
                            }
                            else if (cmdUpper == "QUIT")
                            {
                                writer.WriteLine("221 Bye");
                                break;
                            }
                            else if (cmdUpper == "RSET")
                            {
                                from = "";
                                to.Clear();
                                dataBuilder.Clear();
                                writer.WriteLine("250 OK");
                            }
                            else if (cmdUpper == "NOOP")
                            {
                                writer.WriteLine("250 OK");
                            }
                            else
                            {
                                writer.WriteLine("500 Syntax error, command unrecognized");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void ProcessReceivedEmail(string from, List<string> toList, string rawEmail)
        {
            try
            {
                try { File.WriteAllText(ConfigHelper.GetDataFilePath("raw_email.txt"), rawEmail, Encoding.UTF8); } catch { }
                string subject = "(No Subject)";
                string body = "";
                string contentType = "";
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                int headerEnd = rawEmail.IndexOf("\r\n\r\n");
                if (headerEnd == -1) headerEnd = rawEmail.IndexOf("\n\n");

                string headersText = headerEnd != -1 ? rawEmail.Substring(0, headerEnd) : rawEmail;
                body = headerEnd != -1 ? rawEmail.Substring(headerEnd).Trim() : "";

                // Unfold folded headers (RFC 2822: CRLF followed by space/tab)
                headersText = Regex.Replace(headersText, @"\r?\n[ \t]", " ");

                string[] headerLines = headersText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (string line in headerLines)
                {
                    if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
                    {
                        subject = line.Substring(8).Trim();
                        subject = DecodeMimeHeader(subject);
                    }
                    else if (line.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = line.Substring(13).Trim();
                    }
                    else if (line.StartsWith("Date:", StringComparison.OrdinalIgnoreCase))
                    {
                        dateStr = line.Substring(5).Trim();
                    }
                }

                bool isHtml = contentType.Contains("text/html");

                if (contentType.Contains("multipart/"))
                {
                    string boundary = "";
                    var match = Regex.Match(contentType, @"boundary\s*=\s*""?([^"";\s]+)""?", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        boundary = match.Groups[1].Value;
                    }

                    if (!string.IsNullOrEmpty(boundary))
                    {
                        string[] parts = body.Split(new[] { "--" + boundary }, StringSplitOptions.None);
                        foreach (string part in parts)
                        {
                            if (part.Trim() == "--" || string.IsNullOrEmpty(part.Trim())) continue;
                            
                            int partHeaderEnd = part.IndexOf("\r\n\r\n");
                            if (partHeaderEnd == -1) partHeaderEnd = part.IndexOf("\n\n");
                            if (partHeaderEnd == -1) continue;

                            string partHeaders = part.Substring(0, partHeaderEnd);
                            partHeaders = Regex.Replace(partHeaders, @"\r?\n[ \t]", " ");
                            string partBody = part.Substring(partHeaderEnd).Trim();

                            bool partIsHtml = partHeaders.Contains("text/html");
                            bool partIsText = partHeaders.Contains("text/plain");

                            if (partIsHtml)
                            {
                                body = partBody;
                                isHtml = true;
                                break;
                            }
                            else if (partIsText && !isHtml)
                            {
                                body = partBody;
                                isHtml = false;
                            }
                        }
                    }
                }

                if (rawEmail.IndexOf("Content-Transfer-Encoding: quoted-printable", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    body = DecodeQuotedPrintable(body);
                }

                var email = new CaughtEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    From = from,
                    To = string.Join(", ", toList),
                    Subject = subject,
                    Body = body,
                    Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    IsHtml = isHtml
                };

                SaveCaughtEmail(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Process email error: " + ex.Message);
            }
        }

        private static string DecodeMimeHeader(string value)
        {
            try
            {
                var regex = new Regex(@"=\?([^?]+)\?([BQ])\?([^?]+)\?=", RegexOptions.IgnoreCase);
                return regex.Replace(value, m =>
                {
                    string charset = m.Groups[1].Value;
                    string encoding = m.Groups[2].Value.ToUpper();
                    string data = m.Groups[3].Value;

                    if (encoding == "B")
                    {
                        byte[] bytes = Convert.FromBase64String(data);
                        return Encoding.GetEncoding(charset).GetString(bytes);
                    }
                    else if (encoding == "Q")
                    {
                        List<byte> bytes = new List<byte>();
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (data[i] == '=')
                            {
                                string hex = data.Substring(i + 1, 2);
                                bytes.Add((byte)Convert.ToInt32(hex, 16));
                                i += 2;
                            }
                            else if (data[i] == '_')
                            {
                                bytes.Add((byte)' ');
                            }
                            else
                            {
                                bytes.Add((byte)data[i]);
                            }
                        }
                        try
                        {
                            return Encoding.GetEncoding(charset).GetString(bytes.ToArray());
                        }
                        catch
                        {
                            return Encoding.UTF8.GetString(bytes.ToArray());
                        }
                    }
                    return m.Value;
                });
            }
            catch
            {
                return value;
            }
        }

        private static string DecodeQuotedPrintable(string input)
        {
            try
            {
                string output = Regex.Replace(input, @"=\r?\n", "");
                List<byte> bytes = new List<byte>();
                for (int i = 0; i < output.Length; i++)
                {
                    char c = output[i];
                    if (c == '=' && i + 2 < output.Length)
                    {
                        string hex = output.Substring(i + 1, 2);
                        if (Regex.IsMatch(hex, "^[0-9A-Fa-f]{2}$"))
                        {
                            byte b = (byte)Convert.ToInt32(hex, 16);
                            bytes.Add(b);
                            i += 2;
                            continue;
                        }
                    }
                    
                    if (c == '\r' && i + 1 < output.Length && output[i + 1] == '\n')
                    {
                        bytes.Add((byte)'\r');
                        bytes.Add((byte)'\n');
                        i++;
                    }
                    else if (c == '\n')
                    {
                        bytes.Add((byte)'\n');
                    }
                    else
                    {
                        byte[] charBytes = Encoding.UTF8.GetBytes(new[] { c });
                        bytes.AddRange(charBytes);
                    }
                }
                return Encoding.UTF8.GetString(bytes.ToArray());
            }
            catch
            {
                return input;
            }
        }

        private static string SimpleHtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }

        private static readonly object _emailLock = new object();
        private static string EmailsFilePath { get { return ConfigHelper.GetDataFilePath("caught_emails.json"); } }

        public static List<CaughtEmail> LoadCaughtEmails()
        {
            lock (_emailLock)
            {
                try
                {
                    string path = EmailsFilePath;
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path, Encoding.UTF8);
                        return ParseEmailsJson(json);
                    }
                }
                catch { }
                return new List<CaughtEmail>();
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private static string ReadJsonStringValue(string json, string key)
        {
            string searchKey = "\"" + key + "\"";
            int keyIdx = json.IndexOf(searchKey);
            if (keyIdx < 0) return "";
            int colonIdx = json.IndexOf(':', keyIdx + searchKey.Length);
            if (colonIdx < 0) return "";
            int openQuote = json.IndexOf('"', colonIdx + 1);
            if (openQuote < 0) return "";
            var sb = new StringBuilder();
            for (int i = openQuote + 1; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char nc = json[i + 1];
                    if (nc == '"') { sb.Append('"'); i++; }
                    else if (nc == '\\') { sb.Append('\\'); i++; }
                    else if (nc == 'n') { sb.Append('\n'); i++; }
                    else if (nc == 'r') { sb.Append('\r'); i++; }
                    else if (nc == 't') { sb.Append('\t'); i++; }
                    else { sb.Append(nc); i++; }
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static bool ReadJsonBoolValue(string json, string key)
        {
            string searchKey = "\"" + key + "\"";
            int keyIdx = json.IndexOf(searchKey);
            if (keyIdx < 0) return false;
            int colonIdx = json.IndexOf(':', keyIdx + searchKey.Length);
            if (colonIdx < 0) return false;
            // skip whitespace
            int vi = colonIdx + 1;
            while (vi < json.Length && (json[vi] == ' ' || json[vi] == '\t' || json[vi] == '\r' || json[vi] == '\n')) vi++;
            return vi < json.Length && json[vi] == 't';
        }

        private static List<CaughtEmail> ParseEmailsJson(string json)
        {
            var result = new List<CaughtEmail>();
            // Find each object block {}
            int i = 0;
            while (i < json.Length)
            {
                int start = json.IndexOf('{', i);
                if (start < 0) break;
                int depth = 0;
                int end = start;
                for (int j = start; j < json.Length; j++)
                {
                    if (json[j] == '{') depth++;
                    else if (json[j] == '}') { depth--; if (depth == 0) { end = j; break; } }
                }
                if (end <= start) break;
                string obj = json.Substring(start, end - start + 1);
                var email = new CaughtEmail();
                email.Id = ReadJsonStringValue(obj, "Id");
                email.From = ReadJsonStringValue(obj, "From");
                email.To = ReadJsonStringValue(obj, "To");
                email.Subject = ReadJsonStringValue(obj, "Subject");
                email.Body = ReadJsonStringValue(obj, "Body");
                email.Date = ReadJsonStringValue(obj, "Date");
                email.IsHtml = ReadJsonBoolValue(obj, "IsHtml");
                result.Add(email);
                i = end + 1;
            }
            return result;
        }

        public static void SaveCaughtEmails(List<CaughtEmail> list)
        {
            lock (_emailLock)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.Append("[\r\n");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        sb.Append("  {\r\n");
                        sb.AppendFormat("    \"Id\": \"{0}\",\r\n", EscapeJsonString(e.Id));
                        sb.AppendFormat("    \"From\": \"{0}\",\r\n", EscapeJsonString(e.From));
                        sb.AppendFormat("    \"To\": \"{0}\",\r\n", EscapeJsonString(e.To));
                        sb.AppendFormat("    \"Subject\": \"{0}\",\r\n", EscapeJsonString(e.Subject));
                        sb.AppendFormat("    \"Body\": \"{0}\",\r\n", EscapeJsonString(e.Body));
                        sb.AppendFormat("    \"Date\": \"{0}\",\r\n", EscapeJsonString(e.Date));
                        sb.AppendFormat("    \"IsHtml\": {0}\r\n", e.IsHtml ? "true" : "false");
                        sb.Append("  }");
                        if (i < list.Count - 1) sb.Append(",");
                        sb.Append("\r\n");
                    }
                    sb.Append("]");
                    File.WriteAllText(EmailsFilePath, sb.ToString(), Encoding.UTF8);
                }
                catch { }
            }
        }

        public static void SaveCaughtEmail(CaughtEmail email)
        {
            var list = LoadCaughtEmails();
            list.Insert(0, email);
            if (list.Count > 100)
            {
                list.RemoveRange(100, list.Count - 100);
            }
            SaveCaughtEmails(list);

            if (Instance != null)
            {
                Instance.BeginInvoke(new Action(() => {
                    Instance.RefreshMailList();
                }));
            }
        }

        public void RefreshMailList()
        {
            try
            {
                var list = LoadCaughtEmails();
                lstMailInbox.BeginUpdate();
                lstMailInbox.Items.Clear();
                foreach (var email in list)
                {
                    lstMailInbox.Items.Add(email);
                }
                lstMailInbox.EndUpdate();

                if (list.Count == 0)
                {
                    pnlMailDetail.Visible = false;
                    lblMailEmpty.Visible = true;
                }
            }
            catch { }
        }

        private void LstMailInbox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstMailInbox.Items.Count) return;
            var email = lstMailInbox.Items[e.Index] as CaughtEmail;
            if (email == null) return;

            e.DrawBackground();
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bgCol = isSelected ? Color.FromArgb(239, 246, 255) : Color.White;
            using (var brush = new SolidBrush(bgCol))
            {
                g.FillRectangle(brush, e.Bounds);
            }

            if (isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(37, 99, 235), 2f))
                {
                    g.DrawLine(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.X, e.Bounds.Bottom);
                }
            }

            using (var fontSubject = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var brushSubject = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                string displaySubject = email.Subject;
                if (displaySubject.Length > 28) displaySubject = displaySubject.Substring(0, 26) + "...";
                g.DrawString(displaySubject, fontSubject, brushSubject, e.Bounds.X + 12, e.Bounds.Y + 6);
            }

            using (var fontDetail = new Font("Segoe UI", 7.5f))
            using (var brushDetail = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                string fromDisplay = email.From;
                if (fromDisplay.Contains("<"))
                {
                    int startIdx = fromDisplay.IndexOf("<");
                    if (startIdx > 0) fromDisplay = fromDisplay.Substring(0, startIdx).Trim();
                }
                if (fromDisplay.Length > 18) fromDisplay = fromDisplay.Substring(0, 16) + "...";
                string detail = fromDisplay + "  •  " + email.Date;
                g.DrawString(detail, fontDetail, brushDetail, e.Bounds.X + 12, e.Bounds.Y + 23);
            }

            using (var penDivider = new Pen(Color.FromArgb(241, 245, 249), 1f))
            {
                g.DrawLine(penDivider, e.Bounds.X + 10, e.Bounds.Bottom - 1, e.Bounds.Right - 10, e.Bounds.Bottom - 1);
            }

            e.DrawFocusRectangle();
        }

        private void LstMailInbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstMailInbox.SelectedIndex == -1)
            {
                pnlMailDetail.Visible = false;
                lblMailEmpty.Visible = true;
                return;
            }

            var email = lstMailInbox.SelectedItem as CaughtEmail;
            if (email == null) return;

            lblMailEmpty.Visible = false;
            pnlMailDetail.Visible = true;

            lblMailSubject.Text = email.Subject;
            lblMailFrom.Text = "Từ: " + email.From;
            lblMailTo.Text = "Đến: " + email.To;
            lblMailDate.Text = "Thời gian: " + email.Date;

            string html = email.IsHtml ? email.Body : "<html><body><pre style='font-family: Consolas, monospace; font-size: 12px; white-space: pre-wrap;'>" + SimpleHtmlEncode(email.Body) + "</pre></body></html>";
            
            // Resolve relative/local paths
            html = ResolveHtmlImageSources(html);
            
            webMailBody.DocumentText = html;
        }

        private static Dictionary<string, string> _webpToPngCache = new Dictionary<string, string>();

        public static string ConvertWebpToPng(string webpPath)
        {
            if (string.IsNullOrEmpty(webpPath) || !File.Exists(webpPath)) return null;

            lock (_webpToPngCache)
            {
                if (_webpToPngCache.ContainsKey(webpPath))
                {
                    string cachedPath = _webpToPngCache[webpPath];
                    if (File.Exists(cachedPath))
                    {
                        return cachedPath;
                    }
                }

                try
                {
                    string tmpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"tmp\mail_images");
                    if (!Directory.Exists(tmpDir))
                    {
                        Directory.CreateDirectory(tmpDir);
                    }

                    string pngPath = Path.Combine(tmpDir, Guid.NewGuid().ToString() + ".png");

                    string phpExe = null;
                    if (Instance != null && !string.IsNullOrEmpty(Instance.pathPhpExe))
                    {
                        phpExe = Instance.pathPhpExe;
                        if (!Path.IsPathRooted(phpExe))
                        {
                            phpExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, phpExe);
                        }
                    }

                    if (string.IsNullOrEmpty(phpExe) || !File.Exists(phpExe))
                    {
                        phpExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\php\php.exe");
                    }

                    if (!File.Exists(phpExe))
                    {
                        string phpBinDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\php");
                        if (Directory.Exists(phpBinDir))
                        {
                            foreach (var sub in Directory.GetDirectories(phpBinDir))
                            {
                                string tryPhp = Path.Combine(sub, "php.exe");
                                if (File.Exists(tryPhp))
                                {
                                    phpExe = tryPhp;
                                    break;
                                }
                            }
                        }
                    }

                    if (!File.Exists(phpExe)) return null;

                    string escapedWebp = webpPath.Replace("\\", "\\\\").Replace("'", "\\'");
                    string escapedPng = pngPath.Replace("\\", "\\\\").Replace("'", "\\'");
                    string phpCode = string.Format(
                        "$img = @imagecreatefromwebp('{0}'); if ($img) {{ @imagepng($img, '{1}'); @imagedestroy($img); }}",
                        escapedWebp, escapedPng
                    );

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = phpExe,
                        Arguments = "-r \"" + phpCode.Replace("\"", "\\\"") + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        process.WaitForExit(5000);
                    }

                    if (File.Exists(pngPath))
                    {
                        _webpToPngCache[webpPath] = pngPath;
                        return pngPath;
                    }
                }
                catch { }

                return null;
            }
        }

        public static string ResolveHtmlImageSources(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            try
            {
                // Inject CSS to reset image borders and enable high quality scaling in IE
                string css = "<style>img { border: none !important; -ms-interpolation-mode: bicubic; }</style>";
                if (html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var regexHead = new Regex("<head>", RegexOptions.IgnoreCase);
                    html = regexHead.Replace(html, "<head>" + css, 1);
                }
                else
                {
                    html = css + html;
                }

                var regex = new Regex(@"(<img\s+[^>]*src\s*=\s*(['""]))([^'"" >]+)(\2)", RegexOptions.IgnoreCase);
                return regex.Replace(html, m =>
                {
                    string prefix = m.Groups[1].Value;
                    string path = m.Groups[3].Value;
                    string suffix = m.Groups[4].Value;

                    bool isLocalUrl = false;
                    string localRemainder = "";

                    if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        var urlMatch = Regex.Match(path, @"^https?://([^/]+)/(.*)$", RegexOptions.IgnoreCase);
                        if (urlMatch.Success)
                        {
                            string host = urlMatch.Groups[1].Value.ToLower();
                            string remainder = urlMatch.Groups[2].Value;

                            if (host == "localhost" || host == "127.0.0.1" || host.EndsWith(".local"))
                            {
                                isLocalUrl = true;
                                localRemainder = remainder;
                            }
                        }

                        if (!isLocalUrl)
                        {
                            return m.Value;
                        }
                    }
                    else if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                             path.StartsWith("cid:", StringComparison.OrdinalIgnoreCase) ||
                             path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    {
                        return m.Value;
                    }

                    string relativePath = isLocalUrl ? localRemainder : path;
                    string resolvedPath = null;

                    if (relativePath.Length > 2 && relativePath[1] == ':' && (relativePath[2] == '\\' || relativePath[2] == '/'))
                    {
                        resolvedPath = relativePath;
                    }
                    else
                    {
                        resolvedPath = ResolveRelativeImagePath(relativePath);
                    }

                    if (resolvedPath != null)
                    {
                        if (resolvedPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                        {
                            string pngPath = ConvertWebpToPng(resolvedPath);
                            if (pngPath != null)
                            {
                                resolvedPath = pngPath;
                            }
                        }

                        string fileUrl = "file:///" + resolvedPath.Replace('\\', '/');
                        return prefix + fileUrl + suffix;
                    }

                    return m.Value;
                });
            }
            catch
            {
                return html;
            }
        }

        public static string ResolveRelativeImagePath(string relativePath)
        {
            try
            {
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
                if (Directory.Exists(wwwDir))
                {
                    string fullPath = Path.Combine(wwwDir, relativePath);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }

                    foreach (string dir in Directory.GetDirectories(wwwDir, "*", SearchOption.AllDirectories))
                    {
                        string tryPath = Path.Combine(dir, relativePath);
                        if (File.Exists(tryPath))
                        {
                            return tryPath;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }

    // =======================================================
    // MAIL DETAIL ZOOM POPUP FORM
    // =======================================================
    public class MailDetailForm : Form
    {
        private WebBrowser webBody;
        private Label lblSubject;
        private Label lblFrom;
        private Label lblTo;
        private Label lblDate;

        public MailDetailForm(CaughtEmail email)
        {
            this.Text = "Chi tiết Email - " + email.Subject;
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.BackColor = Color.FromArgb(248, 250, 252); // Slate 50

            // Header Panel
            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 110;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Padding = new Padding(20, 15, 20, 10);
            pnlHeader.Paint += (s, e) => {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f)) // Slate 200
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };
            this.Controls.Add(pnlHeader);

            lblSubject = new Label();
            lblSubject.Text = email.Subject;
            lblSubject.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblSubject.ForeColor = Color.FromArgb(30, 41, 59);
            lblSubject.Location = new Point(20, 12);
            lblSubject.Size = new Size(740, 24);
            lblSubject.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblSubject);

            lblFrom = new Label();
            lblFrom.Text = "Từ: " + email.From;
            lblFrom.Font = new Font("Segoe UI", 9f);
            lblFrom.ForeColor = Color.FromArgb(100, 116, 139);
            lblFrom.Location = new Point(20, 40);
            lblFrom.Size = new Size(740, 18);
            lblFrom.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblFrom);

            lblTo = new Label();
            lblTo.Text = "Đến: " + email.To;
            lblTo.Font = new Font("Segoe UI", 9f);
            lblTo.ForeColor = Color.FromArgb(100, 116, 139);
            lblTo.Location = new Point(20, 58);
            lblTo.Size = new Size(740, 18);
            lblTo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblTo);

            lblDate = new Label();
            lblDate.Text = "Thời gian: " + email.Date;
            lblDate.Font = new Font("Segoe UI", 8.5f);
            lblDate.ForeColor = Color.FromArgb(100, 116, 139);
            lblDate.Location = new Point(20, 76);
            lblDate.Size = new Size(740, 18);
            lblDate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblDate);

            // Web Browser Container
            Panel pnlBrowserWrap = new Panel();
            pnlBrowserWrap.Dock = DockStyle.Fill;
            pnlBrowserWrap.Padding = new Padding(20);
            pnlBrowserWrap.BackColor = Color.FromArgb(248, 250, 252);
            this.Controls.Add(pnlBrowserWrap);
            pnlBrowserWrap.BringToFront();

            webBody = new WebBrowser();
            webBody.Dock = DockStyle.Fill;
            webBody.ScriptErrorsSuppressed = true;
            webBody.Navigating += (s, e) => {
                string urlObj = e.Url.ToString();
                if (urlObj != "about:blank")
                {
                    e.Cancel = true;
                    if (urlObj.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        urlObj.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        try { System.Diagnostics.Process.Start(urlObj); } catch { }
                    }
                }
            };
            pnlBrowserWrap.Controls.Add(webBody);

            string html = email.IsHtml ? email.Body : "<html><body><pre style='font-family: Consolas, monospace; font-size: 12px; white-space: pre-wrap;'>" + SimpleHtmlEncode(email.Body) + "</pre></body></html>";
            
            // Resolve relative paths in C# before loading
            html = MainForm.ResolveHtmlImageSources(html);

            webBody.DocumentText = html;
        }

        private static string SimpleHtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }
    }

    // =======================================================
    // VIRTUAL HOST CONFIGURATION FORM
    // =======================================================
    public class VirtualHostForm : Form
    {
        private string _sitePath;
        private string _folderName;

        private TextBox _txtDomain;
        private ModernButton _btnSave;
        private ModernButton _btnCancel;

        private Color colorBg = Color.White;
        private Color colorBorder = Color.FromArgb(226, 232, 240);
        private Color colorText = Color.FromArgb(15, 23, 42);
        private Color colorTextDim = Color.FromArgb(100, 116, 139);


        private void ApplyRoundedRegion(Control control, int radius)
        {
            // Fully disabled to prevent Access Violations / GDI+ Region crashes
        }

        public VirtualHostForm(string sitePath, string folderName)
        {
            _sitePath = sitePath;
            _folderName = folderName;

            this.Text = "Cấu hình Host ảo - " + folderName;
            this.Size = new Size(380, 180);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252); // Slate 50 background
            this.ShowInTaskbar = false;

            // Paint Outer Border (matching Deploy form)
            this.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen penPopup = new Pen(Color.FromArgb(148, 163, 184), 1.5f)) // Slate 400
                {
                    pe.Graphics.DrawRectangle(penPopup, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // ── HEADER PANEL (matching Deploy form) ──────────────────────────
            Panel pnlHeader = new Panel();
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Size = new Size(380, 40);
            pnlHeader.BackColor = Color.FromArgb(241, 245, 249); // Slate 100
            pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Font font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush br = new SolidBrush(Color.FromArgb(37, 99, 235))) // Blue-600
                {
                    pe.Graphics.DrawString("⚡  CẤU HÌNH HOST ẢO LOCAL", font, br, new PointF(14f, 11f));
                }
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    pe.Graphics.DrawLine(pen, 0, 39, 380, 39);
                }
            };
            pnlHeader.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    MainForm.ReleaseCapture();
                    MainForm.SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            this.Controls.Add(pnlHeader);

            Label btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnClose.ForeColor = Color.FromArgb(148, 163, 184); // Slate 400
            btnClose.Location = new Point(345, 8);
            btnClose.Size = new Size(26, 24);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.FromArgb(239, 68, 68); };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(148, 163, 184); btnClose.BackColor = Color.Transparent; };
            pnlHeader.Controls.Add(btnClose);

            // ── DOMAIN FIELD ──────────────────────────────────────────
            bool enabled;
            string domain;
            bool useSsl;
            MainForm.GetVHostConfig(sitePath, folderName, out enabled, out domain, out useSsl);

            Label lblDomain = new Label();
            lblDomain.Text = "Tên miền ảo:";
            lblDomain.Font = new Font("Segoe UI", 9f);
            lblDomain.ForeColor = Color.FromArgb(100, 116, 139); // Slate 500
            lblDomain.Location = new Point(20, 68);
            lblDomain.Size = new Size(90, 20);
            this.Controls.Add(lblDomain);

            Panel pnlDomainWrap = new Panel();
            pnlDomainWrap.Location = new Point(115, 62);
            pnlDomainWrap.Size = new Size(245, 30);
            pnlDomainWrap.BackColor = Color.White;
            pnlDomainWrap.Paint += (s, paintEvt) => {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f)) // Slate 300
                {
                    paintEvt.Graphics.DrawRectangle(pen, 0, 0, pnlDomainWrap.Width - 1, pnlDomainWrap.Height - 1);
                }
            };
            this.Controls.Add(pnlDomainWrap);
            ApplyRoundedRegion(pnlDomainWrap, 6);

            _txtDomain = new TextBox();
            _txtDomain.Text = domain;
            _txtDomain.Font = new Font("Segoe UI", 9.5f);
            _txtDomain.Location = new Point(8, 6);
            _txtDomain.Size = new Size(229, 18);
            _txtDomain.BorderStyle = BorderStyle.None;
            _txtDomain.BackColor = Color.White;
            _txtDomain.ForeColor = Color.FromArgb(15, 23, 42); // Slate 900
            pnlDomainWrap.Controls.Add(_txtDomain);

            // ── ACTION BUTTONS ─────────────────────────────────────────
            if (!enabled)
            {
                // Host ảo chưa bật: hiện nút "Bật Host ảo" và "Hủy"
                _btnSave = new ModernButton();
                _btnSave.Text = "Bật Host ảo";
                _btnSave.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                _btnSave.NormalColor = Color.FromArgb(34, 197, 94); // Green-500
                _btnSave.HoverColor = Color.FromArgb(22, 163, 74); // Green-600
                _btnSave.ForeColor = Color.White;
                _btnSave.Location = new Point(70, 118);
                _btnSave.Size = new Size(130, 32);
                _btnSave.CornerRadius = 6;
                _btnSave.Click += (s, e) => {
                    string newDomain = _txtDomain.Text.Trim().ToLower();
                    if (string.IsNullOrEmpty(newDomain))
                    {
                        MessageBox.Show("Vui lòng nhập tên miền ảo hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    MainForm.SaveVHostConfig(_sitePath, true, newDomain, true);
                    MainForm.UpdateAllProjectsEnvFiles("");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(_btnSave);

                _btnCancel = new ModernButton();
                _btnCancel.Text = "Hủy";
                _btnCancel.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                _btnCancel.NormalColor = Color.White;
                _btnCancel.HoverColor = Color.FromArgb(243, 244, 246);
                _btnCancel.ForeColor = Color.FromArgb(107, 114, 128);
                _btnCancel.BorderColor = Color.FromArgb(229, 231, 235);
                _btnCancel.Location = new Point(210, 118);
                _btnCancel.Size = new Size(100, 32);
                _btnCancel.CornerRadius = 6;
                _btnCancel.Click += (s, e) => {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                this.Controls.Add(_btnCancel);
            }
            else
            {
                // Host ảo đang chạy: hiện nút "Cập nhật", "Tắt Host ảo" và "Hủy"
                _btnSave = new ModernButton();
                _btnSave.Text = "Cập nhật";
                _btnSave.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                _btnSave.NormalColor = Color.FromArgb(59, 130, 246); // Blue-500
                _btnSave.HoverColor = Color.FromArgb(37, 99, 235); // Blue-600
                _btnSave.ForeColor = Color.White;
                _btnSave.Location = new Point(30, 118);
                _btnSave.Size = new Size(100, 32);
                _btnSave.CornerRadius = 6;
                _btnSave.Click += (s, e) => {
                    string newDomain = _txtDomain.Text.Trim().ToLower();
                    if (string.IsNullOrEmpty(newDomain))
                    {
                        MessageBox.Show("Vui lòng nhập tên miền ảo hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (newDomain != domain)
                    {
                        MainForm.RemoveHostsEntry(domain);
                    }

                    MainForm.SaveVHostConfig(_sitePath, true, newDomain, true);
                    MainForm.UpdateAllProjectsEnvFiles("");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(_btnSave);

                ModernButton btnToggleOff = new ModernButton();
                btnToggleOff.Text = "Tắt Host ảo";
                btnToggleOff.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                btnToggleOff.NormalColor = Color.FromArgb(239, 68, 68); // Red-500
                btnToggleOff.HoverColor = Color.FromArgb(220, 38, 38); // Red-600
                btnToggleOff.ForeColor = Color.White;
                btnToggleOff.Location = new Point(140, 118);
                btnToggleOff.Size = new Size(110, 32);
                btnToggleOff.CornerRadius = 6;
                btnToggleOff.Click += (s, e) => {
                    MainForm.RemoveHostsEntry(domain);
                    MainForm.SaveVHostConfig(_sitePath, false, domain, true);
                    MainForm.UpdateAllProjectsEnvFiles("");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnToggleOff);

                _btnCancel = new ModernButton();
                _btnCancel.Text = "Hủy";
                _btnCancel.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                _btnCancel.NormalColor = Color.White;
                _btnCancel.HoverColor = Color.FromArgb(243, 244, 246);
                _btnCancel.ForeColor = Color.FromArgb(107, 114, 128);
                _btnCancel.BorderColor = Color.FromArgb(229, 231, 235);
                _btnCancel.Location = new Point(260, 118);
                _btnCancel.Size = new Size(80, 32);
                _btnCancel.CornerRadius = 6;
                _btnCancel.Click += (s, e) => {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                this.Controls.Add(_btnCancel);
            }
        }
    }


    // =======================================================
    // DEPLOY DEMO FORM - Tối giản, chỉ nhập Database & SSL
    // =======================================================
    public class DeployDemoForm : Form
    {
        private string _projectDir;
        private string _sitePath;
        private RichTextBox _logBox;
        private Label _btnClose;
        private Panel _pnlHeader;
        private ModernButton _btnDeploy;
        private ModernButton _btnDeployFresh;
        private ModernButton _btnCleanup;
        private Panel _pnlConfig;
        private Label _lblApiStatus;
        private CheckBox _chkSsl;
        private TextBox _txtDbName;
        private bool _onlyDbChecked = false;
        private bool _use7zipChecked = false;
        private bool _isLocked = false;
        private ModernButton _btnLockToggle;
        private ModernButton _btnOpenWeb;
        private string _deployedUrl = "";

        // Hidden backing fields for FTP/DA config (loaded from global settings)
        private TextBox _txtFtpHost, _txtFtpUser, _txtFtpPass, _txtFtpRoot, _txtDaUser, _txtDaPort, _txtWebDomain;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // ── PATHS ───────────────────────────────────────────────
        private static string GlobalConfigPath {
            get { return ConfigHelper.GetDataFilePath("demo_global_config.json"); }
        }
        private static string DeployStatusPath {
            get { return ConfigHelper.GetDataFilePath("demo_deploy_status.json"); }
        }
        private static string SslConfigPath {
            get { return ConfigHelper.GetDataFilePath("demo_ssl_config.json"); }
        }
        private static string DbConfigPath {
            get { return ConfigHelper.GetDataFilePath("demo_db_config.json"); }
        }

        // Color Palette matching main app's premium light aesthetic
        private Color colorBg = Color.FromArgb(248, 250, 252);     // Slate 50 (Nền form)
        private Color colorCard = Color.White;                    // Card nền trắng
        private Color colorBorder = Color.FromArgb(226, 232, 240);  // Slate 200 (Viền)
        private Color colorText = Color.FromArgb(30, 41, 59);       // Slate 800 (Chữ chính)
        private Color colorDim = Color.FromArgb(100, 116, 139);     // Slate 500 (Chữ phụ)
        private Color colorPurple = Color.FromArgb(139, 92, 246);   // Tím accent
        private Color colorGreen = Color.FromArgb(16, 185, 129);    // Xanh lá
        private Color colorRed = Color.FromArgb(239, 68, 68);      // Đỏ
        private Color colorOrange = Color.FromArgb(245, 158, 11);  // Cam/Vàng cảnh báo

        public DeployDemoForm(string projectDir, string sitePath)
        {
            _projectDir = projectDir;
            _sitePath = sitePath;
            _isLocked = IsDeployed(_projectDir);

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(600, 480);
            this.BackColor = colorBg;

            // ── HEADER ──────────────────────────────────────────────────
            _pnlHeader = new Panel();
            _pnlHeader.Location = new Point(0, 0);
            _pnlHeader.Size = new Size(600, 40);
            _pnlHeader.BackColor = Color.FromArgb(241, 245, 249); // Slate 100
            _pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Font font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush br = new SolidBrush(colorPurple))
                    pe.Graphics.DrawString("⚡  DEPLOY DEMO HOSTING", font, br, new PointF(14f, 11f)); // Dùng sấm sét ⚡ an toàn, không lỗi ô vuông
                using (Pen pen = new Pen(colorBorder, 1f))
                    pe.Graphics.DrawLine(pen, 0, 39, 600, 39);
            };
            _pnlHeader.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            this.Controls.Add(_pnlHeader);

            _btnClose = new Label();
            _btnClose.Text = "✕";
            _btnClose.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnClose.ForeColor = colorDim;
            _btnClose.Location = new Point(565, 8);
            _btnClose.Size = new Size(26, 24);
            _btnClose.TextAlign = ContentAlignment.MiddleCenter;
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.Click += (s, e) => this.Close();
            _btnClose.MouseEnter += (s, e) => { _btnClose.ForeColor = Color.White; _btnClose.BackColor = colorRed; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.ForeColor = colorDim; _btnClose.BackColor = Color.Transparent; };
            _pnlHeader.Controls.Add(_btnClose);

            // ── CONFIG PANEL (Top UI) ───────────────────────────────────
            _pnlConfig = new Panel();
            _pnlConfig.Location = new Point(15, 55);
            _pnlConfig.Size = new Size(570, 110);
            _pnlConfig.BackColor = colorCard;
            _pnlConfig.Paint += DrawCardBorder;
            this.Controls.Add(_pnlConfig);
            ApplyRoundedRegion(_pnlConfig, 8);

            // Backing textboxes (not added to controls, background only)
            _txtFtpHost = new TextBox();
            _txtFtpUser = new TextBox();
            _txtFtpPass = new TextBox();
            _txtFtpRoot = new TextBox();
            _txtDaUser = new TextBox();
            _txtDaPort = new TextBox();
            _txtWebDomain = new TextBox();

            BuildConfigPanel();

            // ── LOG BOX (Middle UI) ─────────────────────────────────────
            _logBox = new RichTextBox();
            _logBox.Location = new Point(15, 175);
            _logBox.Size = new Size(570, 235);
            _logBox.BackColor = Color.White; // Nền sáng trắng mượt mà đúng chuẩn "cho ô hiển thị light có border luôn"
            _logBox.ForeColor = Color.FromArgb(15, 23, 42); // Chữ sẫm màu
            _logBox.Font = new Font("Consolas", 8.5f);
            _logBox.BorderStyle = BorderStyle.None;
            _logBox.ReadOnly = true;
            this.Controls.Add(_logBox);
            ApplyRoundedRegion(_logBox, 6);

            // Draw border for RichTextBox and external popup border
            this.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Vẽ viền cho Log Box sáng (màu Slate 300) bao quanh sắc nét
                using (Pen pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                {
                    pe.Graphics.DrawRectangle(pen, _logBox.Location.X - 1, _logBox.Location.Y - 1, _logBox.Width + 1, _logBox.Height + 1);
                }
                
                // Vẽ đường viền bao quanh toàn bộ popup Deploy để dễ phân biệt
                using (Pen penPopup = new Pen(Color.FromArgb(148, 163, 184), 1.5f)) // Màu Slate 400 cao cấp
                {
                    pe.Graphics.DrawRectangle(penPopup, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // ── STATUS & BUTTONS (Bottom UI) ────────────────────────────
            _lblApiStatus = new Label();
            _lblApiStatus.Text = "Sẵn sàng deploy";
            _lblApiStatus.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            _lblApiStatus.ForeColor = colorDim;
            _lblApiStatus.Location = new Point(15, 425);
            _lblApiStatus.Size = new Size(175, 25);
            _lblApiStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(_lblApiStatus);

            _btnDeploy = new ModernButton();
            _btnDeploy.Text = "Deploy";
            _btnDeploy.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            _btnDeploy.NormalColor = colorPurple;
            _btnDeploy.HoverColor = Color.FromArgb(124, 58, 237);
            _btnDeploy.PressedColor = Color.FromArgb(109, 40, 217);
            _btnDeploy.BorderColor = Color.Transparent;
            _btnDeploy.ForeColor = Color.White;
            _btnDeploy.CornerRadius = 6;
            _btnDeploy.Location = new Point(195, 422);
            _btnDeploy.Size = new Size(125, 32);
            _btnDeploy.Click += (s, e) => StartDeploy(_onlyDbChecked);
            this.Controls.Add(_btnDeploy);

            _btnLockToggle = new ModernButton();
            _btnLockToggle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            _btnLockToggle.CornerRadius = 6;
            _btnLockToggle.Location = new Point(328, 422);
            _btnLockToggle.Size = new Size(125, 32);
            _btnLockToggle.Click += (s, e) =>
            {
                _isLocked = !_isLocked;
                SetDeployed(_projectDir, _isLocked);
                if (!_isLocked)
                {
                    if (_btnOpenWeb != null) _btnOpenWeb.Visible = false;
                    _btnDeploy.Visible = true;
                }
                UpdateDeployButtonsState();
            };
            this.Controls.Add(_btnLockToggle);

            _btnOpenWeb = new ModernButton();
            _btnOpenWeb.Text = "🌐 Vào Web";
            _btnOpenWeb.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            _btnOpenWeb.NormalColor = colorGreen;
            _btnOpenWeb.HoverColor = Color.FromArgb(52, 211, 153);
            _btnOpenWeb.PressedColor = Color.FromArgb(5, 150, 105);
            _btnOpenWeb.BorderColor = Color.Transparent;
            _btnOpenWeb.ForeColor = Color.White;
            _btnOpenWeb.CornerRadius = 6;
            _btnOpenWeb.Location = new Point(195, 422);
            _btnOpenWeb.Size = new Size(125, 32);
            _btnOpenWeb.Visible = false;
            _btnOpenWeb.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_deployedUrl))
                {
                    try { System.Diagnostics.Process.Start(_deployedUrl); } catch { }
                }
            };
            this.Controls.Add(_btnOpenWeb);

            _btnDeployFresh = new ModernButton();
            _btnDeployFresh.Text = "Chỉ Database";
            _btnDeployFresh.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            _btnDeployFresh.NormalColor = Color.White;
            _btnDeployFresh.HoverColor = Color.FromArgb(240, 253, 244);
            _btnDeployFresh.PressedColor = Color.FromArgb(220, 252, 231);
            _btnDeployFresh.BorderColor = colorGreen;
            _btnDeployFresh.ForeColor = colorGreen;
            _btnDeployFresh.CornerRadius = 6;
            _btnDeployFresh.Location = new Point(298, 422);
            _btnDeployFresh.Size = new Size(155, 32);
            _btnDeployFresh.Click += (s, e) => StartDeploy(true);
            // NOT adding to Controls to keep it hidden, avoiding overlapping/cut issues.

            _btnCleanup = new ModernButton();
            _btnCleanup.Text = "Dọn dẹp";
            _btnCleanup.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            _btnCleanup.NormalColor = Color.White;
            _btnCleanup.HoverColor = Color.FromArgb(254, 242, 242);
            _btnCleanup.PressedColor = Color.FromArgb(254, 226, 226);
            _btnCleanup.BorderColor = Color.FromArgb(248, 113, 113);
            _btnCleanup.ForeColor = Color.FromArgb(239, 68, 68);
            _btnCleanup.CornerRadius = 6;
            _btnCleanup.Location = new Point(460, 422);
            _btnCleanup.Size = new Size(125, 32);
            _btnCleanup.Click += (s, e) => StartCleanup();
            this.Controls.Add(_btnCleanup);

            LoadConfig();
            UpdateDeployButtonsState();
        }

        private void BuildConfigPanel()
        {
            Color inputBg = Color.White;                        // Nền trắng tinh mượt mà, đồng nhất 100% với Card
            Color inputBorder = Color.FromArgb(203, 213, 225);  // Viền Slate 300
            Color inputFocusBorder = colorPurple;
            Color labelColor = colorText;

            // ── Custom styled input helper for Database Name ───────────
            var lbl = new Label();
            lbl.Text = "Tên Database muốn thay đổi (Thay cho database sinh tự động)";
            lbl.ForeColor = labelColor;
            lbl.Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            lbl.Location = new Point(20, 16);
            lbl.Size = new Size(350, 15);
            _pnlConfig.Controls.Add(lbl);

            bool isFocused = false;
            var wrap = new Panel();
            wrap.Location = new Point(20, 34);
            wrap.Size = new Size(320, 32);
            wrap.BackColor = inputBg;
            wrap.Padding = new Padding(8, 7, 8, 7); // Padding giúp TextBox luôn nằm chính giữa, không lệch viền
            wrap.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color bc = isFocused ? inputFocusBorder : inputBorder;
                
                // Vẽ viền bo tròn 6px bằng GraphicsPath khớp 100% cực kỳ sắc nét, tránh hoàn toàn lỗi lồi viền xám hay nham nhở Region
                using (var pen = new Pen(bc, 1.2f))
                using (var path = GetRoundedRect(new Rectangle(0, 0, wrap.Width - 1, wrap.Height - 1), 6))
                {
                    pe.Graphics.DrawPath(pen, path);
                }
                
                if (isFocused)
                {
                    using (var pen = new Pen(Color.FromArgb(40, inputFocusBorder.R, inputFocusBorder.G, inputFocusBorder.B), 2.5f))
                    using (var path = GetRoundedRect(new Rectangle(1, 1, wrap.Width - 3, wrap.Height - 3), 5))
                    {
                        pe.Graphics.DrawPath(pen, path);
                    }
                }
            };

            _txtDbName = new TextBox();
            _txtDbName.BorderStyle = BorderStyle.None;
            _txtDbName.Dock = DockStyle.Fill; // TextBox vừa khít hoàn toàn bên trong wrap panel
            _txtDbName.BackColor = inputBg;
            _txtDbName.ForeColor = Color.FromArgb(30, 41, 59);
            _txtDbName.Font = new Font("Segoe UI", 9.5f);
            _txtDbName.Enter += (s, e2) => { isFocused = true; wrap.Invalidate(); };
            _txtDbName.Leave += (s, e2) => { isFocused = false; wrap.Invalidate(); };

            string siteName = Path.GetFileName(_projectDir);
            string category = "";
            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            if (!string.IsNullOrEmpty(_projectDir) && _projectDir.StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase))
            {
                string rel = _projectDir.Substring(wwwDir.Length).TrimStart('\\', '/');
                string[] parts = rel.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) { category = parts[0]; siteName = parts[parts.Length - 1]; }
                else if (parts.Length == 1) { siteName = parts[0]; }
            }
            string defaultSuffix = GenerateDemoDbSuffix(category, siteName);

            if (_txtDbName.IsHandleCreated) {
                SendMessage(_txtDbName.Handle, EM_SETCUEBANNER, 0, defaultSuffix);
            } else {
                _txtDbName.HandleCreated += (s, e) => {
                    SendMessage(_txtDbName.Handle, EM_SETCUEBANNER, 0, defaultSuffix);
                };
            }

            wrap.Controls.Add(_txtDbName);
            wrap.Click += (s, e2) => _txtDbName.Focus();
            _pnlConfig.Controls.Add(wrap);
            ApplyRoundedRegion(wrap, 6);

            // SSL custom toggle
            bool sslChecked = false;
            Panel pnlSslToggle = new Panel();
            pnlSslToggle.Location = new Point(370, 24);
            pnlSslToggle.Size = new Size(180, 24);
            pnlSslToggle.BackColor = Color.Transparent;
            pnlSslToggle.Cursor = Cursors.Hand;
            pnlSslToggle.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color track = sslChecked ? colorPurple : Color.FromArgb(203, 213, 225);
                using (var br = new SolidBrush(track))
                using (var path = GetRoundedRect(new Rectangle(0, 4, 38, 16), 8))
                    pe.Graphics.FillPath(br, path);
                int kx = sslChecked ? 21 : 4;
                using (var br = new SolidBrush(Color.White))
                    pe.Graphics.FillEllipse(br, kx, 6, 12, 12);
                using (var font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    pe.Graphics.DrawString("Kích hoạt SSL (https)", font, br, new PointF(44f, 4f));
            };
            pnlSslToggle.Click += (s, e) =>
            {
                sslChecked = !sslChecked;
                _chkSsl.Checked = sslChecked;
                pnlSslToggle.Invalidate();
            };
            _pnlConfig.Controls.Add(pnlSslToggle);

            Panel pnlOnlyDbToggle = new Panel();
            pnlOnlyDbToggle.Location = new Point(370, 56);
            pnlOnlyDbToggle.Size = new Size(180, 24);
            pnlOnlyDbToggle.BackColor = Color.Transparent;
            pnlOnlyDbToggle.Cursor = Cursors.Hand;
            pnlOnlyDbToggle.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color track = _onlyDbChecked ? colorPurple : Color.FromArgb(203, 213, 225);
                using (var br = new SolidBrush(track))
                using (var path = GetRoundedRect(new Rectangle(0, 4, 38, 16), 8))
                    pe.Graphics.FillPath(br, path);
                int kx = _onlyDbChecked ? 21 : 4;
                using (var br = new SolidBrush(Color.White))
                    pe.Graphics.FillEllipse(br, kx, 6, 12, 12);
                using (var font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    pe.Graphics.DrawString("Chỉ deploy Database", font, br, new PointF(44f, 4f));
            };
            pnlOnlyDbToggle.Click += (s, e) =>
            {
                _onlyDbChecked = !_onlyDbChecked;
                pnlOnlyDbToggle.Invalidate();
            };
            _pnlConfig.Controls.Add(pnlOnlyDbToggle);

            _chkSsl = new CheckBox();
            _chkSsl.Visible = false;
            _chkSsl.CheckedChanged += (s, e) => { sslChecked = _chkSsl.Checked; pnlSslToggle.Invalidate(); };
            _pnlConfig.Controls.Add(_chkSsl);

            // 7-Zip custom toggle
            Panel pnl7zToggle = new Panel();
            pnl7zToggle.Location = new Point(370, 84);
            pnl7zToggle.Size = new Size(180, 24);
            pnl7zToggle.BackColor = Color.Transparent;
            pnl7zToggle.Cursor = Cursors.Hand;
            pnl7zToggle.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color track = _use7zipChecked ? colorPurple : Color.FromArgb(203, 213, 225);
                using (var br = new SolidBrush(track))
                using (var path = GetRoundedRect(new Rectangle(0, 4, 38, 16), 8))
                    pe.Graphics.FillPath(br, path);
                int kx = _use7zipChecked ? 21 : 4;
                using (var br = new SolidBrush(Color.White))
                    pe.Graphics.FillEllipse(br, kx, 6, 12, 12);
                using (var font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    pe.Graphics.DrawString("Nén bằng 7-Zip", font, br, new PointF(44f, 4f));
            };
            pnl7zToggle.Click += (s, e) =>
            {
                _use7zipChecked = !_use7zipChecked;
                pnl7zToggle.Invalidate();
                try {
                    var cfg = LoadGlobalConfig();
                    cfg["use_7zip"] = _use7zipChecked ? "true" : "false";
                    SaveGlobalConfig(cfg);
                } catch {}
            };
            _pnlConfig.Controls.Add(pnl7zToggle);

            // Hint label
            Label lblHint = new Label();
            lblHint.Text = "* Để trống tên Database nếu muốn sử dụng tên mặc định theo chuẩn.";
            lblHint.ForeColor = colorDim;
            lblHint.Font = new Font("Segoe UI Italic", 8f);
            lblHint.Location = new Point(20, 76);
            lblHint.Size = new Size(320, 18);
            _pnlConfig.Controls.Add(lblHint);
        }

        private void LoadConfig()
        {
            var cfg = LoadGlobalConfig();
            if (cfg.ContainsKey("ftp_host"))   _txtFtpHost.Text   = cfg["ftp_host"];
            if (cfg.ContainsKey("ftp_user"))   _txtFtpUser.Text   = cfg["ftp_user"];
            if (cfg.ContainsKey("ftp_pass"))   _txtFtpPass.Text   = cfg["ftp_pass"];
            if (cfg.ContainsKey("ftp_root"))   _txtFtpRoot.Text   = cfg["ftp_root"];
            else if (string.IsNullOrEmpty(_txtFtpRoot.Text)) _txtFtpRoot.Text = "/public_html";
            if (cfg.ContainsKey("web_domain")) _txtWebDomain.Text = cfg["web_domain"];
            if (cfg.ContainsKey("da_user"))    _txtDaUser.Text    = cfg["da_user"];
            if (cfg.ContainsKey("da_port"))    _txtDaPort.Text    = cfg["da_port"];

            _txtDbName.Text = GetProjectDb(_projectDir);
            _chkSsl.Checked = GetProjectSsl(_projectDir);
            _use7zipChecked = cfg.ContainsKey("use_7zip") && cfg["use_7zip"] == "true";
        }

        private void SaveConfig()
        {
            try {
                SetProjectSsl(_projectDir, _chkSsl.Checked);
                SetProjectDb(_projectDir, _txtDbName.Text.Trim());
                var cfg = LoadGlobalConfig();
                cfg["use_7zip"] = _use7zipChecked ? "true" : "false";
                SaveGlobalConfig(cfg);
            } catch { }
        }

        private void UpdateDeployButtonsState()
        {
            if (_isLocked)
            {
                _btnDeploy.Enabled = false;
                _btnDeployFresh.Enabled = false;
                _lblApiStatus.Text = "Dự án đã khóa deploy";
                _lblApiStatus.ForeColor = colorRed;

                if (_btnLockToggle != null)
                {
                    _btnLockToggle.Text = "🔓 Mở khóa";
                    _btnLockToggle.NormalColor = Color.White;
                    _btnLockToggle.HoverColor = Color.FromArgb(240, 253, 244);
                    _btnLockToggle.PressedColor = Color.FromArgb(220, 252, 231);
                    _btnLockToggle.BorderColor = colorGreen;
                    _btnLockToggle.ForeColor = colorGreen;
                }

                if (_btnOpenWeb != null)
                {
                    string scheme = _chkSsl.Checked ? "https://" : "http://";
                    string relWebPath = _sitePath.Replace('\\', '/').TrimStart('/');
                    _deployedUrl = scheme + _txtWebDomain.Text.Trim() + "/" + relWebPath + "/";
                    _btnOpenWeb.Visible = true;
                    _btnDeploy.Visible = false;
                }
            }
            else
            {
                _btnDeploy.Enabled = true;
                _btnDeployFresh.Enabled = true;
                _lblApiStatus.Text = "Sẵn sàng deploy";
                _lblApiStatus.ForeColor = colorDim;

                if (_btnLockToggle != null)
                {
                    _btnLockToggle.Text = "🔒 Khóa";
                    _btnLockToggle.NormalColor = Color.White;
                    _btnLockToggle.HoverColor = Color.FromArgb(254, 242, 242);
                    _btnLockToggle.PressedColor = Color.FromArgb(254, 226, 226);
                    _btnLockToggle.BorderColor = Color.FromArgb(248, 113, 113);
                    _btnLockToggle.ForeColor = Color.FromArgb(239, 68, 68);
                }

                if (_btnOpenWeb != null)
                {
                    _btnOpenWeb.Visible = false;
                    _btnDeploy.Visible = true;
                }
            }
            if (_btnLockToggle != null) _btnLockToggle.Invalidate();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private class FileLongPathInfo
        {
            public string FullPath;
            public long Length;
        }

        private static void GetFilesAndDirsLongPath(string dir, List<FileLongPathInfo> fileList, List<string> dirList)
        {
            string searchPath = dir;
            if (!searchPath.StartsWith(@"\\?\"))
            {
                searchPath = @"\\?\" + Path.GetFullPath(searchPath);
            }
            string searchFilter = searchPath.TrimEnd('\\') + @"\*";

            WIN32_FIND_DATA findData;
            IntPtr hFind = FindFirstFile(searchFilter, out findData);
            if (hFind != INVALID_HANDLE_VALUE)
            {
                try
                {
                    do
                    {
                        if (findData.cFileName == "." || findData.cFileName == "..")
                            continue;

                        string cleanDir = dir.EndsWith("\\") ? dir : (dir + "\\");
                        string fullPath = cleanDir + findData.cFileName;
                        bool isDir = (findData.dwFileAttributes & 0x10) != 0; // FILE_ATTRIBUTE_DIRECTORY

                        if (isDir)
                        {
                            dirList.Add(fullPath);
                            GetFilesAndDirsLongPath(fullPath, fileList, dirList);
                        }
                        else
                        {
                            long fileSize = ((long)findData.nFileSizeHigh << 32) + findData.nFileSizeLow;
                            fileList.Add(new FileLongPathInfo { FullPath = fullPath, Length = fileSize });
                        }
                    } while (FindNextFile(hFind, out findData));
                }
                finally
                {
                    FindClose(hFind);
                }
            }
        }

        private static FileStream OpenFileLongPath(string filePath)
        {
            string longPath = filePath;
            if (!longPath.StartsWith(@"\\?\"))
            {
                longPath = @"\\?\" + Path.GetFullPath(longPath);
            }

            Microsoft.Win32.SafeHandles.SafeFileHandle handle = CreateFile(
                longPath,
                0x80000000, // GENERIC_READ
                1,          // FILE_SHARE_READ
                IntPtr.Zero,
                3,          // OPEN_EXISTING
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return new FileStream(handle, FileAccess.Read);
        }

        private static void SafeCreateZipFromDirectory(string sourceDir, string zipPath, System.IO.Compression.CompressionLevel level, Action<long, long, int, int> progressCallback = null)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            
            List<FileLongPathInfo> files = new List<FileLongPathInfo>();
            List<string> dirs = new List<string>();
            GetFilesAndDirsLongPath(sourceDir, files, dirs);
            
            int totalFiles = files.Count;
            long totalBytes = 0;
            foreach (var f in files)
            {
                totalBytes += f.Length;
            }

            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create))
            {
                string absSourceDir = Path.GetFullPath(sourceDir);
                if (!absSourceDir.StartsWith(@"\\?\"))
                {
                    absSourceDir = @"\\?\" + absSourceDir;
                }
                int stripLength = absSourceDir.Length;
                if (!absSourceDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    stripLength++;
                }

                long processedBytes = 0;
                int processedFiles = 0;
                DateTime lastLogTime = DateTime.MinValue;

                foreach (var fileInfo in files)
                {
                    string file = fileInfo.FullPath;
                    string relativePath = file.Substring(stripLength);
                    string entryName = relativePath.Replace('\\', '/');
                    
                    var entry = archive.CreateEntry(entryName, level);
                    using (var entryStream = entry.Open())
                    using (var fileStream = OpenFileLongPath(file))
                    {
                        fileStream.CopyTo(entryStream);
                        processedBytes += fileInfo.Length;
                    }

                    processedFiles++;

                    if (progressCallback != null && ((DateTime.Now - lastLogTime).TotalMilliseconds > 300 || processedFiles == totalFiles))
                    {
                        progressCallback(processedBytes, totalBytes, processedFiles, totalFiles);
                        lastLogTime = DateTime.Now;
                    }
                }

                foreach (var dir in dirs)
                {
                    bool hasContents = false;
                    string dirWithSlash = dir.TrimEnd('\\') + "\\";
                    foreach (var f in files)
                    {
                        if (f.FullPath.StartsWith(dirWithSlash, StringComparison.OrdinalIgnoreCase))
                        {
                            hasContents = true;
                            break;
                        }
                    }
                    if (!hasContents)
                    {
                        foreach (var d in dirs)
                        {
                            if (d != dir && d.StartsWith(dirWithSlash, StringComparison.OrdinalIgnoreCase))
                            {
                                hasContents = true;
                                break;
                            }
                        }
                    }

                    if (!hasContents)
                    {
                        string relativePath = dir.Substring(stripLength);
                        string entryName = relativePath.Replace('\\', '/') + "/";
                        archive.CreateEntry(entryName);
                    }
                }
            }
        }

        private void StartDeploy(bool onlyDb)
        {
            if (string.IsNullOrWhiteSpace(_txtFtpHost.Text) || string.IsNullOrWhiteSpace(_txtFtpUser.Text) || string.IsNullOrWhiteSpace(_txtFtpPass.Text))
            {
                MessageBox.Show("Vui lòng cấu hình đầy đủ thông tin FTP Host, User, Pass trên tab Cài đặt hệ thống trước khi deploy.", "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveConfig();
            _logBox.Clear();
            AppendLog(onlyDb ? "🚀 Bắt đầu deploy Database..." : "🚀 Đang khởi động deploy toàn bộ...", Color.FromArgb(139, 92, 246));

            _btnDeploy.Enabled = false;
            _btnDeployFresh.Enabled = false;
            _btnCleanup.Enabled = false;
            _lblApiStatus.Text = "Đang xử lý...";
            _lblApiStatus.ForeColor = Color.FromArgb(245, 158, 11);

            string ftpHost = _txtFtpHost.Text.Trim();
            string ftpUser = _txtFtpUser.Text.Trim();
            string ftpPass = _txtFtpPass.Text.Trim();
            string ftpRoot = string.IsNullOrWhiteSpace(_txtFtpRoot.Text) ? "/public_html" : _txtFtpRoot.Text.Trim();
            string webDomain = _txtWebDomain.Text.Trim();
            string daUser = _txtDaUser.Text.Trim();
            string daPort = string.IsNullOrWhiteSpace(_txtDaPort.Text) ? "2222" : _txtDaPort.Text.Trim();
            bool useSSL = _chkSsl.Checked;
            string sitePath = _sitePath;

            string siteName = Path.GetFileName(_projectDir);
            string category = "";
            string wwwDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            if (!string.IsNullOrEmpty(_projectDir) && _projectDir.StartsWith(wwwDir, StringComparison.OrdinalIgnoreCase))
            {
                string rel = _projectDir.Substring(wwwDir.Length).TrimStart('\\', '/');
                string[] parts = rel.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) { category = parts[0]; siteName = parts[parts.Length - 1]; }
                else if (parts.Length == 1) { siteName = parts[0]; }
            }

            System.Threading.Thread worker = new System.Threading.Thread(() => {
                string jobId = DateTime.Now.ToString("yyyyMMddHHmmss");
                string baseAppDir = AppDomain.CurrentDomain.BaseDirectory;
                string tempDir = Path.Combine(baseAppDir, "tmp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                
                string zipPath = Path.Combine(tempDir, "dist_" + jobId + ".zip");
                string sqlPath = Path.Combine(tempDir, "dist_" + jobId + ".sql");

                try {
                    // Step 1: Zip local source if NOT only DB
                    if (!onlyDb)
                    {
                        string exe7z = @"C:\Program Files\7-Zip\7z.exe";
                        if (!File.Exists(exe7z)) {
                            exe7z = @"C:\Program Files (x86)\7-Zip\7z.exe";
                        }
                        bool has7z = File.Exists(exe7z);

                        if (_use7zipChecked && has7z)
                        {
                            AppendLog("📦 Đang nén mã nguồn bằng 7-Zip (Siêu nhanh)...", colorText);
                            if (File.Exists(zipPath)) File.Delete(zipPath);
                            
                            using (var proc = new System.Diagnostics.Process())
                            {
                                proc.StartInfo.FileName = exe7z;
                                proc.StartInfo.Arguments = string.Format("a -tzip -mx=1 \"{0}\" *", zipPath);
                                proc.StartInfo.WorkingDirectory = _projectDir;
                                proc.StartInfo.UseShellExecute = false;
                                proc.StartInfo.CreateNoWindow = true;
                                proc.StartInfo.RedirectStandardError = true;
                                proc.StartInfo.RedirectStandardOutput = true;
                                
                                proc.Start();
                                
                                // Poll zip size on disk while 7z is running to show progress
                                while (!proc.HasExited)
                                {
                                    System.Threading.Thread.Sleep(300);
                                    if (File.Exists(zipPath))
                                    {
                                        try
                                        {
                                            long len = new FileInfo(zipPath).Length;
                                            if (len > 0)
                                            {
                                                AppendLog(string.Format("   -> Dung lượng file nén: {0:N2} MB...", len / 1024.0 / 1024.0), colorDim);
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                proc.WaitForExit();
                                
                                if (proc.ExitCode != 0)
                                {
                                    string err = proc.StandardError.ReadToEnd();
                                    string outStr = proc.StandardOutput.ReadToEnd();
                                    throw new Exception("7-Zip nén thất bại với lỗi code: " + proc.ExitCode + "\n" + err + "\n" + outStr);
                                }
                            }
                            AppendLog("✅ Nén 7-Zip xong: " + Math.Round(new FileInfo(zipPath).Length / 1024.0 / 1024.0, 2) + " MB", colorGreen);
                        }
                        else
                        {
                            if (_use7zipChecked && !has7z)
                            {
                                AppendLog("⚠️ Không tìm thấy 7-Zip trên hệ thống! Tự động nén bằng C# mặc định...", Color.FromArgb(245, 158, 11));
                            }
                            AppendLog("📦 Đang nén mã nguồn bằng C# mặc định...", colorText);
                            if (File.Exists(zipPath)) File.Delete(zipPath);
                            
                            SafeCreateZipFromDirectory(_projectDir, zipPath, System.IO.Compression.CompressionLevel.Fastest, (bytes, totalBytes, files, totalFiles) => {
                                string msg = string.Format("   -> Tiến trình nén: {0}/{1} tệp ({2:N2} MB / {3:N2} MB)...", 
                                    files, 
                                    totalFiles,
                                    bytes / 1024.0 / 1024.0, 
                                    totalBytes / 1024.0 / 1024.0);
                                AppendLog(msg, colorDim);
                            });
                            
                            AppendLog("✅ Nén xong: " + Math.Round(new FileInfo(zipPath).Length / 1024.0 / 1024.0, 2) + " MB", colorGreen);
                        }
                    }
                    else
                    {
                        AppendLog("♻️ Chỉ cập nhật database: bỏ qua bước nén mã nguồn.", colorDim);
                    }

                    // Step 2: Export DB from local project .env
                    string localEnvPath = Path.Combine(_projectDir, ".env");
                    string dbHost = "localhost";
                    string dbPort = "3306";
                    string dbDatabase = "";
                    string dbUser = "root";
                    string dbPass = "";
                    
                    if (File.Exists(localEnvPath))
                    {
                        foreach (string line in File.ReadAllLines(localEnvPath))
                        {
                            string trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                            int idx = trimmed.IndexOf('=');
                            if (idx <= 0) continue;
                            string key = trimmed.Substring(0, idx).Trim().ToUpper();
                            string val = trimmed.Substring(idx + 1).Trim().Trim('"', '\'');
                            
                            if (key == "DB_HOST") dbHost = val;
                            else if (key == "DB_PORT") dbPort = val;
                            else if (key == "DB_DATABASE" || key == "DB_NAME") dbDatabase = val;
                            else if (key == "DB_USERNAME" || key == "DB_USER") dbUser = val;
                            else if (key == "DB_PASSWORD" || key == "DB_PASS") dbPass = val;
                        }
                    }

                    if (string.IsNullOrEmpty(dbDatabase))
                    {
                        AppendLog("⚠️ Không tìm thấy DB_DATABASE trong .env – bỏ qua export SQL", Color.FromArgb(245, 158, 11));
                    }
                    else
                    {
                        AppendLog("🗄️ Đang export database " + dbDatabase + "...", colorText);
                        
                        // Dynamic local mysqldump search
                        string mysqlDir = Path.Combine(baseAppDir, "bin", "mysql");
                        string mysqldumpPath = "mysqldump";
                        if (Directory.Exists(mysqlDir))
                        {
                            foreach (var dir in Directory.GetDirectories(mysqlDir))
                            {
                                string target = Path.Combine(dir, "bin", "mysqldump.exe");
                                if (File.Exists(target))
                                {
                                    mysqldumpPath = "\"" + target + "\"";
                                    break;
                                }
                            }
                        }

                        string finalDbPort = !string.IsNullOrEmpty(dbPort) ? dbPort : GetActiveMySqlPort();
                        string portPart = string.IsNullOrEmpty(finalDbPort) ? "" : ("--port=" + finalDbPort);
                        string passPart = string.IsNullOrEmpty(dbPass) ? "" : ("-p\"" + dbPass + "\"");
                        
                        string dumpCmd = string.Format("{0} --host={1} {2} --user={3} {4} --default-character-set=utf8mb4 --result-file=\"{5}\" {6}", 
                                                       mysqldumpPath, dbHost, portPart, dbUser, passPart, sqlPath, dbDatabase);
                        
                        string dumpOut;
                        int dumpRet = RunCommand(dumpCmd, out dumpOut);
                        if (dumpRet != 0)
                        {
                            AppendLog("⚠️ mysqldump lỗi (" + dumpRet + "), thử local fallback...", Color.FromArgb(245, 158, 11));
                            if (!string.IsNullOrEmpty(dumpOut))
                            {
                                AppendLog("  Chi tiết: " + dumpOut, Color.FromArgb(245, 158, 11));
                            }
                            ExportDbPdo(dbHost, dbDatabase, dbUser, dbPass, sqlPath);
                        }
                        if (File.Exists(sqlPath))
                            AppendLog("✅ Export SQL thành công: " + Math.Round(new FileInfo(sqlPath).Length / 1024.0, 1) + " KB", colorGreen);
                    }

                    // Step 3: Define FTP parameters and upload bridge.php early for DB check
                    string ftpBase = "ftp://" + ftpHost + "/" + (ftpRoot.TrimStart('/') + "/" + sitePath.Replace('\\', '/').TrimStart('/')).Replace("//", "/").TrimEnd('/') + "/";
                    string ftpCreds = ftpUser + ":" + ftpPass;

                    string bridgePath = ConfigHelper.GetDataFilePath("bridge.php");
                    if (File.Exists(bridgePath))
                    {
                        try
                        {
                            string content = File.ReadAllText(bridgePath);
                            if (!content.Contains("deployDb"))
                            {
                                File.Delete(bridgePath);
                            }
                        }
                        catch { }
                    }
                    if (!File.Exists(bridgePath)) bridgePath = Path.Combine(baseAppDir, "www", "rambowoon_manager", "bridge.php");
                    if (!File.Exists(bridgePath)) bridgePath = Path.Combine(baseAppDir, "bridge.php");
                    if (File.Exists(bridgePath))
                    {
                        AppendLog("  ↑ Uploading bridge.php for database checks...", colorDim);
                        string _ignoredBridgeErr;
                        UploadFtp(ftpBase + "bridge.php", ftpCreds, bridgePath, out _ignoredBridgeErr);
                    }

                    string finalDbName = "";
                    string finalDbPass = "";
                    bool clearDb = false;
                    bool skipImportDb = false;

                    string mainUser = daUser;
                    if (string.IsNullOrEmpty(mainUser))
                    {
                        mainUser = ftpUser;
                        if (mainUser.Contains("_"))
                        {
                            mainUser = mainUser.Split('_')[0];
                        }
                    }
                    string dbSuffix = _txtDbName != null && !string.IsNullOrWhiteSpace(_txtDbName.Text) ? _txtDbName.Text.Trim() : GenerateDemoDbSuffix(category, siteName);
                    finalDbName = mainUser + "_" + dbSuffix;

                    // Download remote .env to read current password
                    string envDbName = "";
                    string envDbUser = "";
                    string envDbPass = "";
                    string remoteEnvTemp = Path.Combine(tempDir, "remote_env_check_" + jobId);
                    string remoteEnvUrl = ftpBase + ".env";
                    string ftpErrCheck;
                    bool hasRemoteEnv = DownloadFtp(remoteEnvUrl, ftpCreds, remoteEnvTemp, out ftpErrCheck);
                    if (hasRemoteEnv && File.Exists(remoteEnvTemp))
                    {
                        foreach (string line in File.ReadAllLines(remoteEnvTemp))
                        {
                            string trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                            int idx = trimmed.IndexOf('=');
                            if (idx <= 0) continue;
                            string key = trimmed.Substring(0, idx).Trim().ToUpper();
                            string val = trimmed.Substring(idx + 1).Trim().Trim('"', '\'');
                            if (key == "DB_DATABASE" || key == "DB_NAME") envDbName = val;
                            else if (key == "DB_USERNAME" || key == "DB_USER") envDbUser = val;
                            else if (key == "DB_PASSWORD" || key == "DB_PASS") envDbPass = val;
                        }
                        File.Delete(remoteEnvTemp);
                    }

                    string targetDbName = !string.IsNullOrEmpty(envDbName) ? envDbName : finalDbName;
                    string targetDbUser = !string.IsNullOrEmpty(envDbUser) ? envDbUser : finalDbName;
                    string targetDbPass = !string.IsNullOrEmpty(envDbPass) ? envDbPass : "";

                    bool dbCheckSuccess = false;
                    bool dbHasData = false;
                    string scheme = useSSL ? "https://" : "http://";
                    string relWebPath = sitePath.Replace('\\', '/').TrimStart('/');
                    string checkDbUrl = scheme + webDomain + "/" + relWebPath + "/bridge.php?action=checkDb";

                    if (!string.IsNullOrEmpty(targetDbPass))
                    {
                        AppendLog("🔍 Đang kiểm tra kết nối Database...", colorText);
                        string checkDbConfig = "{\"host\":\"localhost\",\"name\":\"" + targetDbName.Replace("\\", "\\\\").Replace("\"", "\\\"") + 
                                               "\",\"user\":\"" + targetDbUser.Replace("\\", "\\\\").Replace("\"", "\\\"") + 
                                               "\",\"pass\":\"" + targetDbPass.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
                        string checkPostData = "db_config=" + Uri.EscapeDataString(checkDbConfig);
                        string checkRes = PostHttp(checkDbUrl, checkPostData);
                        
                        if (!string.IsNullOrEmpty(checkRes) && checkRes.Contains("\"status\":\"success\""))
                        {
                            dbCheckSuccess = true;
                            dbHasData = System.Text.RegularExpressions.Regex.IsMatch(checkRes, @"""has_data""\s*:\s*true", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            finalDbPass = targetDbPass;
                            AppendLog("✅ Kết nối Database thành công.", colorGreen);
                        }
                        else
                        {
                            AppendLog("⚠️ Kết nối Database thất bại: " + (checkRes ?? "không phản hồi"), Color.FromArgb(245, 158, 11));
                        }
                    }

                    if (!dbCheckSuccess)
                    {
                        // Wrong password or env doesn't have it
                        AppendLog("🔑 Cấu hình sai mật khẩu hoặc chưa có Database. Tiến hành tự động đổi mật khẩu và đồng bộ lại...", Color.FromArgb(245, 158, 11));
                        
                        string passwordToSet = "";
                        if (!string.IsNullOrEmpty(envDbPass))
                        {
                            passwordToSet = envDbPass;
                            AppendLog("👉 Lấy mật khẩu cũ từ file .env: " + passwordToSet, colorDim);
                        }
                        else
                        {
                            passwordToSet = GenerateRandomDbPass();
                            AppendLog("👉 File .env không có mật khẩu, tự random mật khẩu mới: " + passwordToSet, colorDim);
                        }

                        // Create database / sync password via DirectAdmin API
                        if (!onlyDb)
                        {
                            AppendLog("🛠️ Đang khởi tạo/đồng bộ Database trên DirectAdmin...", colorText);
                            string dbPostData = "action=create&name=" + Uri.EscapeDataString(dbSuffix) + 
                                               "&user=" + Uri.EscapeDataString(dbSuffix) + 
                                               "&passwd=" + Uri.EscapeDataString(passwordToSet) + 
                                               "&passwd2=" + Uri.EscapeDataString(passwordToSet) + 
                                               "&create=Create";
                            
                            AppendLog("  DA API: Gửi yêu cầu tạo database " + finalDbName + "...", colorDim);
                            string daRes = CallDirectAdminDbApi(ftpHost, daPort, mainUser, ftpPass, dbPostData);
                            string decodedDaRes = Uri.UnescapeDataString(daRes);
                            // AppendLog("  DA API Response: " + decodedDaRes, colorDim);

                            if (daRes.StartsWith("error:") && !daRes.Contains("500") && (daRes.Contains("Unauthorized") || daRes.Contains("Access denied") || daRes.Contains("Forbidden")))
                            {
                                AppendLog("❌ DA API Lỗi kết nối/xác thực (Vui lòng kiểm tra lại tài khoản DA PORT/DA USER): " + decodedDaRes, colorRed);
                            }
                            else
                            {
                                bool isAlreadyExists = decodedDaRes.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                                        decodedDaRes.IndexOf("already user", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                        decodedDaRes.IndexOf("user already exist", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                        daRes.Contains("500");

                                if (isAlreadyExists)
                                {
                                    if (daRes.Contains("500"))
                                    {
                                        AppendLog("⚠️ DA API phản hồi lỗi 500 khi tạo DB (có thể do DB đã tồn tại). Thử đồng bộ password...", Color.FromArgb(245, 158, 11));
                                    }
                                    else
                                    {
                                        AppendLog("  DA API: Database đã tồn tại, tiến hành đồng bộ password...", Color.FromArgb(245, 158, 11));
                                    }
                                    string dbModifyPostData = "action=modify&database=" + Uri.EscapeDataString(targetDbName) + 
                                                             "&user=" + Uri.EscapeDataString(targetDbUser) + 
                                                             "&passwd=" + Uri.EscapeDataString(passwordToSet) + 
                                                             "&passwd2=" + Uri.EscapeDataString(passwordToSet);
                                    string daPassRes = CallDirectAdminDbApi(ftpHost, daPort, mainUser, ftpPass, dbModifyPostData, "CMD_API_DB_USER");
                                    string decodedDaPassRes = Uri.UnescapeDataString(daPassRes);
                                    // AppendLog("  DA API Modify Response: " + decodedDaPassRes, colorDim);

                                    if (daPassRes.StartsWith("error:") || daPassRes.Contains("Unauthorized") || daPassRes.Contains("Access denied"))
                                    {
                                        AppendLog("❌ DA API Lỗi kết nối khi đồng bộ password: " + decodedDaPassRes, colorRed);
                                    }
                                    else if (daPassRes.Contains("error=1") || !daPassRes.Contains("error=0"))
                                    {
                                        AppendLog("❌ DA API Lỗi đồng bộ password: " + decodedDaPassRes, colorRed);
                                    }
                                    else
                                     {
                                        AppendLog("✅ DA API: Đồng bộ password thành công.", colorGreen);
                                    }
                                }
                                else if (decodedDaRes.IndexOf("error=1", StringComparison.OrdinalIgnoreCase) >= 0 && decodedDaRes.IndexOf("exists", StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    AppendLog("⚠️ DA API Cảnh báo: " + decodedDaRes, Color.FromArgb(245, 158, 11));
                                }
                                else
                                {
                                    AppendLog("✅ DA API: Tạo Database thành công.", colorGreen);
                                }
                            }
                        }
                        else
                        {
                            // onlyDb is true, but password is wrong: sync password only
                            if (string.IsNullOrEmpty(targetDbUser) || targetDbUser == "root")
                            {
                                AppendLog("⚠️ User database là 'root' hoặc trống (không hợp lệ trên hosting). Bỏ qua đổi mật khẩu qua DirectAdmin API.", Color.FromArgb(245, 158, 11));
                            }
                            else
                            {
                                AppendLog("  DA API: Đang đổi mật khẩu của user \"" + targetDbUser + "\" thành mật khẩu đồng bộ...", colorDim);
                                string dbModifyPostData = "action=modify&database=" + Uri.EscapeDataString(targetDbName) + 
                                                         "&user=" + Uri.EscapeDataString(targetDbUser) + 
                                                         "&passwd=" + Uri.EscapeDataString(passwordToSet) + 
                                                         "&passwd2=" + Uri.EscapeDataString(passwordToSet);
                                string daPassRes = CallDirectAdminDbApi(ftpHost, daPort, mainUser, ftpPass, dbModifyPostData, "CMD_API_DB_USER");
                                string decodedDaPassRes = Uri.UnescapeDataString(daPassRes);
                                // AppendLog("  DA API Modify Response: " + decodedDaPassRes, colorDim);

                                if (daPassRes.StartsWith("error:") || daPassRes.Contains("Unauthorized") || daPassRes.Contains("Access denied"))
                                {
                                    AppendLog("❌ DA API Lỗi kết nối khi đổi mật khẩu DB: " + decodedDaPassRes, colorRed);
                                }
                                else if (daPassRes.Contains("error=1") || !daPassRes.Contains("error=0"))
                                {
                                    AppendLog("❌ DA API Lỗi đổi mật khẩu DB: " + decodedDaPassRes, colorRed);
                                }
                                else
                                {
                                    AppendLog("✅ DA API: Đổi mật khẩu Database thành công.", colorGreen);
                                }
                            }
                        }

                        finalDbPass = passwordToSet;

                        // Sync back to .env if env password is missing or different
                        if (string.IsNullOrEmpty(envDbPass) || envDbPass != passwordToSet)
                        {
                            try
                            {
                                string remoteEnvTemp2 = Path.Combine(tempDir, "remote_env_write_" + jobId);
                                string remoteEnvUrl2 = ftpBase + ".env";
                                string ftpErr2;
                                List<string> envLines = new List<string>();
                                if (DownloadFtp(remoteEnvUrl2, ftpCreds, remoteEnvTemp2, out ftpErr2) && File.Exists(remoteEnvTemp2))
                                {
                                    envLines.AddRange(File.ReadAllLines(remoteEnvTemp2));
                                    File.Delete(remoteEnvTemp2);
                                }
                                
                                bool updated = false;
                                for (int i = 0; i < envLines.Count; i++)
                                {
                                    if (Regex.IsMatch(envLines[i], @"^\s*DB_PASSWORD\s*=") || Regex.IsMatch(envLines[i], @"^\s*DB_PASS\s*="))
                                    {
                                        string keyName = envLines[i].Split('=')[0].Trim();
                                        envLines[i] = keyName + "=" + passwordToSet;
                                        updated = true;
                                    }
                                }
                                if (!updated)
                                {
                                    envLines.Add("DB_PASSWORD=" + passwordToSet);
                                }
                                
                                File.WriteAllLines(remoteEnvTemp2, envLines.ToArray());
                                UploadFtp(remoteEnvUrl2, ftpCreds, remoteEnvTemp2, out ftpErr2);
                                File.Delete(remoteEnvTemp2);
                                AppendLog("✅ Đã cập nhật mật khẩu mới vào file .env trên hosting.", colorGreen);
                            }
                            catch (Exception ex)
                            {
                                AppendLog("⚠️ Thất bại khi cập nhật file .env: " + ex.Message, Color.FromArgb(245, 158, 11));
                            }
                        }

                        // Check connection again after sync
                        AppendLog("🔎 Đang kiểm tra lại kết nối Database sau khi đổi mật khẩu...", colorText);
                        string checkDbConfig2 = "{\"host\":\"localhost\",\"name\":\"" + targetDbName.Replace("\\", "\\\\").Replace("\"", "\\\"") + 
                                               "\",\"user\":\"" + targetDbUser.Replace("\\", "\\\\").Replace("\"", "\\\"") + 
                                               "\",\"pass\":\"" + finalDbPass.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
                        string checkPostData2 = "db_config=" + Uri.EscapeDataString(checkDbConfig2);
                        string checkRes2 = PostHttp(checkDbUrl, checkPostData2);
                        if (!string.IsNullOrEmpty(checkRes2) && checkRes2.Contains("\"status\":\"success\""))
                        {
                            dbCheckSuccess = true;
                            dbHasData = System.Text.RegularExpressions.Regex.IsMatch(checkRes2, @"""has_data""\s*:\s*true", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            AppendLog("✅ Kết nối Database thành công sau khi đồng bộ mật khẩu. (Có dữ liệu: " + (dbHasData ? "Có" : "Không") + ")", colorGreen);
                        }
                        else
                        {
                            AppendLog("⚠️ Kết nối Database sau khi đổi mật khẩu vẫn thất bại: " + (checkRes2 ?? "không phản hồi"), Color.FromArgb(245, 158, 11));
                        }
                    }

                    // Check if DB already has data and prompt user
                    if (dbHasData)
                    {
                        DialogResult dialogRes = DialogResult.Cancel;
                        this.Invoke((MethodInvoker)delegate {
                            dialogRes = MessageBox.Show(
                                "Database " + finalDbName + " đã có dữ liệu!\n\n- Chọn YES để: Xoá dữ liệu cũ và tiến hành import DB mới.\n- Chọn NO để: Giữ lại dữ liệu cũ (Không import DB) và tiếp tục các bước tiếp theo.\n- Chọn CANCEL để: Hủy bỏ tiến trình deploy.",
                                "Database đã có dữ liệu",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question
                            );
                        });

                        if (dialogRes == DialogResult.Yes)
                        {
                            clearDb = true;
                            AppendLog("👉 Người dùng chọn: Xóa dữ liệu cũ và import database mới.", colorText);
                        }
                        else if (dialogRes == DialogResult.No)
                        {
                            skipImportDb = true;
                            AppendLog("👉 Người dùng chọn: Giữ lại dữ liệu cũ (KHÔNG import DB).", colorText);
                            if (File.Exists(sqlPath))
                            {
                                try { File.Delete(sqlPath); } catch { }
                            }
                        }
                        else
                        {
                            AppendLog("❌ Tiến trình deploy đã bị hủy bởi người dùng.", colorRed);
                            FinishDeploy(false);
                            return;
                        }
                    }

                    // Upload actual distribution package files
                    AppendLog("☁️ Đang upload tệp tin nguồn lên FTP...", colorText);
                    bool uploadOk = true;
                    if (!onlyDb && File.Exists(zipPath))
                    {
                        AppendLog("  ↑ Uploading dist.zip...", colorDim);
                        string uploadErrZip;
                        if (!UploadFtp(ftpBase + "dist.zip", ftpCreds, zipPath, out uploadErrZip))
                        {
                            AppendLog("❌ Upload zip thất bại: " + uploadErrZip, colorRed);
                            uploadOk = false;
                        }
                    }

                    if (!skipImportDb && File.Exists(sqlPath))
                    {
                        AppendLog("  ↑ Uploading dist.sql...", colorDim);
                        string uploadErrSql;
                        if (!UploadFtp(ftpBase + "dist.sql", ftpCreds, sqlPath, out uploadErrSql))
                        {
                            AppendLog("⚠️ Upload SQL thất bại: " + uploadErrSql, Color.FromArgb(245, 158, 11));
                        }
                    }

                    if (!uploadOk) { FinishDeploy(false); return; }
                    AppendLog("✅ Upload hoàn tất.", colorGreen);

                    // Step 4: Trigger bridge to deploy/deployDb
                    string bridgeUrl = scheme + webDomain + "/" + relWebPath + "/bridge.php?action=" + (onlyDb ? "deployDb" : "deploy");
                    string postData = "";

                    if (onlyDb)
                    {
                        AppendLog("⚡ Đang kích hoạt bridge để tự đọc .env và cập nhật database...", colorText);
                        postData = "clear_db=" + (clearDb ? "1" : "0");
                    }
                    else
                    {
                        AppendLog("⚡ Đang kích hoạt bridge để giải nén và cập nhật database...", colorText);
                        string safeDbName = finalDbName.Replace("\\", "\\\\").Replace("\"", "\\\"");
                        string safeDbPass = finalDbPass.Replace("\\", "\\\\").Replace("\"", "\\\"");
                        string safeAppUrl = (scheme + webDomain + "/" + relWebPath).Replace("\\", "\\\\").Replace("\"", "\\\"");

                        string dbConfig = "{\"host\":\"localhost\",\"name\":\"" + safeDbName + "\",\"user\":\"" + safeDbName + "\",\"pass\":\"" + safeDbPass + "\"}";
                        string appConfig = "{\"app_url\":\"" + safeAppUrl + "\",\"ssl\":" + (useSSL ? "true" : "false") + ",\"skip_lock\":true}";
                        postData = "db_config=" + Uri.EscapeDataString(dbConfig) + 
                                   "&app_config=" + Uri.EscapeDataString(appConfig) + 
                                   "&clear_db=" + (clearDb ? "1" : "0");
                    }

                    string bridgeRes = PostHttp(bridgeUrl, postData);

                    PrintBridgeLogs(bridgeRes);

                    if (!string.IsNullOrEmpty(bridgeRes) && bridgeRes.Contains("\"status\":\"success\""))
                    {
                        AppendLog("✅ Bridge hoàn thành!", colorGreen);
                        
                        // Auto-cleanup on hosting (self-destruct bridge.php and clean up temporary files)
                        AppendLog("🧹 Đang tự động dọn dẹp các tệp tạm trên hosting...", colorText);
                        string cleanupUrl = scheme + webDomain + "/" + relWebPath + "/bridge.php?action=cleanup";
                        string cleanupRes = PostHttp(cleanupUrl, "");
                        if ((!string.IsNullOrEmpty(cleanupRes) && cleanupRes.Contains("\"status\":\"success\"")) || (cleanupRes != null && (cleanupRes.Contains("404") || cleanupRes.Contains("Not Found"))))
                        {
                            AppendLog("✅ Tự động dọn dẹp thành công! File bridge.php đã tự hủy.", colorGreen);
                        }
                        else
                        {
                            AppendLog("⚠️ HTTP tự hủy không thành công (" + (cleanupRes ?? "không phản hồi") + "). Thử xóa qua FTP...", Color.FromArgb(245, 158, 11));
                            string ftpDelErr;
                            if (DeleteFtp(ftpBase + "bridge.php", ftpCreds, out ftpDelErr))
                            {
                                AppendLog("✅ Tự động dọn dẹp thành công! Đã xóa file bridge.php qua FTP.", colorGreen);
                            }
                            else
                            {
                                AppendLog("⚠️ Cảnh báo tự dọn dẹp: " + ftpDelErr + ". Bạn có thể tự tay xóa file bridge.php trên hosting.", Color.FromArgb(245, 158, 11));
                            }
                        }

                        _deployedUrl = scheme + webDomain + "/" + relWebPath + "/";
                        AppendLog(onlyDb ? "🎉 Cập nhật database thành công!" : "🎉 Deploy Demo thành công!", colorPurple);
                        FinishDeploy(true);
                    }
                    else
                    {
                        int bridgeResLen = (bridgeRes != null) ? bridgeRes.Length : 0;
                        string bridgeResShort = (bridgeResLen > 300) ? bridgeRes.Substring(0, 300) : (bridgeRes ?? "(no response)");
                        AppendLog("⚠️ Bridge response: " + bridgeResShort, Color.FromArgb(245, 158, 11));
                        AppendLog("💡 Hãy kiểm tra lại URL domain và cấu hình FTP/DA.", colorDim);
                        FinishDeploy(false);
                    }
                } catch (Exception ex) {
                    AppendLog("❌ Lỗi: " + ex.Message, colorRed);
                    FinishDeploy(false);
                } finally {
                    // Cleanup temp files
                    try {
                        if (File.Exists(zipPath)) File.Delete(zipPath);
                        if (File.Exists(sqlPath)) File.Delete(sqlPath);
                    } catch { }
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void StartCleanup()
        {
            if (string.IsNullOrWhiteSpace(_txtFtpHost.Text) || string.IsNullOrWhiteSpace(_txtWebDomain.Text))
            {
                MessageBox.Show("Vui lòng cấu hình đầy đủ thông tin FTP/Domain trước khi dọn dẹp.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn dọn dẹp các tệp tạm (dist.zip, dist.sql) và tự hủy tệp bridge.php trên hosting không?",
                "Xác nhận dọn dẹp",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _btnDeploy.Enabled = false;
            _btnDeployFresh.Enabled = false;
            _btnCleanup.Enabled = false;
            _lblApiStatus.Text = "Đang dọn dẹp...";
            _lblApiStatus.ForeColor = Color.FromArgb(245, 158, 11);

            string webDomain = _txtWebDomain.Text.Trim();
            bool useSSL = _chkSsl.Checked;
            string sitePath = _sitePath;

            System.Threading.Thread worker = new System.Threading.Thread(() => {
                try
                {
                    AppendLog("🧹 Bắt đầu dọn dẹp hosting...", colorText);
                    string ftpHost = "";
                    string ftpUser = "";
                    string ftpPass = "";
                    string ftpRoot = "";
                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                        ftpHost = _txtFtpHost.Text.Trim();
                        ftpUser = _txtFtpUser.Text.Trim();
                        ftpPass = _txtFtpPass.Text.Trim();
                        ftpRoot = string.IsNullOrWhiteSpace(_txtFtpRoot.Text) ? "/public_html" : _txtFtpRoot.Text.Trim();
                    });
                    string ftpBase = "ftp://" + ftpHost + "/" + (ftpRoot.TrimStart('/') + "/" + sitePath.Replace('\\', '/').TrimStart('/')).Replace("//", "/").TrimEnd('/') + "/";
                    string ftpCreds = ftpUser + ":" + ftpPass;

                    string scheme = useSSL ? "https://" : "http://";
                    string relWebPath = sitePath.Replace('\\', '/').TrimStart('/');
                    string cleanupUrl = scheme + webDomain + "/" + relWebPath + "/bridge.php?action=cleanup";
                    
                    AppendLog("  Gửi yêu cầu tự hủy tới: " + cleanupUrl, colorDim);
                    string res = PostHttp(cleanupUrl, "");
                    
                    bool cleanupOk = false;
                    if ((!string.IsNullOrEmpty(res) && res.Contains("\"status\":\"success\"")) || (res != null && (res.Contains("404") || res.Contains("Not Found"))))
                    {
                        AppendLog("✅ Dọn dẹp thành công! File bridge.php đã tự hủy.", colorGreen);
                        cleanupOk = true;
                    }
                    else
                    {
                        AppendLog("⚠️ HTTP tự hủy không thành công (" + (res ?? "không phản hồi") + "). Thử xóa qua FTP...", Color.FromArgb(245, 158, 11));
                        string ftpDelErr;
                        if (DeleteFtp(ftpBase + "bridge.php", ftpCreds, out ftpDelErr))
                        {
                            AppendLog("✅ Dọn dẹp thành công! Đã xóa file bridge.php qua FTP.", colorGreen);
                            cleanupOk = true;
                        }
                        else
                        {
                            AppendLog("⚠️ Không thể dọn dẹp tự động: " + ftpDelErr + ". Bạn có thể tự tay xóa file bridge.php trên hosting.", Color.FromArgb(245, 158, 11));
                            cleanupOk = true; // Vẫn xem là thành công để tránh báo lỗi đỏ gây hoang mang
                        }
                    }

                    if (cleanupOk)
                    {
                        this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate {
                            _lblApiStatus.Text = "Dọn dẹp hoàn tất!";
                            _lblApiStatus.ForeColor = colorGreen;
                        });
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("❌ Lỗi dọn dẹp: " + ex.Message, colorRed);
                    this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate {
                        _lblApiStatus.Text = "Lỗi khi dọn dẹp.";
                        _lblApiStatus.ForeColor = colorRed;
                    });
                }
                finally
                {
                    this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate {
                        _btnDeploy.Enabled = true;
                        _btnDeployFresh.Enabled = true;
                        _btnCleanup.Enabled = true;
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private string GenerateRandomDbPass()
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var rand = new Random();
            char[] password = new char[12];
            for (int i = 0; i < 12; i++)
            {
                password[i] = chars[rand.Next(chars.Length)];
            }
            return new string(password);
        }

        private string GenerateDemoDbSuffix(string category, string projectName)
        {
            string catNum = "";
            var m = System.Text.RegularExpressions.Regex.Match(category ?? "", @"(\d+)$");
            if (m.Success) catNum = m.Groups[1].Value.PadLeft(2, '0');
            string prefix = "6" + catNum;
            string parts0 = (projectName ?? "").Split('_')[0];
            string cleanBase = System.Text.RegularExpressions.Regex.Replace(parts0.ToLower(), "[^a-z]", "");
            string name = cleanBase.Length > 10 ? cleanBase.Substring(0, 10) : cleanBase;
            string suffix = prefix + name;
            return suffix.Length > 13 ? suffix.Substring(0, 13) : suffix;
        }

        private void PrintBridgeLogs(string bridgeRes)
        {
            if (string.IsNullOrEmpty(bridgeRes)) return;
            try
            {
                var matchLogs = System.Text.RegularExpressions.Regex.Match(bridgeRes, @"""logs""\s*:\s*\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (matchLogs.Success)
                {
                    string logsContent = matchLogs.Groups[1].Value;
                    var logMatches = System.Text.RegularExpressions.Regex.Matches(logsContent, @"""(.*?)""");
                    foreach (System.Text.RegularExpressions.Match lm in logMatches)
                    {
                        string logLine = lm.Groups[1].Value;
                        logLine = logLine.Replace("\\/", "/").Replace("\\\\", "\\").Replace("\\\"", "\"");
                        AppendLog("  [Server] " + logLine, colorDim);
                    }
                }
            }
            catch { }
        }

        private void FinishDeploy(bool success)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { FinishDeploy(success); });
                return;
            }
            _btnCleanup.Enabled = true;
            if (success)
            {
                _isLocked = true;
                SetDeployed(_projectDir, true);
                UpdateDeployButtonsState();
                _lblApiStatus.Text = "Deploy thành công!";
                _lblApiStatus.ForeColor = colorGreen;

                if (_btnOpenWeb != null)
                {
                    _btnOpenWeb.Visible = true;
                    _btnDeploy.Visible = false;
                }
            }
            else
            {
                _btnDeploy.Enabled = true;
                _btnDeployFresh.Enabled = true;
                _lblApiStatus.Text = "Có lỗi xảy ra, xem log.";
                _lblApiStatus.ForeColor = colorRed;

                if (_btnOpenWeb != null)
                {
                    _btnOpenWeb.Visible = false;
                    _btnDeploy.Visible = true;
                }
            }
        }

        private void AppendLog(string text, Color color)
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { AppendLog(text, color); });
                return;
            }
            string ts = "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = Color.FromArgb(80, 90, 110);
            _logBox.AppendText(ts);
            _logBox.SelectionColor = color;
            _logBox.AppendText(text + "\n");
            _logBox.ScrollToCaret();
        }

        private int RunCommand(string cmd, out string output)
        {
            var psi = new ProcessStartInfo("cmd.exe", "/C \"" + cmd + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            var proc = Process.Start(psi);
            string o = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            output = o.Trim();
            return proc.ExitCode;
        }

        private bool UploadFtp(string ftpUrl, string userPwd, string localFile, out string error)
        {
            error = "";
            try {
                string curlPath = "curl";
                string cmd = string.Format("-T \"{0}\" --ftp-create-dirs -u \"{1}\" \"{2}\" --ssl-reqd --ftp-ssl --insecure -m 300 -s", localFile, userPwd, ftpUrl);
                var psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                var proc = Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                cmd = string.Format("-T \"{0}\" --ftp-create-dirs -u \"{1}\" \"{2}\" -m 300 -s", localFile, userPwd, ftpUrl);
                psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                proc = Process.Start(psi);
                err += proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                error = err.Trim();
                return false;
            } catch (Exception ex) { error = ex.Message; return false; }
        }

        private bool DownloadFtp(string ftpUrl, string userPwd, string localFile, out string error)
        {
            error = "";
            try {
                string curlPath = "curl";
                string cmd = string.Format("-o \"{0}\" -u \"{1}\" \"{2}\" --ssl-reqd --ftp-ssl --insecure -m 30 -s", localFile, userPwd, ftpUrl);
                var psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                var proc = Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                cmd = string.Format("-o \"{0}\" -u \"{1}\" \"{2}\" -m 30 -s", localFile, userPwd, ftpUrl);
                psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                proc = Process.Start(psi);
                err += proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                error = err.Trim();
                return false;
            } catch (Exception ex) { error = ex.Message; return false; }
        }

        private bool DeleteFtp(string ftpUrl, string userPwd, out string error)
        {
            error = "";
            try {
                string curlPath = "curl";
                string cmd = string.Format("-u \"{0}\" \"{1}\" -X \"DELE\" --ssl-reqd --ftp-ssl --insecure -s", userPwd, ftpUrl);
                var psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                var proc = Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                cmd = string.Format("-u \"{0}\" \"{1}\" -X \"DELE\" -s", userPwd, ftpUrl);
                psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                proc = Process.Start(psi);
                err += proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                error = err.Trim();
                return false;
            } catch (Exception ex) { error = ex.Message; return false; }
        }

        private string PostHttp(string url, string postData)
        {
            try {
                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                    return wc.UploadString(url, postData);
                }
            } catch (Exception ex) { return "{\"status\":\"error\",\"message\":\"" + ex.Message + "\"}"; }
        }

        private string PostHttpWithAuth(string url, string postData, string authUser, string authPass)
        {
            try {
                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authUser + ":" + authPass));
                    wc.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + credentials;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                    return wc.UploadString(url, postData);
                }
            } catch (Exception ex) { return "error: " + ex.Message; }
        }

        private string CallDirectAdminDbApi(string host, string port, string user, string pass, string postData, string endpoint = "CMD_API_DATABASES")
        {
            string url = "https://" + host + ":" + port + "/" + endpoint;
            string res = PostHttpWithAuth(url, postData, user, pass);
            if (res.Contains("error") || res.Contains("wrong version number") || res.Contains("The underlying connection was closed") || res.Contains("timed out") || res.Contains("Server Certificate"))
            {
                url = "http://" + host + ":" + port + "/" + endpoint;
                res = PostHttpWithAuth(url, postData, user, pass);
            }
            return res;
        }

        private string GetActiveMySqlPort()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f is MainForm)
                    {
                        return ((MainForm)f).GetMySqlPort();
                    }
                }
            }
            catch { }
            return "3306";
        }

        private void ExportDbPdo(string host, string dbName, string user, string pass, string outputFile)
        {
            string mysqlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mysql");
            string mysqlBin = "";
            if (Directory.Exists(mysqlDir))
            {
                foreach (var dir in Directory.GetDirectories(mysqlDir))
                {
                    string target = Path.Combine(dir, "bin", "mysqldump.exe");
                    if (File.Exists(target))
                    {
                        mysqlBin = target;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(mysqlBin))
            {
                string passPart = string.IsNullOrEmpty(pass) ? "" : ("-p\"" + pass + "\"");
                string finalDbPort = GetActiveMySqlPort();
                string portPart = string.IsNullOrEmpty(finalDbPort) ? "" : ("--port=" + finalDbPort);
                string cmd = string.Format("\"{0}\" --host={1} {2} --user={3} {4} --default-character-set=utf8mb4 --result-file=\"{5}\" {6}", 
                                           mysqlBin, host, portPart, user, passPart, outputFile, dbName);
                string ignoredOut;
                RunCommand(cmd, out ignoredOut);
            }
        }

        // ── GLOBAL CONFIG (shared by all projects) ───────────────────
        public static Dictionary<string, string> LoadGlobalConfig()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(GlobalConfigPath)) return d;
            try {
                return ParseJson(File.ReadAllText(GlobalConfigPath));
            } catch { return d; }
        }

        public static void SaveGlobalConfig(Dictionary<string, string> dict)
        {
            try {
                File.WriteAllText(GlobalConfigPath, SerializeJson(dict));
            } catch { }
        }

        // (Moved virtual host helper configurations to MainForm)

        public static bool IsDeployed(string projectDir)
        {
            try {
                if (!File.Exists(DeployStatusPath)) return false;
                return ParseJson(File.ReadAllText(DeployStatusPath)).ContainsKey(projectDir.ToLower());
            } catch { return false; }
        }

        public static void SetDeployed(string projectDir, bool value)
        {
            try {
                var dict = new Dictionary<string, string>();
                if (File.Exists(DeployStatusPath)) {
                    dict = ParseJson(File.ReadAllText(DeployStatusPath));
                }
                string key = projectDir.ToLower();
                if (value) dict[key] = "true";
                else dict.Remove(key);
                File.WriteAllText(DeployStatusPath, SerializeJson(dict));
            } catch { }
        }

        public static bool GetProjectSsl(string projectDir)
        {
            try {
                if (!File.Exists(SslConfigPath)) return false;
                var dict = ParseJson(File.ReadAllText(SslConfigPath));
                string key = projectDir.ToLower();
                return dict.ContainsKey(key) && dict[key] == "true";
            } catch { return false; }
        }

        public static void SetProjectSsl(string projectDir, bool value)
        {
            try {
                var dict = new Dictionary<string, string>();
                if (File.Exists(SslConfigPath)) {
                    dict = ParseJson(File.ReadAllText(SslConfigPath));
                }
                dict[projectDir.ToLower()] = value ? "true" : "false";
                File.WriteAllText(SslConfigPath, SerializeJson(dict));
            } catch { }
        }

        public static string GetProjectDb(string projectDir)
        {
            try {
                if (!File.Exists(DbConfigPath)) return "";
                var dict = ParseJson(File.ReadAllText(DbConfigPath));
                string key = projectDir.ToLower();
                return dict.ContainsKey(key) ? dict[key] : "";
            } catch { return ""; }
        }

        public static void SetProjectDb(string projectDir, string dbName)
        {
            try {
                var dict = new Dictionary<string, string>();
                if (File.Exists(DbConfigPath)) {
                    dict = ParseJson(File.ReadAllText(DbConfigPath));
                }
                dict[projectDir.ToLower()] = dbName;
                File.WriteAllText(DbConfigPath, SerializeJson(dict));
            } catch { }
        }

        public static Dictionary<string, string> ParseJson(string json)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json)) return dict;
            var matches = System.Text.RegularExpressions.Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                string key = m.Groups[1].Value.Replace("\\\\", "\\");
                string val = m.Groups[2].Value.Replace("\\\\", "\\");
                dict[key] = val;
            }
            return dict;
        }

        public static string SerializeJson(Dictionary<string, string> dict)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",\n");
                sb.AppendFormat("  \"{0}\": \"{1}\"", kv.Key.Replace("\\", "\\\\").Replace("\"", "\\\""), kv.Value.Replace("\\", "\\\\").Replace("\"", "\\\""));
                first = false;
            }
            sb.Append("\n}");
            return sb.ToString();
        }

        // Helper graphics paths
        private void ApplyRoundedRegion(Control control, int radius)
        {
            control.Region = new Region(GetRoundedRect(new Rectangle(0, 0, control.Width, control.Height), radius));
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            gp.AddArc(bounds.X + bounds.Width - d, bounds.Y, d, d, 270, 90);
            gp.AddArc(bounds.X + bounds.Width - d, bounds.Y + bounds.Height - d, d, d, 0, 90);
            gp.AddArc(bounds.X, bounds.Y + bounds.Height - d, d, d, 90, 90);
            gp.CloseAllFigures();
            return gp;
        }

        private void DrawCardBorder(object sender, PaintEventArgs pe)
        {
            Control c = (Control)sender;
            pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(colorBorder, 1f))
            {
                pe.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
            }
        }
    }


    // =======================================================
    // CUSTOM TRAY POPUP - Giao diện đẹp như Herd khi nhấp chuột phải tray
    // =======================================================
    public class TrayPopupForm : Form
    {
        private Action _onStartAll, _onStopAll, _onOpenUI, _onExit;
        private bool _isClosing = false;

        public TrayPopupForm(
            string webServerName, bool isWebRunning,
            string phpVersion,   bool isPhpRunning,
            bool isMysqlRunning,
            Action onStartAll, Action onStopAll,
            Action onOpenUI,   Action onExit)
        {
            _onStartAll = onStartAll;
            _onStopAll  = onStopAll;
            _onOpenUI   = onOpenUI;
            _onExit     = onExit;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar   = false;
            this.TopMost         = true;
            this.Width           = 268;
            this.BackColor       = Color.White;
            this.Padding         = new Padding(0);

            // T\u1ef1 \u0111\u00f3ng khi m\u1ea5t focus
            this.Deactivate += (s, e) => { if (!_isClosing) { _isClosing = true; this.Close(); } };

            int y = 0;

            // ── HEADER ──────────────────────────────
            Panel pnlHeader = new Panel();
            pnlHeader.Location  = new Point(0, y);
            pnlHeader.Size      = new Size(268, 54);
            pnlHeader.BackColor = Color.White;
            pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Icon nh\u1ecf ch\u1ea1m tr\u00f2n m\u00e0u indigo
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    pe.Graphics.FillEllipse(bg, 14, 15, 26, 26);
                using (Font f = new Font("Segoe MDL2 Assets", 10f))
                using (SolidBrush fg = new SolidBrush(Color.FromArgb(79, 70, 229)))
                    pe.Graphics.DrawString("\uEC27", f, fg, new PointF(17f, 18f));
                // T\u00ean app
                using (Font f = new Font("Segoe UI Semibold", 10f, FontStyle.Bold))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(17, 24, 39)))
                    pe.Graphics.DrawString("RBW Stack", f, c, new PointF(48f, 18f));
                // \u0110\u01b0\u1eddng k\u1ebb d\u01b0\u1edbi
                using (Pen p = new Pen(Color.FromArgb(243, 244, 246)))
                    pe.Graphics.DrawLine(p, 0, 53, 267, 53);
            };
            this.Controls.Add(pnlHeader);
            y += 54;

            // ── LABEL "DỊCH VỤ" ────────────────────
            Panel pnlSvcHdr = new Panel();
            pnlSvcHdr.Location  = new Point(0, y);
            pnlSvcHdr.Size      = new Size(268, 28);
            pnlSvcHdr.BackColor = Color.White;
            pnlSvcHdr.Paint += (s, pe) => {
                using (Font f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(156, 163, 175)))
                    pe.Graphics.DrawString("D\u1ecaCH V\u1ee4", f, c, new PointF(16f, 8f));
            };
            this.Controls.Add(pnlSvcHdr);
            y += 28;

            // ── SERVICE ROWS ────────────────────────
            string webLabel = webServerName;
            string phpLabel = string.IsNullOrEmpty(phpVersion) ? "PHP-CGI" : ("PHP  " + phpVersion);
            y = AddServiceRow(webLabel,         isWebRunning,   y, false);
            y = AddServiceRow(phpLabel,         isPhpRunning,   y, false);
            y = AddServiceRow("MySQL / MariaDB", isMysqlRunning, y, true);

            y += 2;
            AddDivider(y); y += 10;

            // ── ACTIONS ─────────────────────────────
            bool allRunning = isWebRunning && isPhpRunning && isMysqlRunning;
            bool anyRunning = isWebRunning || isPhpRunning || isMysqlRunning;

            // Chỉ hiện "Khởi động" khi chưa start hết
            if (!allRunning)
                y = AddActionRow("\u25b6  Kh\u1edfi \u0111\u1ed9ng t\u1ea5t c\u1ea3", Color.FromArgb(16, 185, 129), y, _onStartAll, true);

            // Chỉ hiện "Dừng" khi có ít nhất 1 service đang chạy
            if (anyRunning)
                y = AddActionRow("\u25a0  D\u1eebng t\u1ea5t c\u1ea3", Color.FromArgb(239, 68, 68), y, _onStopAll, false);

            AddDivider(y); y += 10;

            y = AddActionRow("M\u1edf giao di\u1ec7n", Color.FromArgb(31, 41, 55), y, _onOpenUI, false);

            AddDivider(y); y += 10;

            y = AddActionRow("Tho\u00e1t \u1ee9ng d\u1ee5ng", Color.FromArgb(239, 68, 68), y, _onExit, false);

            y += 6;
            this.Height = y;
        }

        private int AddServiceRow(string name, bool running, int y, bool isLast)
        {
            Panel row = new Panel();
            row.Location  = new Point(0, y);
            row.Size      = new Size(268, 38);
            row.BackColor = Color.White;
            bool capturedRunning = running;
            bool capturedIsLast  = isLast;

            row.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Status dot
                Color dotColor = capturedRunning
                    ? Color.FromArgb(16, 185, 129)
                    : Color.FromArgb(209, 213, 219);
                using (SolidBrush br = new SolidBrush(dotColor))
                    pe.Graphics.FillEllipse(br, 16, 15, 8, 8);

                // Service name
                using (Font f = new Font("Segoe UI", 9.5f))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(31, 41, 55)))
                    pe.Graphics.DrawString(name, f, c, new PointF(34f, 11f));

                // Status badge (right side)
                string badge   = capturedRunning ? "Ch\u1ea1y" : "D\u1eebng";
                Color badgeCol = capturedRunning
                    ? Color.FromArgb(16, 185, 129)
                    : Color.FromArgb(156, 163, 175);
                using (Font f = new Font("Segoe UI", 8f))
                using (SolidBrush c = new SolidBrush(badgeCol))
                {
                    SizeF sz = pe.Graphics.MeasureString(badge, f);
                    pe.Graphics.DrawString(badge, f, c, new PointF(268f - sz.Width - 14f, 13f));
                }

                // Bottom rule
                if (!capturedIsLast)
                    using (Pen p = new Pen(Color.FromArgb(249, 250, 251)))
                        pe.Graphics.DrawLine(p, 32, 37, 252, 37);
            };
            this.Controls.Add(row);
            return y + 38;
        }

        private void AddDivider(int y)
        {
            Panel div = new Panel();
            div.Location  = new Point(0, y);
            div.Size      = new Size(268, 10);
            div.BackColor = Color.White;
            div.Paint += (s, pe) => {
                using (Pen p = new Pen(Color.FromArgb(243, 244, 246)))
                    pe.Graphics.DrawLine(p, 14, 5, 254, 5);
            };
            this.Controls.Add(div);
        }

        private int AddActionRow(string text, Color textColor, int y, Action onClick, bool isBold)
        {
            Panel row = new Panel();
            row.Location  = new Point(0, y);
            row.Size      = new Size(268, 36);
            row.BackColor = Color.White;
            row.Cursor    = Cursors.Hand;

            Color capturedColor = textColor;
            string capturedText = text;
            bool capturedBold   = isBold;
            Action capturedClick = onClick;

            row.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                FontStyle fs = capturedBold ? FontStyle.Bold : FontStyle.Regular;
                using (Font f = new Font("Segoe UI Semibold", 9.5f, fs))
                using (SolidBrush c = new SolidBrush(capturedColor))
                    pe.Graphics.DrawString(capturedText, f, c, new PointF(16f, 10f));
            };

            row.MouseEnter += (s, e) => { row.BackColor = Color.FromArgb(249, 250, 251); row.Invalidate(); };
            row.MouseLeave += (s, e) => { row.BackColor = Color.White; row.Invalidate(); };
            row.Click += (s, e) => {
                _isClosing = true;
                this.Close();
                if (capturedClick != null) capturedClick();
            };

            this.Controls.Add(row);
            return y + 36;
        }

        // Drop shadow
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        // Vi\u1ec1n ngo\u00e0i 1px m\u00e0u xanh nh\u1ea1t
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
        }
    }

    // =======================================================
    // GOOGLE FONT INFO AND LOCAL FONT STRUCTURES
    // =======================================================
    public class GoogleFontInfo
    {
        public string Family { get; set; }
        public string Category { get; set; }
        public List<string> Variants { get; set; }
    }

    public class LocalFontGroup
    {
        public string Id { get; set; }
        public string Family { get; set; }
        public string Category { get; set; }
        public List<LocalFontFile> Files { get; set; }
    }

    public class LocalFontFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string Weight { get; set; }
        public string Style { get; set; }
        public string VariantKey { get; set; }
    }

    // =======================================================
    // FONT INSTALLER FORM - Search & Install Local & Google Fonts
    // =======================================================
    public class FontInstallerForm : Form
    {
        private string _projectDir;
        private string _relativeSitePath;
        private Panel _pnlHeader;
        private Label _btnClose;
        private TextBox txtLocalFontPath;
        private TextBox txtSearch;
        private FlowLayoutPanel flpResults;
        private FlowLayoutPanel flpInstalled;
        private RichTextBox rtxCssPreview;
        private Panel pnlPath;
        private Panel pnlSearch;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        private Color colorBg = Color.FromArgb(248, 250, 252);
        private Color colorBorder = Color.FromArgb(226, 232, 240);
        private Color colorPurple = Color.FromArgb(139, 92, 246);

        private static string FallbackGoogleFonts = @"Roboto|SANS_SERIF|100,100i,300,300i,400,regular,500,500i,700,700i,900,900i
Inter|SANS_SERIF|100,200,300,400,regular,500,600,700,800,900
Open Sans|SANS_SERIF|300,300i,400,regular,500,500i,600,600i,700,700i,800,800i
Lato|SANS_SERIF|100,100i,300,300i,400,regular,700,700i,900,900i
Montserrat|SANS_SERIF|100,100i,200,200i,300,300i,400,regular,500,500i,600,600i,700,700i,800,800i,900,900i
Oswald|SANS_SERIF|200,300,400,regular,500,600,700
Source Sans Pro|SANS_SERIF|200,200i,300,300i,400,regular,600,600i,700,700i,900,900i
Raleway|SANS_SERIF|100,100i,200,200i,300,300i,400,regular,500,500i,600,600i,700,700i,800,800i,900,900i
PT Sans|SANS_SERIF|400,regular,italic,700,700italic
Merriweather|SERIF|300,300i,400,regular,700,700i,900,900i
Noto Sans|SANS_SERIF|100,100i,200,200i,300,300i,400,regular,500,500i,600,600i,700,700i,800,800i,900,900i
Poppins|SANS_SERIF|100,100i,200,200i,300,300i,400,regular,500,500i,600,600i,700,700i,800,800i,900,900i
Playfair Display|SERIF|400,regular,italic,700,700i,900,900i
Ubuntu|SANS_SERIF|300,300i,400,regular,italic,500,500i,700,700i
Nunito|SANS_SERIF|200,200i,300,300i,400,regular,600,600i,700,700i,800,800i,900,900i";

        public FontInstallerForm(string projectDir, string relativeSitePath)
        {
            _projectDir = projectDir;
            _relativeSitePath = relativeSitePath;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(800, 600);
            this.BackColor = colorBg;

            // ── HEADER ──────────────────────────────────────────────────
            _pnlHeader = new Panel();
            _pnlHeader.Location = new Point(0, 0);
            _pnlHeader.Size = new Size(800, 45);
            _pnlHeader.BackColor = Color.FromArgb(241, 245, 249);
            _pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Font font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush br = new SolidBrush(colorPurple))
                    pe.Graphics.DrawString("⚡  CÀI ĐẶT FONTS CHO DỰ ÁN: " + _relativeSitePath.ToUpper(), font, br, new PointF(14f, 13f));
                using (Pen pen = new Pen(colorBorder, 1f))
                    pe.Graphics.DrawLine(pen, 0, 44, 800, 44);
            };
            _pnlHeader.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            this.Controls.Add(_pnlHeader);

            _btnClose = new Label();
            _btnClose.Text = "✕";
            _btnClose.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            _btnClose.Location = new Point(765, 10);
            _btnClose.Size = new Size(26, 24);
            _btnClose.TextAlign = ContentAlignment.MiddleCenter;
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.Click += (s, e) => this.Close();
            _btnClose.MouseEnter += (s, e) => { _btnClose.ForeColor = Color.White; _btnClose.BackColor = Color.FromArgb(239, 68, 68); };
            _btnClose.MouseLeave += (s, e) => { _btnClose.ForeColor = Color.FromArgb(100, 116, 139); _btnClose.BackColor = Color.Transparent; };
            _pnlHeader.Controls.Add(_btnClose);

            // ── PATH PANEL ──────────────────────────────────────────────
            pnlPath = new Panel();
            pnlPath.Location = new Point(15, 55);
            pnlPath.Size = new Size(480, 50);
            pnlPath.BackColor = Color.White;
            pnlPath.Paint += (s, pe) => {
                using (Pen pen = new Pen(colorBorder, 1.5f))
                    pe.Graphics.DrawRectangle(pen, 0, 0, pnlPath.Width - 1, pnlPath.Height - 1);
            };
            this.Controls.Add(pnlPath);

            Label lblLocalPath = new Label();
            lblLocalPath.Text = "THƯ MỤC FONT LOCAL";
            lblLocalPath.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblLocalPath.ForeColor = Color.FromArgb(100, 116, 139);
            lblLocalPath.Location = new Point(10, 6);
            lblLocalPath.AutoSize = true;
            pnlPath.Controls.Add(lblLocalPath);

            txtLocalFontPath = new TextBox();
            txtLocalFontPath.Location = new Point(10, 22);
            txtLocalFontPath.Size = new Size(380, 20);
            txtLocalFontPath.ReadOnly = true;
            txtLocalFontPath.BorderStyle = BorderStyle.None;
            txtLocalFontPath.BackColor = Color.White;
            txtLocalFontPath.Font = new Font("Segoe UI", 9f);
            txtLocalFontPath.ForeColor = Color.FromArgb(30, 41, 59);
            txtLocalFontPath.Text = GetLocalFontPath();
            pnlPath.Controls.Add(txtLocalFontPath);

            ModernButton btnBrowseLocal = new ModernButton();
            btnBrowseLocal.Text = "Chọn...";
            btnBrowseLocal.Size = new Size(70, 24);
            btnBrowseLocal.Location = new Point(400, 18);
            btnBrowseLocal.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnBrowseLocal.NormalColor = Color.White;
            btnBrowseLocal.HoverColor = Color.FromArgb(243, 244, 246);
            btnBrowseLocal.BorderColor = Color.FromArgb(209, 213, 219);
            btnBrowseLocal.ForeColor = Color.FromArgb(55, 65, 81);
            btnBrowseLocal.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Chọn thư mục chứa thư viện font local của bạn";
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        txtLocalFontPath.Text = fbd.SelectedPath;
                        SaveLocalFontPath(fbd.SelectedPath);
                        DoSearch();
                    }
                }
            };
            pnlPath.Controls.Add(btnBrowseLocal);

            // ── SEARCH PANEL ────────────────────────────────────────────
            pnlSearch = new Panel();
            pnlSearch.Location = new Point(15, 115);
            pnlSearch.Size = new Size(480, 40);
            pnlSearch.BackColor = Color.White;
            pnlSearch.Paint += (s, pe) => {
                using (Pen pen = new Pen(colorBorder, 1.5f))
                    pe.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };
            this.Controls.Add(pnlSearch);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(10, 11);
            txtSearch.Size = new Size(380, 20);
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.Font = new Font("Segoe UI", 10f);
            txtSearch.ForeColor = Color.FromArgb(30, 41, 59);
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    DoSearch();
                }
            };
            txtSearch.HandleCreated += (s, e) => {
                SendMessage(txtSearch.Handle, 0x1501, 0, "Tìm kiếm font (Local hoặc Google)...");
            };
            pnlSearch.Controls.Add(txtSearch);

            ModernButton btnSearch = new ModernButton();
            btnSearch.Text = "Tìm";
            btnSearch.Size = new Size(70, 26);
            btnSearch.Location = new Point(400, 7);
            btnSearch.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnSearch.NormalColor = Color.FromArgb(139, 92, 246);
            btnSearch.HoverColor = Color.FromArgb(124, 58, 237);
            btnSearch.ForeColor = Color.White;
            btnSearch.Click += (s, e) => DoSearch();
            pnlSearch.Controls.Add(btnSearch);

            // ── FLOW RESULTS PANEL ──────────────────────────────────────
            flpResults = new FlowLayoutPanel();
            flpResults.Location = new Point(15, 165);
            flpResults.Size = new Size(480, 410);
            flpResults.AutoScroll = true;
            flpResults.BackColor = Color.Transparent;
            this.Controls.Add(flpResults);

            // ── RIGHT INSTALLED PANEL ───────────────────────────────────
            Label lblInstalledTitle = new Label();
            lblInstalledTitle.Text = "FONTS ĐÃ CÀI ĐẶT";
            lblInstalledTitle.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblInstalledTitle.ForeColor = Color.FromArgb(139, 92, 246);
            lblInstalledTitle.Location = new Point(510, 55);
            lblInstalledTitle.AutoSize = true;
            this.Controls.Add(lblInstalledTitle);

            flpInstalled = new FlowLayoutPanel();
            flpInstalled.Location = new Point(510, 72);
            flpInstalled.Size = new Size(275, 100);
            flpInstalled.AutoScroll = true;
            flpInstalled.BackColor = Color.White;
            flpInstalled.Paint += (s, pe) => {
                using (Pen pen = new Pen(colorBorder, 1.5f))
                    pe.Graphics.DrawRectangle(pen, 0, 0, flpInstalled.Width - 1, flpInstalled.Height - 1);
            };
            this.Controls.Add(flpInstalled);

            // ── CSS PREVIEW PANEL ───────────────────────────────────────
            Label lblCssTitle = new Label();
            lblCssTitle.Text = "PREVIEW FONTS.CSS";
            lblCssTitle.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblCssTitle.ForeColor = Color.FromArgb(100, 116, 139);
            lblCssTitle.Location = new Point(510, 182);
            lblCssTitle.AutoSize = true;
            this.Controls.Add(lblCssTitle);

            rtxCssPreview = new RichTextBox();
            rtxCssPreview.Location = new Point(510, 199);
            rtxCssPreview.Size = new Size(275, 376);
            rtxCssPreview.ReadOnly = true;
            rtxCssPreview.BackColor = Color.White;
            rtxCssPreview.ForeColor = Color.FromArgb(15, 23, 42);
            rtxCssPreview.Font = new Font("Consolas", 8.5f);
            rtxCssPreview.BorderStyle = BorderStyle.None;
            this.Controls.Add(rtxCssPreview);

            this.Paint += (s, pe) => {
                using (Pen pen = new Pen(colorBorder, 1.5f))
                {
                    pe.Graphics.DrawRectangle(pen, rtxCssPreview.Location.X - 1, rtxCssPreview.Location.Y - 1, rtxCssPreview.Width + 1, rtxCssPreview.Height + 1);
                    pe.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadInstalledFontsAndCss();
            DoSearch();
        }

        private void LoadInstalledFontsAndCss()
        {
            flpInstalled.Controls.Clear();
            string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
            if (File.Exists(fontsCssPath))
            {
                rtxCssPreview.Text = File.ReadAllText(fontsCssPath);
            }
            else
            {
                rtxCssPreview.Text = "/* Chưa có font nào được cài đặt. */";
            }

            var installed = ParseInstalledFonts();
            if (installed.Count == 0)
            {
                Label lblNo = new Label();
                lblNo.Text = "Chưa có font nào được cài đặt.";
                lblNo.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
                lblNo.ForeColor = Color.FromArgb(148, 163, 184);
                lblNo.AutoSize = true;
                flpInstalled.Controls.Add(lblNo);
            }
            else
            {
                foreach (var fName in installed)
                {
                    Label lblF = new Label();
                    lblF.Text = fName;
                    lblF.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    lblF.ForeColor = Color.FromArgb(15, 23, 42);
                    lblF.BackColor = Color.FromArgb(241, 245, 249);
                    lblF.Padding = new Padding(6, 4, 6, 4);
                    lblF.Margin = new Padding(3);
                    lblF.AutoSize = true;
                    flpInstalled.Controls.Add(lblF);
                }
            }
        }

        private void DoSearch()
        {
            string query = txtSearch.Text.Trim();
            string fontSource = GetLocalFontPath();

            RunOnBackground(() => {
                var localResults = new List<LocalFontGroup>();

                if (Directory.Exists(fontSource))
                {
                    var grouped = new Dictionary<string, LocalFontGroup>(StringComparer.OrdinalIgnoreCase);
                    var files = Directory.GetFiles(fontSource, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        string ext = Path.GetExtension(file).ToLower().TrimStart('.');
                        bool isWoff = (ext == "woff" || ext == "woff2" || ext == "ttf" || ext == "otf");
                        if (isWoff)
                        {
                            string relPath = file.Substring(fontSource.Length).TrimStart('\\', '/');
                            string parentFolder = Path.GetDirectoryName(relPath);
                            string filename = Path.GetFileNameWithoutExtension(file);

                            var parsed = ParseFontFilename(filename);
                            string familyPrefix = parsed.Family;
                            string weight = parsed.Weight;
                            string style = parsed.Style;
                            string vKey = weight + (style == "italic" ? "i" : "");

                            string fontId = familyPrefix;

                            if (!grouped.ContainsKey(fontId))
                            {
                                grouped[fontId] = new LocalFontGroup
                                {
                                    Id = fontId,
                                    Family = familyPrefix,
                                    Category = ext == "ttf" ? "Local TTF" : (ext == "otf" ? "Local OTF" : "Local Library"),
                                    Files = new List<LocalFontFile>()
                                };
                            }

                            grouped[fontId].Files.Add(new LocalFontFile
                            {
                                FilePath = file,
                                FileName = Path.GetFileName(file),
                                Extension = ext,
                                Weight = weight,
                                Style = style,
                                VariantKey = vKey
                            });
                        }
                    }

                    string normalizedQuery = query.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower();
                    foreach (var kv in grouped)
                    {
                        var group = kv.Value;
                        string normalizedFamily = group.Family.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower();
                        if (string.IsNullOrEmpty(query) || normalizedFamily.Contains(normalizedQuery))
                        {
                            localResults.Add(group);
                        }
                    }
                }

                var googleResults = new List<GoogleFontInfo>();
                if (!string.IsNullOrEmpty(query))
                {
                    var googleFonts = FetchGoogleFonts();
                    string queryLower = query.ToLower();
                    foreach (var g in googleFonts)
                    {
                        if (g.Family.ToLower().Contains(queryLower))
                            googleResults.Add(g);
                    }
                }

                var combined = new List<object>();
                // Google Fonts lên đầu, local fonts phía sau
                foreach (var g in googleResults) combined.Add(g);
                foreach (var l in localResults) combined.Add(l);

                this.BeginInvoke((Action)(() => {
                    PopulateResults(combined);
                }));
            }, null);
        }

        private void PopulateResults(List<object> results)
        {
            flpResults.SuspendLayout();
            flpResults.Controls.Clear();

            if (results.Count == 0)
            {
                var lblEmpty = new Label();
                lblEmpty.Text = "Không tìm thấy font nào phù hợp.";
                lblEmpty.ForeColor = Color.FromArgb(100, 116, 139);
                lblEmpty.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
                lblEmpty.Size = new Size(flpResults.Width - 40, 60);
                lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
                flpResults.Controls.Add(lblEmpty);
            }
            else
            {
                int totalCount = results.Count;
                int maxDisplay = 20;
                var displayList = results;
                if (totalCount > maxDisplay)
                {
                    displayList = results.GetRange(0, maxDisplay);

                    var lblLimit = new Label();
                    lblLimit.Text = string.Format("⚠️ Đang hiển thị {0}/{1} font. Hãy gõ từ khóa tìm kiếm để lọc kết quả.", maxDisplay, totalCount);
                    lblLimit.ForeColor = Color.FromArgb(139, 92, 246);
                    lblLimit.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    lblLimit.Size = new Size(flpResults.Width - 40, 30);
                    lblLimit.TextAlign = ContentAlignment.MiddleCenter;
                    flpResults.Controls.Add(lblLimit);
                }

                foreach (var r in displayList)
                {
                    Panel card = CreateFontCard(r);
                    flpResults.Controls.Add(card);
                }
            }

            flpResults.ResumeLayout();
            flpResults.Invalidate();
        }

        private Panel CreateFontCard(object r)
        {
            bool isLocal = r is LocalFontGroup;
            string family = isLocal ? ((LocalFontGroup)r).Family : ((GoogleFontInfo)r).Family;
            string category = isLocal ? "Local Library" : ((GoogleFontInfo)r).Category;
            List<string> variants = new List<string>();
            if (isLocal)
            {
                foreach (var f in ((LocalFontGroup)r).Files) variants.Add(f.VariantKey);
            }
            else
            {
                variants.AddRange(((GoogleFontInfo)r).Variants);
            }

            var uniqueVariants = new List<string>();
            foreach (var v in variants)
            {
                if (!uniqueVariants.Contains(v)) uniqueVariants.Add(v);
            }
            uniqueVariants.Sort();

            Panel pnlCard = new Panel();
            pnlCard.Width = flpResults.Width - 25;
            pnlCard.BackColor = Color.White;

            pnlCard.Paint += (s, pe) => {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1.5f))
                    pe.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            Label lblFamily = new Label();
            lblFamily.Text = family;
            lblFamily.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblFamily.ForeColor = Color.FromArgb(30, 41, 59);
            lblFamily.Location = new Point(12, 10);
            lblFamily.AutoSize = true;
            pnlCard.Controls.Add(lblFamily);

            Label lblBadge = new Label();
            lblBadge.Text = isLocal ? "LOCAL LIBRARY" : "GOOGLE FONTS";
            lblBadge.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblBadge.TextAlign = ContentAlignment.MiddleCenter;
            lblBadge.AutoSize = true;
            lblBadge.Location = new Point(pnlCard.Width - 120, 12);
            if (isLocal)
            {
                lblBadge.BackColor = Color.FromArgb(239, 246, 255);
                lblBadge.ForeColor = Color.FromArgb(37, 99, 235);
            }
            else
            {
                lblBadge.BackColor = Color.FromArgb(243, 232, 255);
                lblBadge.ForeColor = Color.FromArgb(147, 51, 234);
            }
            pnlCard.Controls.Add(lblBadge);

            Label lblCat = new Label();
            lblCat.Text = category;
            lblCat.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblCat.ForeColor = Color.FromArgb(100, 116, 139);
            lblCat.Location = new Point(12, 32);
            lblCat.AutoSize = true;
            pnlCard.Controls.Add(lblCat);

            FlowLayoutPanel flpVars = new FlowLayoutPanel();
            flpVars.Location = new Point(12, 52);
            int flpWidth = pnlCard.Width - 24;
            flpVars.Width = flpWidth;
            flpVars.MaximumSize = new Size(flpWidth, 0);
            flpVars.BackColor = Color.FromArgb(248, 250, 252);
            flpVars.AutoSize = true;
            flpVars.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpVars.AutoScroll = false;
            flpVars.FlowDirection = FlowDirection.LeftToRight;
            flpVars.WrapContents = true;
            pnlCard.Controls.Add(flpVars);

            var checkboxes = new List<CheckBox>();
            foreach (var v in uniqueVariants)
            {
                CheckBox chk = new CheckBox();
                chk.Text = v.Replace("i", " Italic").Replace("regular", "Regular");
                chk.Font = new Font("Segoe UI", 8f);
                chk.ForeColor = Color.FromArgb(51, 65, 85);
                chk.AutoSize = true;
                chk.Tag = v;
                if (!isLocal) chk.Checked = true;
                flpVars.Controls.Add(chk);
                checkboxes.Add(chk);
            }

            // Để AutoSize tự tính, đo lại sau khi đã add controls
            flpVars.PerformLayout();
            int varsHeight = flpVars.PreferredSize.Height;
            if (varsHeight < 26) varsHeight = 26;
            flpVars.Height = varsHeight;

            int Y_button = flpVars.Bottom + 10;
            pnlCard.Height = Y_button + 28 + 12;

            if (isLocal)
            {
                var localGrp = (LocalFontGroup)r;

                // Kiểm tra xem group có chỉ toàn TTF/OTF mà không có WOFF/WOFF2 không
                bool hasTtfOtfOnly = localGrp.Files.Count > 0
                    && !localGrp.Files.Exists(f => f.Extension == "woff" || f.Extension == "woff2")
                    && localGrp.Files.Exists(f => f.Extension == "ttf" || f.Extension == "otf");

                ModernButton btnInstall = new ModernButton();
                btnInstall.Text = hasTtfOtfOnly ? "🔄 Convert & Install" : "Cài đặt Font Local";
                btnInstall.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btnInstall.NormalColor = hasTtfOtfOnly ? Color.FromArgb(245, 158, 11) : Color.FromArgb(59, 130, 246);
                btnInstall.HoverColor = hasTtfOtfOnly ? Color.FromArgb(217, 119, 6) : Color.FromArgb(37, 99, 235);
                btnInstall.ForeColor = Color.White;
                btnInstall.Size = new Size(hasTtfOtfOnly ? 170 : 160, 28);
                btnInstall.Location = new Point(12, Y_button);
                btnInstall.Click += (s, e) => {
                    var selected = new List<string>();
                    foreach (var chk in checkboxes)
                    {
                        if (chk.Checked) selected.Add((string)chk.Tag);
                    }
                    if (selected.Count == 0)
                    {
                        MessageBox.Show("Vui lòng chọn ít nhất một biến thể font!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (hasTtfOtfOnly)
                        ConvertAndInstallFont(localGrp, selected);
                    else
                        InstallLocalFont(localGrp, selected);
                };
                pnlCard.Controls.Add(btnInstall);

                ModernButton btnToggle = new ModernButton();
                btnToggle.Text = "Chọn hết";
                btnToggle.Font = new Font("Segoe UI", 8.5f);
                btnToggle.NormalColor = Color.White;
                btnToggle.HoverColor = Color.FromArgb(243, 244, 246);
                btnToggle.BorderColor = Color.FromArgb(209, 213, 219);
                btnToggle.ForeColor = Color.FromArgb(55, 65, 81);
                btnToggle.Size = new Size(80, 28);
                btnToggle.Location = new Point(hasTtfOtfOnly ? 190 : 180, Y_button);
                btnToggle.Click += (s, e) => {
                    bool allChecked = true;
                    foreach (var chk in checkboxes) { if (!chk.Checked) allChecked = false; }
                    foreach (var chk in checkboxes) { chk.Checked = !allChecked; }
                    btnToggle.Text = allChecked ? "Chọn hết" : "Bỏ chọn";
                };
                pnlCard.Controls.Add(btnToggle);
            }
            else
            {
                var gInfo = (GoogleFontInfo)r;

                ModernButton btnImport = new ModernButton();
                btnImport.Text = "⚡ Add via @import";
                btnImport.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btnImport.NormalColor = Color.FromArgb(139, 92, 246);
                btnImport.HoverColor = Color.FromArgb(124, 58, 237);
                btnImport.ForeColor = Color.White;
                btnImport.Size = new Size(130, 28);
                btnImport.Location = new Point(12, Y_button);
                btnImport.Click += (s, e) => {
                    // Google Font tự động chèn hết các biến thể, không cần chọn
                    var selected = new List<string>(gInfo.Variants);
                    AddGoogleFontImport(gInfo, selected);
                };
                pnlCard.Controls.Add(btnImport);

                ModernButton btnDownload = new ModernButton();
                btnDownload.Text = "💾 Tải WOFF2 & Tích hợp";
                btnDownload.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btnDownload.NormalColor = Color.FromArgb(16, 185, 129);
                btnDownload.HoverColor = Color.FromArgb(5, 150, 105);
                btnDownload.ForeColor = Color.White;
                btnDownload.Size = new Size(180, 28);
                btnDownload.Location = new Point(150, Y_button);
                btnDownload.Click += (s, e) => {
                    // Google Font tự động chèn hết các biến thể, không cần chọn
                    var selected = new List<string>(gInfo.Variants);
                    DownloadGoogleFontSelfHost(gInfo, selected);
                };
                pnlCard.Controls.Add(btnDownload);

                ModernButton btnToggle = new ModernButton();
                btnToggle.Text = "Chọn hết";
                btnToggle.Font = new Font("Segoe UI", 8.5f);
                btnToggle.NormalColor = Color.White;
                btnToggle.HoverColor = Color.FromArgb(243, 244, 246);
                btnToggle.BorderColor = Color.FromArgb(209, 213, 219);
                btnToggle.ForeColor = Color.FromArgb(55, 65, 81);
                btnToggle.Size = new Size(70, 28);
                btnToggle.Location = new Point(340, Y_button);
                btnToggle.Click += (s, e) => {
                    bool allChecked = true;
                    foreach (var chk in checkboxes) { if (!chk.Checked) allChecked = false; }
                    foreach (var chk in checkboxes) { chk.Checked = !allChecked; }
                    btnToggle.Text = allChecked ? "Chọn hết" : "Bỏ chọn";
                };
                pnlCard.Controls.Add(btnToggle);
            }

            return pnlCard;
        }

        private void InstallLocalFont(LocalFontGroup group, List<string> selectedVariants)
        {
            try
            {
                string cleanFolderName = RemoveVietnameseDiacritics(group.Family);
                string destDir = Path.Combine(_projectDir, "assets", "fonts", cleanFolderName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
                string fontsCssDir = Path.GetDirectoryName(fontsCssPath);
                if (!Directory.Exists(fontsCssDir)) Directory.CreateDirectory(fontsCssDir);

                if (File.Exists(fontsCssPath))
                {
                    string currentCss = File.ReadAllText(fontsCssPath);
                    // Kiểm tra theo cả Family name (có dấu cách) và cleanFolder
                    if (currentCss.IndexOf("font-family: '" + group.Family + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        currentCss.IndexOf("font-family: \"" + group.Family + "\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        currentCss.IndexOf("font-family: '" + cleanFolderName + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        currentCss.IndexOf("font-family: \"" + cleanFolderName + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var dr = MessageBox.Show(string.Format("Font '{0}' đã tồn tại trong file fonts.css. Bạn có muốn tiếp tục cài đặt không?", group.Family), "Trùng font", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.No) return;
                    }
                }

                var copiedFiles = new Dictionary<string, List<LocalFontFile>>();
                foreach (var v in selectedVariants)
                {
                    var vFiles = group.Files.FindAll(f => f.VariantKey == v);
                    foreach (var vf in vFiles)
                    {
                        string destPath = Path.Combine(destDir, vf.FileName);
                        File.Copy(vf.FilePath, destPath, true);
                        if (!copiedFiles.ContainsKey(v)) copiedFiles[v] = new List<LocalFontFile>();
                        copiedFiles[v].Add(vf);
                    }
                }

                var sb = new StringBuilder();
                foreach (var kv in copiedFiles)
                {
                    string vKey = kv.Key;
                    var vFiles = kv.Value;
                    if (vFiles.Count == 0) continue;

                    string weight = vFiles[0].Weight;
                    string style = vFiles[0].Style;

                    sb.AppendLine("@font-face {");
                    sb.AppendLine(string.Format("  font-family: '{0}';", cleanFolderName));
                    sb.AppendLine(string.Format("  font-style: {0};", style));
                    sb.AppendLine(string.Format("  font-weight: {0};", weight));
                    sb.AppendLine("  font-display: swap;");

                    var extRank = new Dictionary<string, int> { {"woff2",0}, {"woff",1}, {"ttf",2}, {"otf",3} };
                    vFiles.Sort((a, b) => {
                        int ra = extRank.ContainsKey(a.Extension) ? extRank[a.Extension] : 4;
                        int rb = extRank.ContainsKey(b.Extension) ? extRank[b.Extension] : 4;
                        return ra.CompareTo(rb);
                    });

                    // Deduplicate theo FileName để tránh trùng src
                    var seenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var srcs = new List<string>();
                    foreach (var vf in vFiles)
                    {
                        if (!seenFileNames.Add(vf.FileName)) continue;
                        string format = vf.Extension == "ttf" ? "truetype" : (vf.Extension == "otf" ? "opentype" : vf.Extension);
                        srcs.Add(string.Format("url('../fonts/{0}/{1}') format('{2}')", cleanFolderName, vf.FileName, format));
                    }

                    sb.AppendLine("  src: " + string.Join(",\n       ", srcs.ToArray()) + ";");
                    sb.AppendLine("}");
                }

                string existing = File.Exists(fontsCssPath) ? File.ReadAllText(fontsCssPath) : "";
                string prefix = (string.IsNullOrEmpty(existing) || existing.EndsWith("\n")) ? "" : "\n";
                File.AppendAllText(fontsCssPath, prefix + sb.ToString());

                MessageBox.Show(string.Format("Đã cài đặt font '{0}' thành công và cập nhật fonts.css!", group.Family), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadInstalledFontsAndCss();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cài đặt font: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddGoogleFontImport(GoogleFontInfo info, List<string> selectedVariants)
        {
            try
            {
                string gUrl = GetGoogleFontUrl(info.Family, selectedVariants);
                string importUrl = string.Format("@import url({0});\n", gUrl);

                string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
                string fontsCssDir = Path.GetDirectoryName(fontsCssPath);
                if (!Directory.Exists(fontsCssDir)) Directory.CreateDirectory(fontsCssDir);

                string existing = File.Exists(fontsCssPath) ? File.ReadAllText(fontsCssPath) : "";

                // Kiểm tra trùng URL đầy đủ trước
                if (existing.IndexOf(gUrl, StringComparison.OrdinalIgnoreCase) >= 0) return;

                // Nếu có URL cũ (thiếu variants) của cùng font, xóa dòng đó trước
                string simplePattern = "@import url(" + "https://fonts.googleapis.com/css2?family=" + info.Family.Replace(" ", "+") + "&display=swap)";
                string simplePat2 = "@import url('" + "https://fonts.googleapis.com/css2?family=" + info.Family.Replace(" ", "+") + "&display=swap')";
                var cssLines = new List<string>(existing.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
                cssLines.RemoveAll(l => l.IndexOf(simplePattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        l.IndexOf(simplePat2, StringComparison.OrdinalIgnoreCase) >= 0);
                existing = string.Join("\n", cssLines).TrimEnd() + (cssLines.Count > 0 ? "\n" : "");
                File.WriteAllText(fontsCssPath, existing);

                string prefix = (string.IsNullOrEmpty(existing) || existing.EndsWith("\n")) ? "" : "\n";
                File.AppendAllText(fontsCssPath, prefix + importUrl);

                MessageBox.Show(string.Format("Đã thêm @import Google Font '{0}' thành công!", info.Family), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadInstalledFontsAndCss();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi liên kết Google Font: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DownloadGoogleFontSelfHost(GoogleFontInfo info, List<string> selectedVariants)
        {
            var drConfirm = MessageBox.Show(string.Format("Bạn có muốn tải các tệp font WOFF2 của '{0}' từ Google về máy và tự host trong dự án không?", info.Family), "Xác nhận tải", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (drConfirm == DialogResult.No) return;

            string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
            string fontsCssDir = Path.GetDirectoryName(fontsCssPath);
            if (!Directory.Exists(fontsCssDir)) Directory.CreateDirectory(fontsCssDir);

            if (File.Exists(fontsCssPath))
            {
                string currentCss = File.ReadAllText(fontsCssPath);
                if (currentCss.IndexOf("font-family: '" + info.Family + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    currentCss.IndexOf("font-family: \"" + info.Family + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var dr = MessageBox.Show(string.Format("Font '{0}' đã có trong file fonts.css. Bạn có muốn tiếp tục tải và đè không?", info.Family), "Trùng font", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.No) return;
                }
            }

            string gUrl = GetGoogleFontUrl(info.Family, selectedVariants);

            RunOnBackground(() => {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                    string cssContent = client.DownloadString(gUrl);

                    var fontFaceBlocks = Regex.Matches(cssContent, @"@font-face\s*\{([^}]+)\}");
                    if (fontFaceBlocks.Count == 0)
                    {
                        throw new Exception("Không thể phân tích dữ liệu CSS của Google Fonts.");
                    }

                    var sb = new StringBuilder();
                    string cleanFolderName = RemoveVietnameseDiacritics(info.Family);
                    string destDir = Path.Combine(_projectDir, "assets", "fonts", cleanFolderName);
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    foreach (Match blockMatch in fontFaceBlocks)
                    {
                        string block = blockMatch.Groups[1].Value;

                        var familyM = Regex.Match(block, @"font-family:\s*['""]([^'""]+)['""]");
                        var styleM = Regex.Match(block, @"font-style:\s*([a-zA-Z]+)");
                        var weightM = Regex.Match(block, @"font-weight:\s*(\d+)");
                        var urlM = Regex.Match(block, @"url\((https://fonts.gstatic.com/[^\)]+)\)");

                        if (familyM.Success && urlM.Success)
                        {
                            string family = familyM.Groups[1].Value;
                            string style = styleM.Success ? styleM.Groups[1].Value : "normal";
                            string weight = weightM.Success ? weightM.Groups[1].Value : "400";
                            string downloadUrl = urlM.Groups[1].Value;

                            string fileName = Path.GetFileName(downloadUrl);
                            if (fileName.Contains("?")) fileName = fileName.Split('?')[0];
                            if (!fileName.EndsWith(".woff2")) fileName += ".woff2";

                            fileName = weight + (style == "italic" ? "i" : "") + "_" + fileName;

                            string destPath = Path.Combine(destDir, fileName);
                            client.DownloadFile(downloadUrl, destPath);

                            sb.AppendLine("@font-face {");
                            sb.AppendLine(string.Format("  font-family: '{0}';", family));
                            sb.AppendLine(string.Format("  font-style: {0};", style));
                            sb.AppendLine(string.Format("  font-weight: {0};", weight));
                            sb.AppendLine("  font-display: swap;");
                            sb.AppendLine(string.Format("  src: url('../fonts/{0}/{1}') format('woff2');", cleanFolderName, fileName));
                            sb.AppendLine("}");
                        }
                    }

                    string existing = File.Exists(fontsCssPath) ? File.ReadAllText(fontsCssPath) : "";
                    string prefix = (string.IsNullOrEmpty(existing) || existing.EndsWith("\n")) ? "" : "\n";
                    File.AppendAllText(fontsCssPath, prefix + sb.ToString());
                }
            }, () => {
                MessageBox.Show(string.Format("Đã tải và cài đặt self-host font '{0}' thành công!", info.Family), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadInstalledFontsAndCss();
            });
        }

        private string GetGoogleFontUrl(string family, List<string> selectedVariants)
        {
            string urlParams = "";
            if (selectedVariants != null && selectedVariants.Count > 0)
            {
                var normalWeights = new List<int>();
                var italicWeights = new List<int>();
                foreach (var v in selectedVariants)
                {
                    string variantStr = v.ToString();
                    if (variantStr.EndsWith("i") || variantStr.Contains("italic"))
                    {
                        string wStr = Regex.Match(variantStr, @"\d+").Value;
                        int w = 400;
                        if (int.TryParse(wStr, out w)) italicWeights.Add(w);
                        else if (variantStr == "italic") italicWeights.Add(400);
                    }
                    else
                    {
                        string wStr = Regex.Match(variantStr, @"\d+").Value;
                        int w = 400;
                        if (int.TryParse(wStr, out w)) normalWeights.Add(w);
                        else if (variantStr == "regular") normalWeights.Add(400);
                    }
                }

                normalWeights.Sort();
                italicWeights.Sort();

                if (italicWeights.Count > 0)
                {
                    var pairs = new List<string>();
                    foreach (var w in normalWeights) pairs.Add("0," + w);
                    foreach (var w in italicWeights) pairs.Add("1," + w);
                    urlParams = ":ital,wght@" + string.Join(";", pairs.ToArray());
                }
                else if (normalWeights.Count > 0)
                {
                    var normalWeightStrings = new List<string>();
                    foreach (var w in normalWeights) normalWeightStrings.Add(w.ToString());
                    urlParams = ":wght@" + string.Join(";", normalWeightStrings.ToArray());
                }
            }

            return "https://fonts.googleapis.com/css2?family=" + family.Replace(" ", "+") + urlParams + "&display=swap";
        }

        private string GetLocalFontPath()
        {
            var globalCfg = DeployDemoForm.LoadGlobalConfig();
            string fontPath = "";
            if (globalCfg.TryGetValue("font_source_path", out fontPath))
            {
                if (Directory.Exists(fontPath)) return fontPath;
            }

            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts");
            if (!Directory.Exists(defaultPath))
            {
                try { Directory.CreateDirectory(defaultPath); } catch { }
            }
            return defaultPath;
        }

        private void SaveLocalFontPath(string path)
        {
            var globalCfg = DeployDemoForm.LoadGlobalConfig();
            globalCfg["font_source_path"] = path;
            DeployDemoForm.SaveGlobalConfig(globalCfg);
        }

        public static ParsedFont ParseFontFilename(string filename)
        {
            string weight = "400";
            if (Regex.IsMatch(filename, "(thin|100)", RegexOptions.IgnoreCase)) weight = "100";
            else if (Regex.IsMatch(filename, "(extralight|200)", RegexOptions.IgnoreCase)) weight = "200";
            else if (Regex.IsMatch(filename, "(light|300)", RegexOptions.IgnoreCase)) weight = "300";
            else if (Regex.IsMatch(filename, "(medium|500)", RegexOptions.IgnoreCase)) weight = "500";
            else if (Regex.IsMatch(filename, "(semibold|600)", RegexOptions.IgnoreCase)) weight = "600";
            else if (Regex.IsMatch(filename, "(bold|700)", RegexOptions.IgnoreCase)) weight = "700";
            else if (Regex.IsMatch(filename, "(extrabold|800)", RegexOptions.IgnoreCase)) weight = "800";
            else if (Regex.IsMatch(filename, "(black|900)", RegexOptions.IgnoreCase)) weight = "900";

            string style = Regex.IsMatch(filename, "italic", RegexOptions.IgnoreCase) ? "italic" : "normal";

            string cleanFamily = Regex.Replace(filename, @"[-_]?(thin|100|extralight|200|light|300|medium|500|semibold|demibold|600|bold|700|extrabold|800|black|heavy|900|regular|italic|normal|it|rg)", "", RegexOptions.IgnoreCase);
            cleanFamily = cleanFamily.Trim('-', '_', ' ');
            if (string.IsNullOrEmpty(cleanFamily))
            {
                cleanFamily = filename;
            }

            return new ParsedFont
            {
                Family = cleanFamily,
                Weight = weight,
                Style = style
            };
        }

        public static string RemoveVietnameseDiacritics(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            string[] unicode = new string[]
            {
                "a", "á|à|ả|ã|ạ|ă|ắ|ằ|ẳ|ẵ|ặ|â|ấ|ầ|ẩ|ẫ|ậ",
                "A", "Á|À|Ả|Ã|Ạ|Ă|Ắ|Ằ|Ẳ|Ẵ|Ặ|Â|Ấ|Ầ|Ẩ|Ẫ|Ậ",
                "d", "đ",
                "D", "Đ",
                "e", "é|è|ẻ|ẽ|ẹ|ê|ế|ề|ể|ễ|ệ",
                "E", "É|È|Ẻ|Ẽ|Ẹ|Ê|Ế|Ề|Ể|Ễ|Ệ",
                "i", "í|ì|ỉ|ĩ|ị",
                "I", "Í|Ì|Ỉ|Ĩ|Ị",
                "o", "ó|ò|ỏ|õ|ọ|ô|ố|ồ|ổ|ỗ|ộ|ơ|ớ|ờ|ở|ỡ|ợ",
                "O", "Ó|Ò|Ỏ|Õ|Ọ|Ô|Ố|Ồ|Ổ|Ỗ|Ộ|Ơ|Ớ|Ờ|Ở|Ỡ|Ợ",
                "u", "ú|ù|ủ|ũ|ụ|ư|ứ|ừ|ử|ữ|ự",
                "U", "Ú|Ù|Ủ|Ũ|Ụ|Ư|Ứ|Ừ|Ử|Ữ|Ự",
                "y", "ý|ỳ|ỷ|ỹ|ỵ",
                "Y", "Ý|Ỳ|Ỷ|Ỹ|Ỵ"
            };

            for (int i = 0; i < unicode.Length; i += 2)
            {
                string nonUni = unicode[i];
                string uni = unicode[i + 1];
                str = Regex.Replace(str, "(" + uni + ")", nonUni);
            }

            str = Regex.Replace(str, "[^a-zA-Z0-9]", "");
            return str;
        }

        private List<string> ParseInstalledFonts()
        {
            var list = new List<string>();
            string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
            if (!File.Exists(fontsCssPath)) return list;

            string cssContent = File.ReadAllText(fontsCssPath);

            var fontFaceMatches = Regex.Matches(cssContent, @"font-family:\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase);
            foreach (Match m in fontFaceMatches)
            {
                string family = m.Groups[1].Value;
                if (!list.Contains(family)) list.Add(family);
            }

            var importMatches = Regex.Matches(cssContent, @"family=([^&:'""\)]+)", RegexOptions.IgnoreCase);
            foreach (Match m in importMatches)
            {
                string family = Uri.UnescapeDataString(m.Groups[1].Value).Replace("+", " ");
                if (!list.Contains(family)) list.Add(family);
            }

            return list;
        }

        private List<GoogleFontInfo> FetchGoogleFonts()
        {
            string cacheFile = ConfigHelper.GetDataFilePath("google_fonts_cache.txt");
            if (File.Exists(cacheFile) && (DateTime.Now - File.GetLastWriteTime(cacheFile)).TotalDays < 7)
            {
                var cached = LoadGoogleFontsFromCache();
                if (cached.Count > 0) return cached;
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                    string json = client.DownloadString("https://fonts.google.com/metadata/fonts");
                    var list = ParseGoogleFontsJson(json);
                    if (list.Count > 0)
                    {
                        SaveGoogleFontsToCache(list);
                        return list;
                    }
                }
            }
            catch { }

            var fallbackList = LoadGoogleFontsFromCache();
            if (fallbackList.Count == 0)
            {
                fallbackList = new List<GoogleFontInfo>();
                foreach (var line in FallbackGoogleFonts.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = line.Trim().Split('|');
                    if (parts.Length >= 2)
                    {
                        var info = new GoogleFontInfo
                        {
                            Family = parts[0],
                            Category = parts[1],
                            Variants = new List<string>()
                        };
                        if (parts.Length > 2) info.Variants.AddRange(parts[2].Split(','));
                        fallbackList.Add(info);
                    }
                }
                SaveGoogleFontsToCache(fallbackList);
            }
            return fallbackList;
        }

        private static List<GoogleFontInfo> ParseGoogleFontsJson(string json)
        {
            var list = new List<GoogleFontInfo>();
            int startIndex = json.IndexOf("\"familyMetadataList\"");
            if (startIndex < 0) return list;

            int braceCount = 0;
            int len = json.Length;
            StringBuilder sb = null;
            bool inString = false;

            for (int i = startIndex; i < len; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        if (braceCount == 0)
                        {
                            sb = new StringBuilder();
                        }
                        braceCount++;
                    }

                    if (braceCount > 0)
                    {
                        sb.Append(c);
                    }

                    if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && sb != null)
                        {
                            string block = sb.ToString();
                            ParseFontBlock(block, list);
                        }
                    }
                }
                else if (braceCount > 0)
                {
                    sb.Append(c);
                }
            }
            return list;
        }

        private static void ParseFontBlock(string block, List<GoogleFontInfo> list)
        {
            var familyMatch = Regex.Match(block, @"""family""\s*:\s*""([^""]+)""");
            if (!familyMatch.Success) return;
            string family = familyMatch.Groups[1].Value;

            var categoryMatch = Regex.Match(block, @"""category""\s*:\s*""([^""]+)""");
            string category = categoryMatch.Success ? categoryMatch.Groups[1].Value : "";

            int fontsIdx = block.IndexOf("\"fonts\"");
            var variants = new List<string>();
            if (fontsIdx > 0)
            {
                string fontsSub = block.Substring(fontsIdx);
                var matches = Regex.Matches(fontsSub, @"""([^""]+)""\s*:\s*\{");
                foreach (Match m in matches)
                {
                    string v = m.Groups[1].Value;
                    if (v != "thickness" && v != "slant" && v != "width" && v != "lineHeight")
                    {
                        variants.Add(v);
                    }
                }
            }

            list.Add(new GoogleFontInfo { Family = family, Category = category, Variants = variants });
        }

        private static List<GoogleFontInfo> LoadGoogleFontsFromCache()
        {
            var list = new List<GoogleFontInfo>();
            string cacheFile = ConfigHelper.GetDataFilePath("google_fonts_cache.txt");
            if (!File.Exists(cacheFile)) return list;

            foreach (var line in File.ReadAllLines(cacheFile, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('|');
                if (parts.Length >= 2)
                {
                    var info = new GoogleFontInfo
                    {
                        Family = parts[0],
                        Category = parts[1],
                        Variants = new List<string>()
                    };
                    if (parts.Length > 2)
                    {
                        info.Variants.AddRange(parts[2].Split(','));
                    }
                    list.Add(info);
                }
            }
            return list;
        }

        private static void SaveGoogleFontsToCache(List<GoogleFontInfo> list)
        {
            string cacheFile = ConfigHelper.GetDataFilePath("google_fonts_cache.txt");
            var sb = new StringBuilder();
            foreach (var info in list)
            {
                sb.AppendLine(string.Format("{0}|{1}|{2}", info.Family, info.Category, string.Join(",", info.Variants.ToArray())));
            }
            File.WriteAllText(cacheFile, sb.ToString(), Encoding.UTF8);
        }

        private void RunOnBackground(Action action, Action onCompleted)
        {
            this.Cursor = Cursors.WaitCursor;
            txtSearch.Enabled = false;

            new Thread(() => {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((Action)(() => {
                        MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                finally
                {
                    this.BeginInvoke((Action)(() => {
                        this.Cursor = Cursors.Default;
                        txtSearch.Enabled = true;
                        if (onCompleted != null) onCompleted();
                    }));
                }
            }).Start();
        }

        private void ApplyRoundedRegion(Control control, int radius)
        {
            control.Region = new Region(GetRoundedRect(new Rectangle(0, 0, control.Width, control.Height), radius));
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            gp.AddArc(bounds.X + bounds.Width - d, bounds.Y, d, d, 270, 90);
            gp.AddArc(bounds.X + bounds.Width - d, bounds.Y + bounds.Height - d, d, d, 0, 90);
            gp.AddArc(bounds.X, bounds.Y + bounds.Height - d, d, d, 90, 90);
            gp.CloseAllFigures();
            return gp;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        // ============================================================
        // TTF/OTF → WOFF Converter (pure C# / .NET 4.x)
        // ============================================================

        private void ConvertAndInstallFont(LocalFontGroup group, List<string> selectedVariants)
        {
            try
            {
                string cleanFolderName = RemoveVietnameseDiacritics(group.Family);
                string destDir = Path.Combine(_projectDir, "assets", "fonts", cleanFolderName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
                string fontsCssDir = Path.GetDirectoryName(fontsCssPath);
                if (!Directory.Exists(fontsCssDir)) Directory.CreateDirectory(fontsCssDir);

                if (File.Exists(fontsCssPath))
                {
                    string cur = File.ReadAllText(fontsCssPath);
                    // Kiểm tra theo cả Family name (có dấu cách) và cleanFolder (không dấu cách)
                    if (cur.IndexOf("font-family: '" + group.Family + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cur.IndexOf("font-family: \"" + group.Family + "\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cur.IndexOf("font-family: '" + cleanFolderName + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cur.IndexOf("font-family: \"" + cleanFolderName + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var dr = MessageBox.Show(string.Format("Font '{0}' đã tồn tại trong fonts.css. Tiếp tục?", group.Family),
                            "Trùng font", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.No) return;
                    }
                }

                // Nhóm file theo variant
                var copiedFiles = new Dictionary<string, List<LocalFontFile>>();
                foreach (var v in selectedVariants)
                {
                    var vFiles = group.Files.FindAll(f => f.VariantKey == v);
                    foreach (var vf in vFiles)
                    {
                        if (!copiedFiles.ContainsKey(v)) copiedFiles[v] = new List<LocalFontFile>();
                        copiedFiles[v].Add(vf);
                    }
                }

                var sb = new StringBuilder();

                foreach (var kv in copiedFiles)
                {
                    var vFiles = kv.Value;
                    if (vFiles.Count == 0) continue;

                    string weight = vFiles[0].Weight;
                    string style  = vFiles[0].Style;

                    // Lấy file TTF hoặc OTF đầu tiên trong variant
                    LocalFontFile srcFile = vFiles.Find(f => f.Extension == "ttf");
                    if (srcFile == null) srcFile = vFiles.Find(f => f.Extension == "otf");
                    if (srcFile == null) continue;

                    string baseName = Path.GetFileNameWithoutExtension(srcFile.FileName);
                    string woffName  = baseName + ".woff";
                    string woff2Name = baseName + ".woff2";
                    string origName  = srcFile.FileName;

                    // Copy file gốc TTF/OTF vào dự án
                    File.Copy(srcFile.FilePath, Path.Combine(destDir, origName), true);

                    // Convert → WOFF
                    byte[] ttfBytes = File.ReadAllBytes(srcFile.FilePath);
                    byte[] woffBytes = ConvertToWoff(ttfBytes);
                    File.WriteAllBytes(Path.Combine(destDir, woffName), woffBytes);

                    // WOFF2: thử dùng file TTF wrap đơn giản (báo rõ chỉ là WOFF fallback)
                    // Nếu muốn WOFF2 thực sự cần Brotli (không có trong .NET 4.x);
                    // ta copy WOFF làm WOFF2 placeholder — trình duyệt sẽ fallback xuống WOFF
                    // Lưu ý: file .woff2 thực ra là WOFF nhưng khai báo rõ trong src
                    // Trình duyệt hiện đại đọc WOFF2 theo Brotli — nên ta chỉ dùng WOFF + TTF

                    sb.AppendLine("@font-face {");
                    sb.AppendLine(string.Format("  font-family: '{0}';", cleanFolderName));
                    sb.AppendLine(string.Format("  font-style: {0};", style));
                    sb.AppendLine(string.Format("  font-weight: {0};", weight));
                    sb.AppendLine("  font-display: swap;");
                    sb.AppendLine(string.Format("  src: url('../fonts/{0}/{1}') format('woff'),", cleanFolderName, woffName));
                    sb.AppendLine(string.Format("       url('../fonts/{0}/{1}') format('{2}');",
                        cleanFolderName, origName,
                        srcFile.Extension == "ttf" ? "truetype" : "opentype"));
                    sb.AppendLine("}");
                }

                string existing = File.Exists(fontsCssPath) ? File.ReadAllText(fontsCssPath) : "";
                string prefix = (string.IsNullOrEmpty(existing) || existing.EndsWith("\n")) ? "" : "\n";
                File.AppendAllText(fontsCssPath, prefix + sb.ToString());

                MessageBox.Show(
                    string.Format("Đã convert và cài đặt font '{0}' thành công!\n(TTF/OTF → WOFF + giữ nguyên file gốc làm fallback)", group.Family),
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadInstalledFontsAndCss();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi convert font: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Convert TTF/OTF bytes sang định dạng WOFF (W3C spec) thuần C#</summary>
        private static byte[] ConvertToWoff(byte[] sfntData)
        {
            // --- đọc sfnt header ---
            uint flavor    = RdU32(sfntData, 0);
            int numTables  = RdU16(sfntData, 4);

            // --- đọc table records ---
            var tables = new List<WoffTable>();
            for (int i = 0; i < numTables; i++)
            {
                int off = 12 + i * 16;
                tables.Add(new WoffTable {
                    Tag      = RdU32(sfntData, off),
                    CheckSum = RdU32(sfntData, off + 4),
                    Offset   = (int)RdU32(sfntData, off + 8),
                    Length   = (int)RdU32(sfntData, off + 12)
                });
            }

            // --- nén từng table bằng zlib (DeflateStream + zlib header/checksum) ---
            var compressed = new byte[numTables][];
            for (int i = 0; i < numTables; i++)
            {
                byte[] raw = new byte[tables[i].Length];
                Array.Copy(sfntData, tables[i].Offset, raw, 0, raw.Length);
                byte[] zlibData = ZlibDeflate(raw);
                compressed[i] = (zlibData.Length < raw.Length) ? zlibData : raw;
            }

            // --- tính offset từng table (align 4 bytes) ---
            int woffHeaderSz  = 44;
            int woffTableDirSz = numTables * 20;
            int dataStart     = woffHeaderSz + woffTableDirSz;
            var tableOffsets  = new int[numTables];
            int cursor        = dataStart;
            for (int i = 0; i < numTables; i++)
            {
                tableOffsets[i] = cursor;
                cursor += compressed[i].Length;
                if (cursor % 4 != 0) cursor += 4 - (cursor % 4);
            }
            int totalWoffSize = cursor;

            // --- tính totalSfntSize ---
            uint totalSfntSize = (uint)(12 + numTables * 16);
            foreach (var t in tables)
            {
                int pad = t.Length;
                if (pad % 4 != 0) pad += 4 - (pad % 4);
                totalSfntSize += (uint)pad;
            }

            var woff = new byte[totalWoffSize];
            int p = 0;

            // --- WOFF Header (44 bytes) ---
            WrU32(woff, p, 0x774F4646); p += 4; // 'wOFF'
            WrU32(woff, p, flavor);     p += 4;
            WrU32(woff, p, (uint)totalWoffSize); p += 4;
            WrU16(woff, p, (ushort)numTables);   p += 2;
            WrU16(woff, p, 0); p += 2;           // reserved
            WrU32(woff, p, totalSfntSize); p += 4;
            WrU16(woff, p, 1); p += 2;           // majorVersion
            WrU16(woff, p, 0); p += 2;           // minorVersion
            WrU32(woff, p, 0); p += 4;           // metaOffset
            WrU32(woff, p, 0); p += 4;           // metaLength
            WrU32(woff, p, 0); p += 4;           // metaOrigLength
            WrU32(woff, p, 0); p += 4;           // privOffset
            WrU32(woff, p, 0); p += 4;           // privLength

            // --- Table Directory ---
            for (int i = 0; i < numTables; i++)
            {
                WrU32(woff, p, tables[i].Tag);                       p += 4;
                WrU32(woff, p, (uint)tableOffsets[i]);               p += 4;
                WrU32(woff, p, (uint)compressed[i].Length);          p += 4;
                WrU32(woff, p, (uint)tables[i].Length);              p += 4;
                WrU32(woff, p, tables[i].CheckSum);                  p += 4;
            }

            // --- Table Data ---
            for (int i = 0; i < numTables; i++)
                Array.Copy(compressed[i], 0, woff, tableOffsets[i], compressed[i].Length);

            return woff;
        }

        private static byte[] ZlibDeflate(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78); ms.WriteByte(0x9C); // zlib header (default compression)
                using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
                    ds.Write(data, 0, data.Length);
                // Adler-32 checksum (big-endian)
                uint a = Adler32(data);
                ms.WriteByte((byte)(a >> 24)); ms.WriteByte((byte)(a >> 16));
                ms.WriteByte((byte)(a >>  8)); ms.WriteByte((byte)(a & 0xFF));
                return ms.ToArray();
            }
        }

        private static uint Adler32(byte[] data)
        {
            uint s1 = 1, s2 = 0;
            foreach (byte b in data) { s1 = (s1 + b) % 65521; s2 = (s2 + s1) % 65521; }
            return (s2 << 16) | s1;
        }

        private static uint RdU32(byte[] b, int o)
        { return (uint)((b[o] << 24) | (b[o+1] << 16) | (b[o+2] << 8) | b[o+3]); }
        private static int  RdU16(byte[] b, int o)
        { return (b[o] << 8) | b[o+1]; }
        private static void WrU32(byte[] b, int o, uint v)
        { b[o]=(byte)(v>>24); b[o+1]=(byte)(v>>16); b[o+2]=(byte)(v>>8); b[o+3]=(byte)(v&0xFF); }
        private static void WrU16(byte[] b, int o, ushort v)
        { b[o]=(byte)(v>>8); b[o+1]=(byte)(v&0xFF); }

        private class WoffTable
        {
            public uint Tag; public uint CheckSum; public int Offset; public int Length;
        }
    }

    public class ParsedFont
    {
        public string Family { get; set; }
        public string Weight { get; set; }
        public string Style { get; set; }
    }

    public class CaughtEmail
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Date { get; set; }
        public bool IsHtml { get; set; }
    }
}
