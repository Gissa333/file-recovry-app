using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepVideoRecovery
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ============ زر دائري احترافي ============
    public class RoundButton : Button
    {
        public int CornerRadius { get; set; } = 10;
        public Color NormalColor { get; set; } = Color.FromArgb(124, 58, 237);
        public Color HoverColor { get; set; } = Color.FromArgb(139, 92, 246);
        public Color PressedColor { get; set; } = Color.FromArgb(109, 40, 217);
        private bool _hovering = false;
        private bool _pressing = false;

        public RoundButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; _pressing = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressing = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressing = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg;
            if (!Enabled) bg = Color.FromArgb(70, 70, 90);
            else if (_pressing) bg = PressedColor;
            else if (_hovering) bg = HoverColor;
            else bg = NormalColor;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GetRoundPath(rect, CornerRadius))
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath GetRoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ============ لوحة دائرية ============
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 12;
        public Color BorderColor { get; set; } = Color.FromArgb(60, 60, 85);

        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GetRoundPath(rect, CornerRadius))
            {
                using (var brush = new SolidBrush(BackColor))
                    e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }

        private GraphicsPath GetRoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ============ الواجهة الرئيسية ============
    public class MainForm : Form
    {
        private static readonly Color BgDark = Color.FromArgb(20, 20, 32);
        private static readonly Color CardBg = Color.FromArgb(32, 32, 50);
        private static readonly Color CardBorder = Color.FromArgb(60, 60, 90);
        private static readonly Color Accent = Color.FromArgb(124, 58, 237);
        private static readonly Color TextMain = Color.FromArgb(235, 235, 245);
        private static readonly Color TextDim = Color.FromArgb(150, 155, 175);
        private static readonly Color Success = Color.FromArgb(34, 197, 94);
        private static readonly Color Warning = Color.FromArgb(245, 158, 11);
        private static readonly Color Error = Color.FromArgb(239, 68, 68);
        private static readonly Color Info = Color.FromArgb(96, 165, 250);

        private const string DeveloperName = "وقاص عباس جاويش التوم";
        private const string AppVersion = "v1.0";

        private ComboBox cmbDrives;
        private TextBox txtOutput;
        private RoundButton btnBrowseOutput;
        private RoundButton btnStart;
        private RoundButton btnStop;
        private ProgressBar progressBar;
        private Label lblPercent;
        private RichTextBox txtLog;
        private Label lblStatus;
        private CancellationTokenSource cts;

        private const long MaxFileSize = 2L * 1024 * 1024 * 1024;
        private const int BufferSize = 1024 * 1024;

        public MainForm()
        {
            SetupForm();
            BuildUI();
            LoadDrives();
        }

        private void SetupForm()
        {
            Text = "🎬 Deep Video Recovery  |  by " + DeveloperName;
            Width = 1000;
            Height = 740;
            BackColor = BgDark;
            ForeColor = TextMain;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
        }

        private void BuildUI()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(45, 25, 90)
            };
            header.Paint += (s, e) =>
            {
                using (var lg = new LinearGradientBrush(header.ClientRectangle,
                    Color.FromArgb(60, 30, 120), Color.FromArgb(124, 58, 237), 0F))
                    e.Graphics.FillRectangle(lg, header.ClientRectangle);
            };

            var lblTitle = new Label
            {
                Text = "🎬  Deep Video Recovery",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(30, 22),
                BackColor = Color.Transparent
            };

            var lblSubtitle = new Label
            {
                Text = "استعادة الفيديوهات المحذوفة بالبحث العميق في القرص",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(220, 210, 255),
                AutoSize = true,
                Location = new Point(35, 65),
                BackColor = Color.Transparent
            };

            var lblDev = new Label
            {
                Text = "Developed by  " + DeveloperName + "  •  " + AppVersion,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 130),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            lblDev.Location = new Point(header.Width - 280, 80);
            header.Resize += (s, e) => lblDev.Location = new Point(header.Width - lblDev.Width - 30, 80);

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);
            header.Controls.Add(lblDev);

            var card1 = new RoundedPanel
            {
                BackColor = CardBg,
                BorderColor = CardBorder,
                CornerRadius = 14,
                Location = new Point(25, 130),
                Size = new Size(950, 165),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblDrive = new Label
            {
                Text = "💾  اختر القرص المراد فحصه:",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = TextMain,
                Location = new Point(25, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            cmbDrives = new ComboBox
            {
                Location = new Point(25, 48),
                Width = 890,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(45, 45, 65),
                ForeColor = TextMain,
                FlatStyle = FlatStyle.Flat
            };

            var lblOutput = new Label
            {
                Text = "📁  مجلد حفظ الفيديوهات المستعادة (يفضّل قرص مختلف):",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = TextMain,
                Location = new Point(25, 90),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            txtOutput = new TextBox
            {
                Location = new Point(25, 120),
                Width = 750,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(45, 45, 65),
                ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnBrowseOutput = new RoundButton
            {
                Text = "استعراض…",
                Location = new Point(790, 118),
                Size = new Size(125, 32),
                NormalColor = Color.FromArgb(55, 55, 80),
                HoverColor = Color.FromArgb(75, 75, 105),
                CornerRadius = 8,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnBrowseOutput.Click += BtnBrowse_Click;

            card1.Controls.Add(lblDrive);
            card1.Controls.Add(cmbDrives);
            card1.Controls.Add(lblOutput);
            card1.Controls.Add(txtOutput);
            card1.Controls.Add(btnBrowseOutput);

            var card2 = new RoundedPanel
            {
                BackColor = CardBg,
                BorderColor = CardBorder,
                CornerRadius = 14,
                Location = new Point(25, 310),
                Size = new Size(950, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnStart = new RoundButton
            {
                Text = "▶  بدء البحث العميق",
                Location = new Point(25, 25),
                Size = new Size(260, 50),
                NormalColor = Accent,
                HoverColor = Color.FromArgb(139, 92, 246),
                CornerRadius = 12,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new RoundButton
            {
                Text = "⏹  إيقاف",
                Location = new Point(300, 25),
                Size = new Size(140, 50),
                NormalColor = Error,
                HoverColor = Color.FromArgb(220, 38, 38),
                CornerRadius = 12,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Enabled = false
            };
            btnStop.Click += (s, e) => { cts?.Cancel(); AddLog("⏹ تم إرسال أمر الإيقاف…", Warning); };

            lblPercent = new Label
            {
                Text = "0%",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Accent,
                Location = new Point(840, 30),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card2.Resize += (s, e) => lblPercent.Location = new Point(card2.Width - 100, 30);

            progressBar = new ProgressBar
            {
                Location = new Point(25, 90),
                Width = 900,
                Height = 28,
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card2.Controls.Add(btnStart);
            card2.Controls.Add(btnStop);
            card2.Controls.Add(lblPercent);
            card2.Controls.Add(progressBar);

            var lblLog = new Label
            {
                Text = "📋  سجل العمليات:",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = TextMain,
                Location = new Point(30, 465),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            txtLog = new RichTextBox
            {
                Location = new Point(25, 495),
                Size = new Size(950, 165),
                BackColor = Color.FromArgb(15, 15, 25),
                ForeColor = TextMain,
                Font = new Font("Consolas", 9.5F),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                RightToLeft = RightToLeft.No,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            lblStatus = new Label
            {
                Text = "● جاهز للعمل",
                ForeColor = Success,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                BackColor = Color.FromArgb(15, 15, 25)
            };

            Controls.Add(header);
            Controls.Add(card1);
            Controls.Add(card2);
            Controls.Add(lblLog);
            Controls.Add(txtLog);
            Controls.Add(lblStatus);
        }

        private void LoadDrives()
        {
            cmbDrives.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady && (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable))
                    {
                        long totalGB = drive.TotalSize / (1024L * 1024 * 1024);
                        cmbDrives.Items.Add($"{drive.Name}   {drive.VolumeLabel}   ({drive.DriveFormat}, {totalGB} GB)");
                    }
                }
                catch { }
            }
            if (cmbDrives.Items.Count > 0) cmbDrives.SelectedIndex = 0;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "اختر مجلد حفظ الفيديوهات المستعادة";
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtOutput.Text = fbd.SelectedPath;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (cmbDrives.SelectedItem == null) { MessageBox.Show("اختر القرص أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtOutput.Text) || !Directory.Exists(txtOutput.Text))
            { MessageBox.Show("اختر مجلد حفظ صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string selected = cmbDrives.SelectedItem.ToString();
            char driveLetter = selected[0];

            if (txtOutput.Text.StartsWith(driveLetter.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var res = MessageBox.Show(
                    "⚠️ تحذير: مجلد الحفظ على نفس القرص المراد فحصه!\n\nسيؤدي هذا إلى الكتابة فوق الملفات المفقودة وإتلافها نهائيًا.\n\nهل أنت متأكد؟",
                    "تحذير خطير", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res != DialogResult.Yes) return;
            }

            cts = new CancellationTokenSource();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            txtLog.Clear();
            progressBar.Value = 0;
            lblPercent.Text = "0%";
            SetStatus("● جارٍ الفحص…", Warning);

            string drivePath = $@"\\.\{driveLetter}:";
            string outputDir = txtOutput.Text;
            var token = cts.Token;

            Task.Run(() => ScanDrive(drivePath, outputDir, token));
        }

        private void ScanDrive(string drivePath, string outputDir, CancellationToken token)
        {
            AddLog("═══════════════════════════════════════════════════════", Info);
            AddLog("  🎬 Deep Video Recovery  |  Developed by " + DeveloperName, Info);
            AddLog("═══════════════════════════════════════════════════════", Info);
            AddLog($"💾 القرص:  {drivePath}", TextMain);
            AddLog($"📁 الحفظ:  {outputDir}", TextMain);
            AddLog("───────────────────────────────────────────────────────", CardBorder);

            try
            {
                using (var stream = new FileStream(drivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
                {
                    long totalSize = stream.Length;
                    AddLog($"📊 حجم القرص: {totalSize / (1024L * 1024 * 1024)} GB", Info);
                    AddLog("🔍 جارٍ البحث عن بصمات الفيديو… قد يستغرق وقتًا.", Warning);
                    AddLog("───────────────────────────────────────────────────────", CardBorder);

                    byte[] buffer = new byte[BufferSize];
                    long position = 0;
                    long lastExtractedEnd = -1;
                    int foundCount = 0;

                    while (position < totalSize)
                    {
                        if (token.IsCancellationRequested) { AddLog("⏹ تم الإيقاف بواسطة المستخدم.", Warning); break; }

                        int bytesRead = stream.Read(buffer, 0, BufferSize);
                        if (bytesRead <= 0) break;

                        for (int i = 0; i < bytesRead - 16; i++)
                        {
                            long absoluteOffset = position + i;
                            if (absoluteOffset <= lastExtractedEnd) continue;

                            string ext = DetectSignature(buffer, i, bytesRead);
                            if (ext == null) continue;

                            foundCount++;
                            AddLog($"✅ [{foundCount}] بصمة {ext.ToUpper()} عند {absoluteOffset:N0}", Success);

                            string fileName = $"recovered_{foundCount:D4}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
                            string filePath = Path.Combine(outputDir, fileName);

                            long extracted = ExtractFile(stream, absoluteOffset, filePath, token);
                            if (extracted > 0)
                            {
                                lastExtractedEnd = absoluteOffset + extracted;
                                AddLog($"    💾 تم الحفظ ({extracted / (1024 * 1024)} MB): {fileName}", Success);
                            }
                            else
                            {
                                foundCount--;
                            }
                        }

                        position += bytesRead;
                        int percent = (int)((position * 100) / totalSize);
                        UpdateProgress(percent, foundCount);
                    }

                    AddLog("───────────────────────────────────────────────────────", CardBorder);
                    AddLog($"🎉 انتهى الفحص. إجمالي الفيديوهات المستعادة: {foundCount}", Success);
                    AddLog($"📂 مكان الملفات: {outputDir}", Info);
                    SetStatus($"● اكتمل — تم استعادة {foundCount} ملف", Success);
                }
            }
            catch (UnauthorizedAccessException)
            {
                AddLog("❌ خطأ: يجب تشغيل البرنامج كمسؤول (Run as Administrator).", Error);
                SetStatus("● فشل — تحتاج صلاحيات مسؤول", Error);
            }
            catch (Exception ex)
            {
                AddLog("❌ خطأ: " + ex.Message, Error);
                SetStatus("● فشل — حدث خطأ", Error);
            }
            finally
            {
                Invoke((Action)(() => { btnStart.Enabled = true; btnStop.Enabled = false; }));
            }
        }

        private string DetectSignature(byte[] buf, int i, int len)
        {
            // MP4 / MOV / 3GP / M4V
            if (i >= 4 && i + 8 < len &&
                buf[i] == 0x66 && buf[i + 1] == 0x74 && buf[i + 2] == 0x79 && buf[i + 3] == 0x70)
            {
                string brand = Encoding.ASCII.GetString(buf, i + 4, 4);
                if (brand.StartsWith("qt")) return "mov";
                if (brand.StartsWith("3g")) return "3gp";
                if (brand.StartsWith("M4V") || brand.StartsWith("M4A")) return "m4v";
                return "mp4";
            }

            // AVI
            if (i + 12 < len &&
                buf[i] == 0x52 && buf[i + 1] == 0x49 && buf[i + 2] == 0x46 && buf[i + 3] == 0x46 &&
                buf[i + 8] == 0x41 && buf[i + 9] == 0x56 && buf[i + 10] == 0x49 && buf[i + 11] == 0x20)
                return "avi";

            // MKV / WebM
            if (i + 4 < len &&
                buf[i] == 0x1A && buf[i + 1] == 0x45 && buf[i + 2] == 0xDF && buf[i + 3] == 0xA3)
                return "mkv";

            // WMV / ASF
            if (i + 4 < len &&
                buf[i] == 0x30 && buf[i + 1] == 0x26 && buf[i + 2] == 0xB2 && buf[i + 3] == 0x75)
                return "wmv";

            // FLV
            if (i + 4 < len &&
                buf[i] == 0x46 && buf[i + 1] == 0x4C && buf[i + 2] == 0x56 && buf[i + 3] == 0x01)
                return "flv";

            return null;
        }

        private long ExtractFile(FileStream stream, long offset, string outputPath, CancellationToken token)
        {
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);

                using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
                {
                    byte[] copyBuf = new byte[BufferSize];
                    long totalWritten = 0;
                    int zeroRunCount = 0;

                    while (totalWritten < MaxFileSize)
                    {
                        if (token.IsCancellationRequested) break;

                        int read = stream.Read(copyBuf, 0, BufferSize);
                        if (read <= 0) break;

                        bool stop = false;
                        for (int j = 0; j < read; j++)
                        {
                            if (copyBuf[j] == 0x00) zeroRunCount++;
                            else zeroRunCount = 0;

                            if (zeroRunCount >= 1024 * 1024)
                            {
                                output.Write(copyBuf, 0, j - (1024 * 1024) + 1 > 0 ? j - (1024 * 1024) + 1 : 0);
                                totalWritten += j;
                                stop = true;
                                break;
                            }
                        }
                        if (stop) break;

                        output.Write(copyBuf, 0, read);
                        totalWritten += read;
                    }

                    return totalWritten;
                }
            }
            catch
            {
                return 0;
            }
        }

        private void AddLog(string message, Color color)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke((Action)(() => AddLog(message, color)));
                return;
            }
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        private void UpdateProgress(int percent, int foundCount)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke((Action)(() => UpdateProgress(percent, foundCount)));
                return;
            }
            progressBar.Value = Math.Min(100, Math.Max(0, percent));
            lblPercent.Text = percent + "%";
            SetStatus($"● فحص {percent}%  —  تم استعادة: {foundCount} فيديو", Warning);
        }

        private void SetStatus(string text, Color color)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke((Action)(() => SetStatus(text, color)));
                return;
            }
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }
    }
}