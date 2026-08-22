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

        private static string GetRelativePath(string rootDir, string fullPath)
        {
            string root = rootDir;
            if (root.StartsWith(@"\\?\"))
            {
                root = root.Substring(4);
            }
            string file = fullPath;
            if (file.StartsWith(@"\\?\"))
            {
                file = file.Substring(4);
            }

            root = Path.GetFullPath(root).Replace('\\', '/').TrimEnd('/');
            file = Path.GetFullPath(file).Replace('\\', '/');

            if (file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                string rel = file.Substring(root.Length).TrimStart('/');
                return rel;
            }

            return Path.GetFileName(file);
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
                long processedBytes = 0;
                int processedFiles = 0;
                DateTime lastLogTime = DateTime.MinValue;

                foreach (var fileInfo in files)
                {
                    string file = fileInfo.FullPath;
                    string entryName = GetRelativePath(sourceDir, file);
                    if (string.IsNullOrEmpty(entryName))
                        continue;
                    
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
                        string entryName = GetRelativePath(sourceDir, dir);
                        if (string.IsNullOrEmpty(entryName))
                            continue;
                        
                        entryName = entryName.Replace('\\', '/').TrimEnd('/') + "/";
                        if (entryName == "/")
                            continue;
                        
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
                                bool downloadSuccess = DownloadFtp(remoteEnvUrl2, ftpCreds, remoteEnvTemp2, out ftpErr2);
                                if (downloadSuccess && File.Exists(remoteEnvTemp2) && new FileInfo(remoteEnvTemp2).Length > 0)
                                {
                                    List<string> envLines = new List<string>();
                                    envLines.AddRange(File.ReadAllLines(remoteEnvTemp2));
                                    File.Delete(remoteEnvTemp2);

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
                                else
                                {
                                    if (File.Exists(remoteEnvTemp2)) File.Delete(remoteEnvTemp2);
                                    AppendLog("⚠️ Cảnh báo: Không thể tải file .env từ hosting. Bỏ qua việc đồng bộ mật khẩu .env để tránh lỗi tệp tin.", colorOrange);
                                }
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
                string escLocal = localFile.Replace("\"", "\\\"");
                string escPwd = userPwd.Replace("\"", "\\\"");
                string escUrl = ftpUrl.Replace("\"", "\\\"");
                
                string cmd = string.Format("-T \"{0}\" --ftp-create-dirs -u \"{1}\" \"{2}\" --ssl-reqd --ftp-ssl --insecure --disable-epsv -m 300 -s", escLocal, escPwd, escUrl);
                var psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                var proc = Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                cmd = string.Format("-T \"{0}\" --ftp-create-dirs -u \"{1}\" \"{2}\" --disable-epsv -m 300 -s", escLocal, escPwd, escUrl);
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
                string escLocal = localFile.Replace("\"", "\\\"");
                string escPwd = userPwd.Replace("\"", "\\\"");
                string escUrl = ftpUrl.Replace("\"", "\\\"");
                
                string cmd = string.Format("-o \"{0}\" -u \"{1}\" \"{2}\" --ssl-reqd --ftp-ssl --insecure --disable-epsv -m 30 -s", escLocal, escPwd, escUrl);
                var psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                var proc = Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                cmd = string.Format("-o \"{0}\" -u \"{1}\" \"{2}\" --disable-epsv -m 30 -s", escLocal, escPwd, escUrl);
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
                string escPwd = userPwd.Replace("\"", "\\\"");
                string escUrl = ftpUrl.Replace("\"", "\\\"");
                
                string cmd = string.Format("-u \"{0}\" \"{1}\" -X \"DELE\" --ssl-reqd --ftp-ssl --insecure --disable-epsv -s", escPwd, escUrl);
                var psi = new ProcessStartInfo(curlPath, cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                var proc = Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
                
                cmd = string.Format("-u \"{0}\" \"{1}\" -X \"DELE\" --disable-epsv -s", escPwd, escUrl);
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
                using (var wc = new TimeoutWebClient(600000)) // 10-minute timeout (600000 ms)
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
                using (var wc = new TimeoutWebClient(600000)) // 10-minute timeout (600000 ms)
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
}
