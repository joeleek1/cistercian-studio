using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CistercianStudio
{
    public interface IWindowsIntegration
    {
        bool StartupEnabled { get; }
        void SetStartup(bool enabled);
        string CreateDesktopShortcut();
    }

    // A per-user Startup shortcut is reversible and needs no administrator access.
    public sealed class WindowsIntegration : IWindowsIntegration
    {
        private readonly string executable, desktop, startup;
        public WindowsIntegration() : this(Application.ExecutablePath,
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.Startup)) { }
        public WindowsIntegration(string executablePath, string desktopFolder, string startupFolder)
        {
            executable = Path.GetFullPath(executablePath);
            desktop = desktopFolder; startup = startupFolder;
        }
        public string StartupPath { get { return Path.Combine(startup, "Cistercian Clock.lnk"); } }
        public bool StartupEnabled
        {
            get { return File.Exists(StartupPath) && SamePath(ReadTarget(StartupPath), executable); }
        }
        private static bool SamePath(string left, string right)
        { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
        public static string ReadTarget(string shortcutPath)
        {
            dynamic shell = null, shortcut = null;
            try
            {
                shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", true));
                shortcut = shell.CreateShortcut(shortcutPath);
                return (string)shortcut.TargetPath;
            }
            finally { Release(shortcut); Release(shell); }
        }
        private static void Release(object value)
        { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
        private void CheckExisting(string shortcutPath)
        {
            if (!File.Exists(shortcutPath)) return;
            string target = ReadTarget(shortcutPath);
            if (!string.Equals(Path.GetFileName(target), "CistercianClock.exe", StringComparison.OrdinalIgnoreCase))
                throw new IOException("A different shortcut named Cistercian Clock already exists. Rename it before trying again.");
        }
        private void WriteShortcut(string shortcutPath)
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("Keep the clock in a permanent folder before creating shortcuts.", executable);
            CheckExisting(shortcutPath);
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath));
            dynamic shell = null, shortcut = null;
            try
            {
                shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", true));
                shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = executable;
                shortcut.WorkingDirectory = Path.GetDirectoryName(executable);
                shortcut.Arguments = "";
                shortcut.IconLocation = executable + ",0";
                shortcut.Description = "Cistercian Clock - green desktop clock";
                shortcut.WindowStyle = 1;
                shortcut.Save();
            }
            finally { Release(shortcut); Release(shell); }
            if (!File.Exists(shortcutPath) || !SamePath(ReadTarget(shortcutPath), executable))
                throw new IOException("Windows did not save the clock shortcut. Please try again.");
        }
        public void SetStartup(bool enabled)
        {
            if (enabled) WriteShortcut(StartupPath);
            else if (File.Exists(StartupPath)) { CheckExisting(StartupPath); File.Delete(StartupPath); }
        }
        public string CreateDesktopShortcut()
        {
            string path = Path.Combine(desktop, "Cistercian Clock.lnk");
            WriteShortcut(path); return path;
        }
    }

    public static class ClockInstance
    {
        public static readonly int ShowMessage = RegisterWindowMessage("CistercianStudio.Clock.Show.1");
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int RegisterWindowMessage(string name);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
        public static void ShowExisting() { PostMessage(new IntPtr(0xffff), ShowMessage, IntPtr.Zero, IntPtr.Zero); }
    }
}
