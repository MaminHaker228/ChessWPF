using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ChessWPF.Models
{
    public enum ThemeType { Dark, Light, System }

    public static class AppTheme
    {
        private static readonly string SavePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChessWPF", "theme.json");

        public static ThemeType Current { get; private set; } = ThemeType.Dark;

        // Цвета для каждой темы
        public static Color BgMain { get; private set; }
        public static Color BgCard { get; private set; }
        public static Color BgElement { get; private set; }
        public static Color TextPrimary { get; private set; }
        public static Color TextSecond { get; private set; }
        public static Color Accent { get; private set; } =
            Color.FromRgb(233, 69, 96);

        public static void Apply(ThemeType theme)
        {
            var effective = theme;
            if (theme == ThemeType.System)
            {
                effective = IsSystemDark() ? ThemeType.Dark : ThemeType.Light;
            }

            Current = theme;

            if (effective == ThemeType.Dark)
            {
                BgMain = Color.FromRgb(26, 26, 46);
                BgCard = Color.FromRgb(22, 33, 62);
                BgElement = Color.FromRgb(42, 42, 74);
                TextPrimary = Color.FromRgb(234, 234, 234);
                TextSecond = Color.FromRgb(160, 160, 176);
            }
            else
            {
                BgMain = Color.FromRgb(245, 245, 250);
                BgCard = Color.FromRgb(255, 255, 255);
                BgElement = Color.FromRgb(230, 230, 240);
                TextPrimary = Color.FromRgb(30, 30, 50);
                TextSecond = Color.FromRgb(100, 100, 120);
            }

            // Обновляем ResourceDictionary
            var res = Application.Current.Resources;
            res["BrushBgMain"] = new SolidColorBrush(BgMain);
            res["BrushBgCard"] = new SolidColorBrush(BgCard);
            res["BrushBgElement"] = new SolidColorBrush(BgElement);
            res["BrushTextPrim"] = new SolidColorBrush(TextPrimary);
            res["BrushTextSec"] = new SolidColorBrush(TextSecond);
            res["BrushAccent"] = new SolidColorBrush(Accent);

            Save(theme);
        }

        public static void LoadSaved()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var t = JsonSerializer.Deserialize<ThemeType>(
                                File.ReadAllText(SavePath));
                    Apply(t);
                    return;
                }
            }
            catch { }
            Apply(ThemeType.Dark);
        }

        private static void Save(ThemeType t)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            File.WriteAllText(SavePath, JsonSerializer.Serialize(t));
        }

        private static bool IsSystemDark()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                return val is int i && i == 0;
            }
            catch { return true; }
        }
    }
}
