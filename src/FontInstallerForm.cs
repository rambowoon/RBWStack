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
                    FlowLayoutPanel pnlBadge = new FlowLayoutPanel();
                    pnlBadge.FlowDirection = FlowDirection.LeftToRight;
                    pnlBadge.BackColor = Color.FromArgb(241, 245, 249);
                    pnlBadge.AutoSize = true;
                    pnlBadge.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    pnlBadge.Margin = new Padding(3);
                    pnlBadge.Padding = new Padding(4, 2, 4, 2);
                    pnlBadge.WrapContents = false;

                    Label lblF = new Label();
                    lblF.Text = fName;
                    lblF.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    lblF.ForeColor = Color.FromArgb(15, 23, 42);
                    lblF.AutoSize = true;
                    lblF.Margin = new Padding(0, 3, 2, 0);
                    pnlBadge.Controls.Add(lblF);

                    Label btnDel = new Label();
                    btnDel.Text = "❌";
                    btnDel.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                    btnDel.ForeColor = Color.FromArgb(239, 68, 68);
                    btnDel.Cursor = Cursors.Hand;
                    btnDel.AutoSize = true;
                    btnDel.Margin = new Padding(2, 4, 0, 0);
                    string targetFontName = fName;
                    btnDel.Click += (s, e) => {
                        UninstallFont(targetFontName);
                    };
                    pnlBadge.Controls.Add(btnDel);

                    flpInstalled.Controls.Add(pnlBadge);
                }
            }
        }

        private void UninstallFont(string fontName)
        {
            var dr = MessageBox.Show(string.Format("Bạn có chắc chắn muốn gỡ bỏ font '{0}' ra khỏi dự án không?", fontName),
                "Xác nhận gỡ bỏ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.No) return;

            try
            {
                string fontsCssPath = Path.Combine(_projectDir, "assets", "css", "fonts.css");
                if (File.Exists(fontsCssPath))
                {
                    string cssContent = File.ReadAllText(fontsCssPath);
                    
                    // 1. Remove @font-face blocks matching font-family
                    string fontFacePattern = @"@font-face\s*\{[^}]*font-family:\s*['""]" + Regex.Escape(fontName) + @"['""][^}]*\}";
                    cssContent = Regex.Replace(cssContent, fontFacePattern, "", RegexOptions.IgnoreCase);

                    // 2. Remove @import Google Fonts
                    string cleanFamily = fontName.Replace(" ", "+");
                    var lines = new List<string>(cssContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
                    lines.RemoveAll(l => l.IndexOf("family=" + cleanFamily, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         l.IndexOf("family=" + Uri.EscapeDataString(fontName), StringComparison.OrdinalIgnoreCase) >= 0);
                    cssContent = string.Join("\n", lines);
                    
                    // Clean up empty lines
                    cssContent = Regex.Replace(cssContent, @"^\s*$\n", "", RegexOptions.Multiline);
                    
                    File.WriteAllText(fontsCssPath, cssContent.Trim() + "\n");
                }

                // 3. Delete font directory
                string cleanFolderName = RemoveVietnameseDiacritics(fontName);
                string fontDir = Path.Combine(_projectDir, "assets", "fonts", cleanFolderName);
                if (Directory.Exists(fontDir))
                {
                    Directory.Delete(fontDir, true);
                }

                MessageBox.Show(string.Format("Đã gỡ bỏ font '{0}' thành công!", fontName), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                LoadInstalledFontsAndCss();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gỡ bỏ font: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        bool matchFamily = string.IsNullOrEmpty(query) || normalizedFamily.Contains(normalizedQuery);
                        bool matchFile = !string.IsNullOrEmpty(query) && group.Files.Exists(f => 
                            f.FileName.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower().Contains(normalizedQuery) || 
                            f.FilePath.ToLower().Contains(query.ToLower()));

                        if (matchFamily || matchFile)
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

                bool isDuplicate = false;
                if (File.Exists(fontsCssPath))
                {
                    string currentCss = File.ReadAllText(fontsCssPath);
                    // Kiểm tra theo cả Family name (có dấu cách) và cleanFolder
                    if (currentCss.IndexOf("font-family: '" + group.Family + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        currentCss.IndexOf("font-family: \"" + group.Family + "\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        currentCss.IndexOf("font-family: '" + cleanFolderName + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        currentCss.IndexOf("font-family: \"" + cleanFolderName + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isDuplicate = true;
                        var dr = MessageBox.Show(string.Format("Font '{0}' đã tồn tại trong file fonts.css. Bạn có muốn tiếp tục cài đặt không?", group.Family), "Trùng font", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.No) return;
                    }
                }

                if (!isDuplicate)
                {
                    var dr = MessageBox.Show(string.Format("Bạn có chắc chắn muốn cài đặt local font '{0}' vào dự án không?", group.Family), "Xác nhận cài đặt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.No) return;
                }

                var copiedFiles = new Dictionary<string, List<LocalFontFile>>();
                foreach (var v in selectedVariants)
                {
                    var vFiles = group.Files.FindAll(f => f.VariantKey == v);
                    foreach (var vf in vFiles)
                    {
                        if (vf.Extension != "woff" && vf.Extension != "woff2") continue;
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

                string cleanFamily = info.Family.Replace(" ", "+");
                bool isDuplicate = false;

                // Kiểm tra trùng URL đầy đủ trước
                if (existing.IndexOf(gUrl, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MessageBox.Show(string.Format("Google Font '{0}' với các biến thể này đã được thêm trong fonts.css rồi.", info.Family), 
                        "Thông báo trùng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Kiểm tra trùng font family trong file fonts.css
                if (existing.IndexOf("family=" + cleanFamily, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    existing.IndexOf("font-family: '" + info.Family + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    existing.IndexOf("font-family: \"" + info.Family + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isDuplicate = true;
                    var dr = MessageBox.Show(string.Format("Google Font '{0}' đã tồn tại trong file fonts.css.\nBạn có chắc chắn muốn cài đặt đè/cập nhật không?", info.Family), 
                        "Xác nhận cài đặt trùng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.No) return;
                }

                if (!isDuplicate)
                {
                    var dr = MessageBox.Show(string.Format("Bạn có chắc chắn muốn liên kết Google Font '{0}' vào dự án không?", info.Family), 
                        "Xác nhận cài đặt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.No) return;
                }

                // Nếu có bất kỳ URL @import cũ nào của cùng font này, xóa bỏ dòng đó bằng Regex trước khi thêm
                string cleanFamilyPattern = info.Family.Replace(" ", @"\+");
                string regexPattern = "@import\\s+url\\(['\"']?https://fonts\\.googleapis\\.com/css2\\?family=" + cleanFamilyPattern + "(?::[^'\"\\s]+)?['\"']?\\);?";
                existing = Regex.Replace(existing, regexPattern, "", RegexOptions.IgnoreCase);
                existing = existing.TrimEnd() + (string.IsNullOrEmpty(existing) ? "" : "\n");
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
                        var unicodeM = Regex.Match(block, @"unicode-range:\s*([^;]+);?");

                        if (familyM.Success && urlM.Success)
                        {
                            string family = familyM.Groups[1].Value;
                            string style = styleM.Success ? styleM.Groups[1].Value : "normal";
                            string weight = weightM.Success ? weightM.Groups[1].Value : "400";
                            string downloadUrl = urlM.Groups[1].Value;
                            string unicodeRange = unicodeM.Success ? unicodeM.Groups[1].Value.Trim() : "";

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
                            if (!string.IsNullOrEmpty(unicodeRange))
                            {
                                sb.AppendLine(string.Format("  unicode-range: {0};", unicodeRange));
                            }
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
            if (Regex.IsMatch(filename, @"\b(thin|100)\b", RegexOptions.IgnoreCase)) weight = "100";
            else if (Regex.IsMatch(filename, @"\b(extralight|200)\b", RegexOptions.IgnoreCase)) weight = "200";
            else if (Regex.IsMatch(filename, @"\b(light|300)\b", RegexOptions.IgnoreCase)) weight = "300";
            else if (Regex.IsMatch(filename, @"\b(medium|500)\b", RegexOptions.IgnoreCase)) weight = "500";
            else if (Regex.IsMatch(filename, @"\b(semibold|600)\b", RegexOptions.IgnoreCase)) weight = "600";
            else if (Regex.IsMatch(filename, @"\b(extrabold|heavy|800)\b", RegexOptions.IgnoreCase)) weight = "800";
            else if (Regex.IsMatch(filename, @"\b(bold|700)\b", RegexOptions.IgnoreCase)) weight = "700";
            else if (Regex.IsMatch(filename, @"\b(black|900)\b", RegexOptions.IgnoreCase)) weight = "900";

            string style = Regex.IsMatch(filename, @"\b(italic|it)\b", RegexOptions.IgnoreCase) ? "italic" : "normal";

            string cleanFamily = Regex.Replace(filename, @"[-_]?(extrabold|800|semibold|demibold|600|bold|700|extralight|200|light|300|thin|100|medium|500|heavy|900|regular|italic|normal)\b", "", RegexOptions.IgnoreCase);
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

                bool isDuplicate = false;
                if (File.Exists(fontsCssPath))
                {
                    string cur = File.ReadAllText(fontsCssPath);
                    // Kiểm tra theo cả Family name (có dấu cách) và cleanFolder (không dấu cách)
                    if (cur.IndexOf("font-family: '" + group.Family + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cur.IndexOf("font-family: \"" + group.Family + "\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cur.IndexOf("font-family: '" + cleanFolderName + "'", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cur.IndexOf("font-family: \"" + cleanFolderName + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isDuplicate = true;
                        var dr = MessageBox.Show(string.Format("Font '{0}' đã tồn tại trong fonts.css. Tiếp tục?", group.Family),
                            "Trùng font", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.No) return;
                    }
                }

                if (!isDuplicate)
                {
                    var dr = MessageBox.Show(string.Format("Bạn có chắc chắn muốn convert và cài đặt local font '{0}' vào dự án không?", group.Family), "Xác nhận cài đặt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.No) return;
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
                    // Convert → WOFF
                    byte[] ttfBytes = File.ReadAllBytes(srcFile.FilePath);
                    byte[] woffBytes = ConvertToWoff(ttfBytes);
                    File.WriteAllBytes(Path.Combine(destDir, woffName), woffBytes);

                    // WOFF2: copy WOFF làm WOFF2 placeholder (trình duyệt sẽ fallback xuống WOFF)
                    File.WriteAllBytes(Path.Combine(destDir, woff2Name), woffBytes);

                    sb.AppendLine("@font-face {");
                    sb.AppendLine(string.Format("  font-family: '{0}';", cleanFolderName));
                    sb.AppendLine(string.Format("  font-style: {0};", style));
                    sb.AppendLine(string.Format("  font-weight: {0};", weight));
                    sb.AppendLine("  font-display: swap;");
                    sb.AppendLine(string.Format("  src: url('../fonts/{0}/{1}') format('woff2'),", cleanFolderName, woff2Name));
                    sb.AppendLine(string.Format("       url('../fonts/{0}/{1}') format('woff');", cleanFolderName, woffName));
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
}
