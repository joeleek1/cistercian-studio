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
        public string DateStyle = "Symbols", DateFormat = "DMY";
        public int Size = 1, Opacity = 100, X = int.MinValue, Y = int.MinValue;
        public int Width = 0, Height = 0;
        public bool TransparentBackground = false;
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
                        case "DateStyle": if (new[] { "Symbols", "Text", "Both" }.Contains(pair[1])) settings.DateStyle = pair[1]; break;
                        case "DateFormat": if (new[] { "DMY", "MDY", "YMD", "Long" }.Contains(pair[1])) settings.DateFormat = pair[1]; break;
                        case "Width": if (int.TryParse(pair[1], out number)) settings.Width = Math.Max(240, Math.Min(1600, number)); break;
                        case "Height": if (int.TryParse(pair[1], out number)) settings.Height = Math.Max(150, Math.Min(1200, number)); break;
                        case "TransparentBackground": if (bool.TryParse(pair[1], out flag)) settings.TransparentBackground = flag; break;
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
                "X=" + X, "Y=" + Y, "Date=" + Date, "Labels=" + Labels, "Seconds=" + Seconds, "Glow=" + Glow, "Scanlines=" + Scanlines, "OnTop=" + OnTop,
                "Width=" + Width, "Height=" + Height, "DateStyle=" + DateStyle, "DateFormat=" + DateFormat, "TransparentBackground=" + TransparentBackground });
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

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0 && args[0] == "--self-test")
            {
                try { SelfTest(); WidgetTests.Run(); if (args.Length > 1) File.WriteAllText(args[1], "PASS: numerals, time zones, settings, layouts, startup toggle, desktop shortcuts and error recovery. Version 1.2.0."); return 0; }
                catch (Exception error) { if (args.Length > 1) File.WriteAllText(args[1], "FAIL: " + error.Message); return 1; }
            }
            if (args.Length > 1 && args[0] == "--render-preview")
            {
                var settings = new ClockSettings();
                if (args.Length > 2) settings.Theme = args[2];
                settings.Date = args.Contains("--date"); settings.Labels = args.Contains("--labels");
                if (args.Contains("--text-date")) settings.DateStyle = "Both";
                if (args.Contains("--long-date")) settings.DateFormat = "Long";
                if (args.Contains("--wide")) { settings.Width = 680; settings.Height = 390; }
                if (args.Contains("--compact")) { settings.Width = 260; settings.Height = 240; }
                using (var window = new ClockWindow(settings, true))
                using (var bitmap = new Bitmap(window.ClientSize.Width, window.ClientSize.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                { window.RenderClock(graphics, new DateTime(2026,11,17,19,48,36)); bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png); }
                return 0;
            }
            if (args.Contains("--enable-startup") || args.Contains("--disable-startup") || args.Contains("--desktop-shortcut"))
            {
                int reportIndex=Array.IndexOf(args,"--integration-report");
                string report=reportIndex>=0 && reportIndex+1<args.Length?args[reportIndex+1]:null;
                try
                {
                    var integration=new WindowsIntegration();
                    if(args.Contains("--enable-startup"))integration.SetStartup(true);
                    if(args.Contains("--disable-startup"))integration.SetStartup(false);
                    string desktop=args.Contains("--desktop-shortcut")?integration.CreateDesktopShortcut():"unchanged";
                    if(report!=null)File.WriteAllText(report,"Startup="+integration.StartupEnabled+Environment.NewLine+"Desktop="+desktop);
                    return 0;
                }
                catch(Exception error) { if(report!=null)File.WriteAllText(report,"ERROR: "+error.Message); return 1; }
            }
            bool preview = args.Contains("--preview");
            if(preview) { Application.Run(new ClockWindow(new ClockSettings(),true)); return 0; }
            bool firstInstance;
            using(var instance=new System.Threading.Mutex(true,"Local\\CistercianStudio.Clock."+Environment.UserName,out firstInstance))
            {
                if(!firstInstance) { ClockInstance.ShowExisting(); return 0; }
                try { Application.Run(new ClockWindow(ClockSettings.Load(ClockSettings.FilePath))); }
                finally { instance.ReleaseMutex(); }
            }
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
