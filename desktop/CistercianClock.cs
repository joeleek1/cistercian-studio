using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CistercianStudio
{
    public sealed class ClockSettings
    {
        public string Theme = "Phosphor", Zone = "Local";
        public int Size = 1, Opacity = 100, X = int.MinValue, Y = int.MinValue;
        public bool Date = false, Labels = false, Seconds = true, Glow = true, Scanlines = true, OnTop = true;
        public static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CistercianClock.settings"); } }
        public static ClockSettings Load(string path)
        {
            var settings = new ClockSettings();
            if (!File.Exists(path)) return settings;
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    var pair = line.Split(new[] { '=' }, 2);
                    if (pair.Length != 2) continue;
                    int number; bool flag;
                    switch (pair[0])
                    {
                        case "Theme": if (Themes.All.Any(t => t.Name == pair[1])) settings.Theme = pair[1]; break;
                        case "Zone": if (new[] { "Local", "London", "UTC" }.Contains(pair[1])) settings.Zone = pair[1]; break;
                        case "Size": if (int.TryParse(pair[1], out number)) settings.Size = Math.Max(0, Math.Min(2, number)); break;
                        case "Opacity": if (int.TryParse(pair[1], out number)) settings.Opacity = Math.Max(40, Math.Min(100, number)); break;
                        case "X": if (int.TryParse(pair[1], out number)) settings.X = number; break;
                        case "Y": if (int.TryParse(pair[1], out number)) settings.Y = number; break;
                        case "Date": if (bool.TryParse(pair[1], out flag)) settings.Date = flag; break;
                        case "Labels": if (bool.TryParse(pair[1], out flag)) settings.Labels = flag; break;
                        case "Seconds": if (bool.TryParse(pair[1], out flag)) settings.Seconds = flag; break;
                        case "Glow": if (bool.TryParse(pair[1], out flag)) settings.Glow = flag; break;
                        case "Scanlines": if (bool.TryParse(pair[1], out flag)) settings.Scanlines = flag; break;
                        case "OnTop": if (bool.TryParse(pair[1], out flag)) settings.OnTop = flag; break;
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return settings;
        }
        public void Save(string path)
        {
            string text = string.Join(Environment.NewLine, new[] { "Theme=" + Theme, "Zone=" + Zone, "Size=" + Size, "Opacity=" + Opacity,
                "X=" + X, "Y=" + Y, "Date=" + Date, "Labels=" + Labels, "Seconds=" + Seconds, "Glow=" + Glow, "Scanlines=" + Scanlines, "OnTop=" + OnTop });
            // Portable settings stay beside the executable; a read-only folder simply disables persistence.
            try { File.WriteAllText(path, text, Encoding.UTF8); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public sealed class ClockTheme
    {
        public string Name; public Color Ink, Background; public bool Light;
        public ClockTheme(string name, string ink, string background, bool light = false)
        { Name = name; Ink = ColorTranslator.FromHtml(ink); Background = ColorTranslator.FromHtml(background); Light = light; }
    }
    public static class Themes
    {
        public static readonly ClockTheme[] All = {
            new ClockTheme("Phosphor", "#68FF99", "#020604"),
            new ClockTheme("Amber", "#FFBA5A", "#090500"),
            new ClockTheme("Ice", "#75E8FF", "#02070B"),
            new ClockTheme("Paper", "#253E3B", "#F4F1E9", true)
        };
    }

    public static class Numerals
    {
        private static readonly int[][][] Digits = {
            new int[0][], new[] { new[] {0,0,1,0} }, new[] { new[] {0,1,1,1} }, new[] { new[] {0,0,1,1} },
            new[] { new[] {0,1,1,0} }, new[] { new[] {0,0,1,0}, new[] {0,1,1,0} }, new[] { new[] {1,0,1,1} },
            new[] { new[] {0,0,1,0}, new[] {1,0,1,1} }, new[] { new[] {0,1,1,1}, new[] {1,0,1,1} },
            new[] { new[] {0,0,1,0}, new[] {0,1,1,1}, new[] {1,0,1,1} }
        };
        public static List<float[]> Lines(int value)
        {
            if (value < 0 || value > 9999) throw new ArgumentOutOfRangeException("value");
            var lines = new List<float[]> { new float[] { 50, 10, 50, 130 } };
            int factor = 1;
            for (int place = 0; place < 4; place++, factor *= 10)
            {
                int digit = value / factor % 10, flip = place % 2 == 0 ? 1 : -1;
                foreach (var segment in Digits[digit])
                {
                    lines.Add(new[] { 50f + flip * segment[0] * 38, place >= 2 ? 130f - segment[1] * 38 : 10f + segment[1] * 38,
                        50f + flip * segment[2] * 38, place >= 2 ? 130f - segment[3] * 38 : 10f + segment[3] * 38 });
                }
            }
            return lines;
        }
        public static void Draw(Graphics graphics, int value, RectangleF rectangle, Color colour, bool glow)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(rectangle.X, rectangle.Y);
            graphics.ScaleTransform(rectangle.Width / 100f, rectangle.Height / 140f);
            var lines = Lines(value);
            if (glow)
            {
                PaintLines(graphics, lines, Color.FromArgb(12, colour), 14f);
                PaintLines(graphics, lines, Color.FromArgb(28, colour), 8f);
                PaintLines(graphics, lines, Color.FromArgb(50, colour), 5.2f);
            }
            PaintLines(graphics, lines, colour, 3.2f);
            graphics.Restore(state);
        }
        private static void PaintLines(Graphics graphics, List<float[]> lines, Color colour, float width)
        {
            using (var pen = new Pen(colour, width))
            {
                pen.StartCap = pen.EndCap = LineCap.Round;
                foreach (var line in lines) graphics.DrawLine(pen, line[0], line[1], line[2], line[3]);
            }
        }
        public static DateTime TimeAt(DateTime utc, string zone)
        {
            if (zone == "UTC") return utc;
            if (zone == "London") return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
            return utc.ToLocalTime();
        }
    }

    public sealed class ClockWindow : Form
    {
        private readonly ClockSettings settings;
        private readonly bool preview;
        private readonly Timer timer = new Timer();
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly NotifyIcon tray = new NotifyIcon();
        private Point dragAnchor;
        private bool dragging;
        private long lastSecond = -1;
        private DateTime currentTime;
        public ClockWindow(ClockSettings initial, bool previewMode = false)
        {
            settings = initial; preview = previewMode;
            Text = "Cistercian Clock";
            AccessibleName = "Cistercian desktop clock";
            AccessibleDescription = "Hours, minutes and seconds in Cistercian numerals. Right-click or press S for settings. F1 for help.";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true; KeyPreview = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Icon = MakeIcon();
            ContextMenuStrip = menu;
            menu.Opening += delegate { BuildMenu(); };
            tray.Icon = Icon; tray.Text = "Cistercian Clock - right-click for settings"; tray.ContextMenuStrip = menu; tray.Visible = !preview;
            tray.DoubleClick += delegate { Show(); WindowState = FormWindowState.Normal; Activate(); };
            ApplySettings(false);
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 28, area.Top + 28);
            if (settings.X != int.MinValue && Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(new Rectangle(settings.X, settings.Y, Width, Height)))) Location = new Point(settings.X, settings.Y);
            timer.Interval = 100;
            timer.Tick += delegate { TickClock(); };
            TickClock(); timer.Start();
            MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragAnchor = e.Location; dragging = true; Capture = true; } };
            MouseMove += delegate(object sender, MouseEventArgs e) { if (dragging) Location = new Point(Location.X + e.X - dragAnchor.X, Location.Y + e.Y - dragAnchor.Y); };
            MouseUp += delegate { if (dragging) { dragging = false; Capture = false; Save(); } };
            MouseDoubleClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) menu.Show(this, e.Location); };
        }
        private static Icon MakeIcon()
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(2,6,4));
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Numerals.Draw(graphics, 2026, new RectangleF(6, 3, 20, 26), Color.FromArgb(104,255,153), false);
                IntPtr handle = bitmap.GetHicon();
                try { using (var borrowed = Icon.FromHandle(handle)) return (Icon)borrowed.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
        private void TickClock()
        {
            var now = DateTime.UtcNow;
            long second = now.Ticks / TimeSpan.TicksPerSecond;
            if (second == lastSecond) return;
            lastSecond = second;
            currentTime = Numerals.TimeAt(now, settings.Zone);
            AccessibleName = "Cistercian Clock " + currentTime.ToString("HH:mm:ss, dd MMMM yyyy", CultureInfo.InvariantCulture) + ", " + settings.Zone;
            Invalidate();
        }
        private void Save()
        {
            if (preview) return;
            settings.X = Location.X; settings.Y = Location.Y; settings.Save(ClockSettings.FilePath);
        }
        private void ApplySettings(bool save = true)
        {
            float scale = new[] { .75f, 1f, 1.4f }[settings.Size];
            int width = settings.Seconds ? 420 : 300;
            int height = 178 + (settings.Labels ? 35 : 0) + (settings.Date ? 125 + (settings.Labels ? 25 : 0) : 0);
            ClientSize = new Size((int)(width * scale), (int)(height * scale));
            TopMost = settings.OnTop;
            Opacity = settings.Opacity / 100.0;
            BackColor = Themes.All.First(t => t.Name == settings.Theme).Background;
            lastSecond = -1; TickClock();
            // Keep the widget reachable after a size change or monitor removal.
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(Math.Max(area.Left, Math.Min(Location.X, area.Right - Width)), Math.Max(area.Top, Math.Min(Location.Y, area.Bottom - Height)));
            if (save) Save();
            Invalidate();
        }
        private ToolStripMenuItem AddChoice(ToolStripMenuItem parent, string label, bool selected, Action action)
        {
            var item = new ToolStripMenuItem(label) { Checked = selected };
            item.Click += delegate { action(); ApplySettings(); };
            parent.DropDownItems.Add(item); return item;
        }
        private void AddToggle(string label, bool selected, Action action)
        {
            var item = new ToolStripMenuItem(label) { Checked = selected };
            item.Click += delegate { action(); ApplySettings(); };
            menu.Items.Add(item);
        }
        private void BuildMenu()
        {
            // Dispose prior items, including submenu handles, before rebuilding.
            foreach (ToolStripItem old in menu.Items.Cast<ToolStripItem>().ToArray()) old.Dispose();
            menu.Items.Clear();
            var heading = new ToolStripMenuItem("Cistercian Clock") { Enabled = false };
            menu.Items.Add(heading); menu.Items.Add(new ToolStripSeparator());
            var theme = new ToolStripMenuItem("Theme"); menu.Items.Add(theme);
            foreach (var candidate in Themes.All) { var chosen = candidate; AddChoice(theme, candidate.Name + (candidate.Name == "Phosphor" ? " - green on black" : ""), settings.Theme == candidate.Name, () => settings.Theme = chosen.Name); }
            var size = new ToolStripMenuItem("Size"); menu.Items.Add(size);
            for (int i = 0; i < 3; i++) { int index = i; AddChoice(size, new[] { "Small", "Medium", "Large" }[i], settings.Size == i, () => settings.Size = index); }
            var opacity = new ToolStripMenuItem("Opacity"); menu.Items.Add(opacity);
            foreach (int level in new[] { 100, 85, 70, 55, 40 }) { int chosen = level; AddChoice(opacity, level + "%", settings.Opacity == level, () => settings.Opacity = chosen); }
            var zone = new ToolStripMenuItem("Time zone"); menu.Items.Add(zone);
            foreach (string value in new[] { "Local", "London", "UTC" }) { string chosen = value; AddChoice(zone, value == "Local" ? "This device" : value, settings.Zone == value, () => settings.Zone = chosen); }
            menu.Items.Add(new ToolStripSeparator());
            AddToggle("Show seconds", settings.Seconds, () => settings.Seconds = !settings.Seconds);
            AddToggle("Show date (day / month / year)", settings.Date, () => settings.Date = !settings.Date);
            AddToggle("Show decimal labels", settings.Labels, () => settings.Labels = !settings.Labels);
            AddToggle("Phosphor glow", settings.Glow, () => settings.Glow = !settings.Glow);
            AddToggle("CRT scanlines", settings.Scanlines, () => settings.Scanlines = !settings.Scanlines);
            AddToggle("Always on top", settings.OnTop, () => settings.OnTop = !settings.OnTop);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Show clock", null, delegate { Show(); Activate(); });
            menu.Items.Add("Hide to tray (Esc)", null, delegate { Hide(); });
            menu.Items.Add("How to use (F1)", null, delegate { ShowHelp(); });
            menu.Items.Add("Quit (Ctrl+Q)", null, delegate { Close(); });
        }
        private void ShowHelp()
        {
            MessageBox.Show(this, "Read left to right: hours, minutes, seconds (24-hour time).\n\nIf the date is enabled, the second row is day, month, year. An empty stem means zero.\n\nDrag anywhere to move. Right-click, double-click or press S for settings.\n\nThemes: Phosphor green, Amber, Ice and Paper. You can change the size, opacity, date, decimal labels, glow, scanlines and always-on-top setting.\n\nEsc hides to the tray. Double-click the tray icon to restore. Ctrl+Q quits.\n\nSettings are saved beside the app. No network connection or installation is required.", "Cistercian Clock", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Apps) { menu.Show(this, new Point(20, 20)); e.Handled = true; }
            else if (e.KeyCode == Keys.F1) { ShowHelp(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { Hide(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Q) { Close(); e.Handled = true; }
        }
        public void RenderClock(Graphics graphics, DateTime moment)
        {
            var theme = Themes.All.First(t => t.Name == settings.Theme);
            graphics.Clear(theme.Background);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = new[] { .75f, 1f, 1.4f }[settings.Size];
            var state = graphics.Save();
            graphics.ScaleTransform(scale, scale);
            float logicalWidth = ClientSize.Width / scale;
            DrawRow(graphics, settings.Seconds ? new[] { moment.Hour, moment.Minute, moment.Second } : new[] { moment.Hour, moment.Minute }, new[] { "HOURS", "MINUTES", "SECONDS" }, 22, 134, logicalWidth, theme);
            if (settings.Date) DrawRow(graphics, new[] { moment.Day, moment.Month, moment.Year }, new[] { "DAY", "MONTH", "YEAR" }, 188 + (settings.Labels ? 35 : 0), 90, logicalWidth, theme);
            graphics.Restore(state);
            if (settings.Scanlines && !theme.Light)
            {
                using (var scan = new Pen(Color.FromArgb(20, 0, 0, 0)))
                    for (int y = 1; y < Height; y += 3) graphics.DrawLine(scan, 0, y, Width, y);
            }
            using (var border = new Pen(Color.FromArgb(theme.Light ? 35 : 25, theme.Ink))) graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
        private void DrawRow(Graphics graphics, int[] values, string[] names, float y, float height, float width, ClockTheme theme)
        {
            float cell = (width - 32) / values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                float glyphWidth = height / 1.4f;
                float x = 16 + cell * i + (cell - glyphWidth) / 2;
                Numerals.Draw(graphics, values[i], new RectangleF(x, y, glyphWidth, height), theme.Ink, settings.Glow && !theme.Light);
                if (settings.Labels)
                {
                    using (var font = new Font("Consolas", 8.5f))
                    using (var brush = new SolidBrush(Color.FromArgb(theme.Light ? 180 : 145, theme.Ink)))
                    using (var format = new StringFormat { Alignment = StringAlignment.Center })
                        graphics.DrawString(values[i].ToString(i == 2 && names[2] == "YEAR" ? "0000" : "00") + "  " + names[i], font, brush, new RectangleF(16 + cell * i, y + height + 8, cell, 22), format);
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e) { RenderClock(e.Graphics, currentTime); }
        protected override void OnFormClosed(FormClosedEventArgs e)
        { Save(); timer.Stop(); timer.Dispose(); tray.Visible = false; tray.Dispose(); menu.Dispose(); Icon.Dispose(); base.OnFormClosed(e); }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0 && args[0] == "--self-test")
            {
                try { SelfTest(); if (args.Length > 1) File.WriteAllText(args[1], "PASS: 10,000 unique numeral geometries, reference fixtures, time-zone transitions, and settings round trip."); return 0; }
                catch (Exception error) { if (args.Length > 1) File.WriteAllText(args[1], "FAIL: " + error.Message); return 1; }
            }
            if (args.Length > 1 && args[0] == "--render-preview")
            {
                var settings = new ClockSettings();
                if (args.Length > 2) settings.Theme = args[2];
                settings.Date = args.Contains("--date"); settings.Labels = args.Contains("--labels");
                using (var window = new ClockWindow(settings, true))
                using (var bitmap = new Bitmap(window.ClientSize.Width, window.ClientSize.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                { window.RenderClock(graphics, new DateTime(2026,11,17,19,48,36)); bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png); }
                return 0;
            }
            bool preview = args.Contains("--preview");
            Application.Run(new ClockWindow(preview ? new ClockSettings() : ClockSettings.Load(ClockSettings.FilePath), preview));
            return 0;
        }
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static string Signature(int value) { return string.Join(";", Numerals.Lines(value).Select(line => string.Join(",", line.Select(n => n.ToString(CultureInfo.InvariantCulture))))); }
        private static void SelfTest()
        {
            var shapes = new HashSet<string>();
            for (int i = 0; i < 10000; i++) shapes.Add(Signature(i));
            Check(shapes.Count == 10000, "Distinct glyphs");
            Check(Signature(6) == "50,10,50,130;88,10,88,48", "Detached six");
            Check(Signature(1000) == "50,10,50,130;50,130,12,130", "Thousands orientation");
            Check(Signature(1993) == "50,10,50,130;50,10,88,48;50,10,12,10;50,48,12,48;12,10,12,48;50,130,88,130;50,92,88,92;88,130,88,92;50,130,12,130", "1993 reference chart");
            Check(Numerals.TimeAt(new DateTime(2026,7,1,23,30,0,DateTimeKind.Utc), "London") == new DateTime(2026,7,2,0,30,0), "BST date rollover");
            Check(Numerals.TimeAt(new DateTime(2026,11,17,12,0,0,DateTimeKind.Utc), "London").Hour == 12, "Winter GMT");
            string temp = Path.Combine(Path.GetTempPath(), "cistercian-test-" + Guid.NewGuid().ToString("N") + ".settings");
            try { var settings = new ClockSettings { Theme="Amber", Date=true, Labels=true, Size=2, Opacity=70, Zone="UTC" }; settings.Save(temp); var loaded=ClockSettings.Load(temp); Check(loaded.Theme=="Amber"&&loaded.Date&&loaded.Labels&&loaded.Size==2&&loaded.Opacity==70&&loaded.Zone=="UTC", "Settings round trip"); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
