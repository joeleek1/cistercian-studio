using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace CistercianStudio
{
    public static class DateDisplay
    {
        public static int[] Values(DateTime date, string format)
        { return format == "MDY" ? new[] { date.Month, date.Day, date.Year } : format == "YMD" ? new[] { date.Year, date.Month, date.Day } : new[] { date.Day, date.Month, date.Year }; }
        public static string[] Names(string format)
        { return format == "MDY" ? new[] { "MONTH", "DAY", "YEAR" } : format == "YMD" ? new[] { "YEAR", "MONTH", "DAY" } : new[] { "DAY", "MONTH", "YEAR" }; }
        public static string Text(DateTime date, string format)
        { return date.ToString(format == "MDY" ? "MM/dd/yyyy" : format == "YMD" ? "yyyy-MM-dd" : format == "Long" ? "dddd, d MMMM yyyy" : "dd/MM/yyyy", CultureInfo.GetCultureInfo("en-GB")); }
    }

    public sealed class ClockLayout
    {
        public RectangleF Time, Date, DateText;
        public static Rectangle Cog(Size size) { return new Rectangle(size.Width - 51, size.Height - 36, 29, 29); }
        public static ClockLayout Create(Size size, ClockSettings settings)
        {
            bool glyphDate = settings.Date && settings.DateStyle != "Text";
            bool textDate = settings.Date && settings.DateStyle != "Symbols";
            float available = Math.Max(55, size.Height - 55 - (textDate ? 28 : 0));
            float timeHeight = glyphDate ? available / 1.7f : available;
            var layout = new ClockLayout();
            layout.Time = new RectangleF(12, 10, size.Width - 24, timeHeight);
            layout.Date = glyphDate ? new RectangleF(12, 10 + timeHeight, size.Width - 24, available - timeHeight) : RectangleF.Empty;
            layout.DateText = textDate ? new RectangleF(12, 12 + available, size.Width - 24, 26) : RectangleF.Empty;
            return layout;
        }
        public static Size Preset(ClockSettings settings, int index)
        {
            float scale = new[] { .75f, 1f, 1.4f }[index];
            int height = 210 + (settings.Labels ? 25 : 0) + (settings.Date && settings.DateStyle != "Text" ? 120 : 0) + (settings.Date && settings.DateStyle != "Symbols" ? 30 : 0);
            return new Size(Math.Max(240, (int)((settings.Seconds ? 440 : 320) * scale)), Math.Max(150, (int)(height * scale)));
        }
    }

    public sealed class CogButton : Button
    {
        public Color Ink = Color.FromArgb(104, 255, 153);
        public CogButton()
        {
            AccessibleName = "Clock settings"; AccessibleDescription = "Change appearance, transparency, size, date and time settings";
            Cursor = Cursors.Hand; TabStop = true; FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        public static void DrawGear(Graphics graphics, Rectangle bounds, Color ink, bool focus)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = bounds.X + bounds.Width / 2f, cy = bounds.Y + bounds.Height / 2f;
            var points = new PointF[32];
            for (int i = 0; i < points.Length; i++)
            {
                double angle = i * Math.PI / 16 - Math.PI / 2;
                float radius = i % 4 == 1 || i % 4 == 2 ? 10 : 7.5f;
                points[i] = new PointF(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius);
            }
            using (var pen = new Pen(ink, 1.4f)) { graphics.DrawPolygon(pen, points); graphics.DrawEllipse(pen, cx - 3, cy - 3, 6, 6); }
            if (focus) using (var pen = new Pen(ink) { DashStyle = DashStyle.Dot }) graphics.DrawRectangle(pen, bounds.X + 1, bounds.Y + 1, bounds.Width - 3, bounds.Height - 3);
        }
        protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(BackColor); DrawGear(e.Graphics, ClientRectangle, Ink, Focused); }
    }

    public sealed class ClockWindow : Form
    {
        private readonly ClockSettings settings;
        private readonly bool preview;
        private readonly Timer timer = new Timer();
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly CogButton cog = new CogButton();
        private readonly ToolTip tips = new ToolTip();
        private SettingsWindow settingsWindow;
        private Point dragAnchor;
        private bool dragging, ready;
        private long lastSecond = -1;
        private DateTime currentTime;
        public Rectangle SettingsBounds { get { return cog.Bounds; } }
        public ClockWindow(ClockSettings initial, bool previewMode = false)
        {
            settings = initial; preview = previewMode;
            Text = "Cistercian Clock";
            AccessibleDescription = "Drag the edges to resize. Click the green cog for settings. Drag a symbol to move.";
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None; MinimumSize = new Size(240, 150); MaximumSize = new Size(1600, 1200);
            DoubleBuffered = true; KeyPreview = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Icon = MakeIcon();
            Controls.Add(cog); cog.Click += delegate { OpenSettings(); };
            tips.SetToolTip(cog, "Settings (S) - resize, transparency, date and appearance");
            menu.Items.Add("Settings...", null, delegate { OpenSettings(); });
            menu.Items.Add("Show clock", null, delegate { Show(); WindowState = FormWindowState.Normal; Activate(); });
            menu.Items.Add("Hide to tray (Esc)", null, delegate { Hide(); });
            menu.Items.Add("How to use (F1)", null, delegate { ShowHelp(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Quit (Ctrl+Q)", null, delegate { Close(); });
            ContextMenuStrip = menu;
            tray.Icon = Icon; tray.Text = "Cistercian Clock - double-click to show"; tray.ContextMenuStrip = menu; tray.Visible = !preview;
            tray.DoubleClick += delegate { Show(); WindowState = FormWindowState.Normal; Activate(); };
            ClientSize = settings.Width >= 240 && settings.Height >= 150 ? new Size(settings.Width, settings.Height) : ClockLayout.Preset(settings, settings.Size);
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 28, area.Top + 28);
            if (settings.X != int.MinValue && Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(new Rectangle(settings.X, settings.Y, Width, Height)))) Location = new Point(settings.X, settings.Y);
            ready = true; ApplySettings(false);
            timer.Interval = 200; timer.Tick += delegate { TickClock(); }; TickClock(); timer.Start();
            MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragAnchor = e.Location; dragging = true; Capture = true; } };
            MouseMove += delegate(object sender, MouseEventArgs e) { if (dragging) Location = new Point(Location.X + e.X - dragAnchor.X, Location.Y + e.Y - dragAnchor.Y); };
            MouseUp += delegate { if (dragging) { dragging = false; Capture = false; Save(); } };
            MouseDoubleClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) OpenSettings(); };
        }
        private static Icon MakeIcon()
        {
            using (var bitmap = new Bitmap(32,32)) using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(2,6,4)); graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Numerals.Draw(graphics, 2026, new RectangleF(6,3,20,26), Color.FromArgb(104,255,153), false);
                IntPtr handle = bitmap.GetHicon();
                try { using (var borrowed = Icon.FromHandle(handle)) return (Icon)borrowed.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
        private void TickClock()
        {
            var utc = DateTime.UtcNow; long second = utc.Ticks / TimeSpan.TicksPerSecond;
            if (second == lastSecond) return;
            lastSecond = second; currentTime = Numerals.TimeAt(utc, settings.Zone);
            AccessibleName = "Cistercian Clock " + currentTime.ToString("HH:mm:ss, dd MMMM yyyy", CultureInfo.InvariantCulture) + ", " + settings.Zone;
            Invalidate();
        }
        private void Save()
        {
            if (!ready || preview) return;
            settings.X = Location.X; settings.Y = Location.Y; settings.Width = ClientSize.Width; settings.Height = ClientSize.Height;
            settings.Save(ClockSettings.FilePath);
        }
        public void ApplySettings(bool save = true)
        {
            var theme = Themes.All.First(t => t.Name == settings.Theme);
            // Keep labels readable when both time and date symbol rows are enabled.
            MinimumSize = new Size(240, settings.Date && settings.DateStyle != "Text" && settings.Labels ? (settings.DateStyle == "Both" ? 210 : 180) : 150);
            TopMost = settings.OnTop; Opacity = settings.Opacity / 100.0;
            BackColor = theme.Background; TransparencyKey = settings.TransparentBackground ? theme.Background : Color.Empty;
            cog.BackColor = theme.Background; cog.Ink = theme.Light ? Color.FromArgb(33,110,61) : Color.FromArgb(104,255,153); cog.Invalidate();
            if (ready && settings.Width >= 240 && settings.Height >= 150)
            {
                var size = new Size(Math.Max(MinimumSize.Width, settings.Width), Math.Max(MinimumSize.Height, settings.Height));
                if (ClientSize != size) ClientSize = size;
            }
            lastSecond = -1; TickClock();
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(Math.Max(area.Left, Math.Min(Location.X,area.Right - Width)), Math.Max(area.Top,Math.Min(Location.Y,area.Bottom - Height)));
            if (save) Save(); Invalidate();
        }
        public void SetPreset(int index)
        {
            settings.Size = index; var size = ClockLayout.Preset(settings,index);
            settings.Width = size.Width; settings.Height = size.Height; ApplySettings();
        }
        private void OpenSettings()
        {
            if (settingsWindow == null || settingsWindow.IsDisposed) settingsWindow = new SettingsWindow(this,settings);
            if (!settingsWindow.Visible) settingsWindow.Show(this);
            settingsWindow.Activate();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (cog != null) cog.Bounds = ClockLayout.Cog(ClientSize);
            if (ready) { settings.Width = ClientSize.Width; settings.Height = ClientSize.Height; }
            Invalidate();
        }
        protected override void OnResizeEnd(EventArgs e) { base.OnResizeEnd(e); Save(); }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == ClockInstance.ShowMessage) { Show(); WindowState=FormWindowState.Normal; Activate(); return; }
            base.WndProc(ref message);
            if (message.Msg != 0x84 || (int)message.Result != 1) return; // WM_NCHITTEST / HTCLIENT
            long packed = message.LParam.ToInt64();
            var point = PointToClient(new Point(unchecked((short)(packed & 0xffff)), unchecked((short)((packed >> 16) & 0xffff))));
            if (cog != null && cog.Bounds.Contains(point)) return;
            int edge = 8; bool left=point.X<edge, right=point.X>=ClientSize.Width-edge, top=point.Y<edge, bottom=point.Y>=ClientSize.Height-edge;
            int hit = top&&left?13:top&&right?14:bottom&&left?16:bottom&&right?17:left?10:right?11:top?12:bottom?15:1;
            message.Result = (IntPtr)hit;
        }
        private void ShowHelp()
        {
            MessageBox.Show(this,"Read left to right: hours, minutes, seconds (24-hour time). An empty stem means zero.\n\nDrag any edge or corner to resize. Drag the clock to move it. With a transparent background, drag a symbol to move and use the lower-right grip to resize.\n\nClick the green cog, press S, or right-click for settings. Choose the date order and whether to show symbols, a standard date, or both. Transparency can affect the whole window or remove its background.\n\nEsc hides to the tray. Double-click the tray icon to restore. Ctrl+Q quits.\n\nThe Windows section can create a desktop shortcut and start the clock when you sign in.\n\nSettings are saved beside the app. No network connection or administrator access is required.","Cistercian Clock 1.2",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode==Keys.S || e.KeyCode==Keys.Apps) { OpenSettings(); e.Handled=true; }
            else if(e.KeyCode==Keys.F1) { ShowHelp(); e.Handled=true; }
            else if(e.KeyCode==Keys.Escape) { Hide(); e.Handled=true; }
            else if(e.Control&&e.KeyCode==Keys.Q) { Close(); e.Handled=true; }
        }
        public void RenderClock(Graphics graphics, DateTime moment)
        {
            var theme=Themes.All.First(t=>t.Name==settings.Theme); var layout=ClockLayout.Create(ClientSize,settings);
            graphics.Clear(theme.Background); graphics.SmoothingMode=SmoothingMode.AntiAlias;
            DrawRow(graphics,settings.Seconds?new[]{moment.Hour,moment.Minute,moment.Second}:new[]{moment.Hour,moment.Minute},new[]{"HOURS","MINUTES","SECONDS"},layout.Time,theme);
            if(!layout.Date.IsEmpty) DrawRow(graphics,DateDisplay.Values(moment,settings.DateFormat),DateDisplay.Names(settings.DateFormat),layout.Date,theme);
            if(!layout.DateText.IsEmpty)
            {
                using(var font=new Font("Segoe UI",Math.Max(8,Math.Min(14,ClientSize.Width/32f))))
                using(var brush=new SolidBrush(theme.Ink))
                using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter})
                    graphics.DrawString(DateDisplay.Text(moment,settings.DateFormat),font,brush,layout.DateText,format);
            }
            if(settings.Scanlines&&!theme.Light&&!settings.TransparentBackground)
                using(var scan=new Pen(Color.FromArgb(20,0,0,0))) for(int y=1;y<ClientSize.Height-38;y+=3)graphics.DrawLine(scan,0,y,ClientSize.Width,y);
            if(!settings.TransparentBackground)using(var border=new Pen(Color.FromArgb(theme.Light?35:25,theme.Ink)))graphics.DrawRectangle(border,0,0,ClientSize.Width-1,ClientSize.Height-1);
            CogButton.DrawGear(graphics,ClockLayout.Cog(ClientSize),theme.Light?Color.FromArgb(33,110,61):Color.FromArgb(104,255,153),false);
            using(var grip=new Pen(Color.FromArgb(130,theme.Ink),1.2f))for(int i=0;i<3;i++)graphics.DrawLine(grip,ClientSize.Width-5-i*4,ClientSize.Height-5,ClientSize.Width-5,ClientSize.Height-5-i*4);
        }
        private void DrawRow(Graphics graphics,int[] values,string[] names,RectangleF bounds,ClockTheme theme)
        {
            float cell=bounds.Width/values.Length, labelHeight=settings.Labels?24:0;
            float height=Math.Max(18,Math.Min(bounds.Height-labelHeight-14,cell*1.4f*.73f));
            float width=height/1.4f, y=bounds.Y+(bounds.Height-labelHeight-height)/2;
            for(int i=0;i<values.Length;i++)
            {
                float x=bounds.X+cell*i+(cell-width)/2;
                Numerals.Draw(graphics,values[i],new RectangleF(x,y,width,height),theme.Ink,settings.Glow&&!theme.Light);
                if(settings.Labels)
                    using(var font=new Font("Consolas",Math.Max(6.5f,Math.Min(9.5f,cell/13))))
                    using(var brush=new SolidBrush(Color.FromArgb(180,theme.Ink)))
                    using(var format=new StringFormat{Alignment=StringAlignment.Center})
                        graphics.DrawString(values[i].ToString(names[i]=="YEAR"?"0000":"00")+"  "+names[i],font,brush,new RectangleF(bounds.X+cell*i,y+height+5,cell,22),format);
            }
        }
        protected override void OnPaint(PaintEventArgs e) { RenderClock(e.Graphics,currentTime); }
        protected override void OnFormClosed(FormClosedEventArgs e) { Save(); base.OnFormClosed(e); }
        protected override void Dispose(bool disposing)
        {
            if(disposing) { timer.Dispose(); tray.Visible=false; tray.Dispose(); menu.Dispose(); tips.Dispose(); if(settingsWindow!=null)settingsWindow.Dispose(); if(Icon!=null)Icon.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
