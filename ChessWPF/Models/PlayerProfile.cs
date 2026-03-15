using System;
using System.IO;
using System.Text.Json;

namespace ChessWPF.Models
{
    public class PlayerProfile
    {
        public string Nickname { get; set; } = "Игрок";
        public int GamesPlayed { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Draws { get; set; } = 0;
        public int Checkmates { get; set; } = 0;
        public int Stalemates { get; set; } = 0;
        public int BestStreak { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public string BoardSkin { get; set; } = "Классика";
        public string PieceSkin { get; set; } = "Unicode";
        public string AvatarPath { get; set; } = ""; // путь к аватарке
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public int TotalMovesPlayed { get; set; } = 0;

        // ── singleton ─────────────────────────────────────────────────────
        private static PlayerProfile _instance;
        private static readonly string SavePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChessWPF", "profile.json");

        public static PlayerProfile Instance
        {
            get { if (_instance == null) _instance = Load(); return _instance; }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            File.WriteAllText(SavePath,
                JsonSerializer.Serialize(this,
                    new JsonSerializerOptions { WriteIndented = true }));
        }

        private static PlayerProfile Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    return JsonSerializer.Deserialize<PlayerProfile>(
                               File.ReadAllText(SavePath))
                           ?? new PlayerProfile();
            }
            catch { }
            return new PlayerProfile();
        }

        public double WinRate =>
            GamesPlayed > 0
            ? Math.Round(Wins * 100.0 / GamesPlayed, 1) : 0;

        public string Rank => GamesPlayed switch
        {
            0 => "Новичок",
            < 5 => "Любитель",
            < 15 => "Боец",
            < 30 => "Тактик",
            < 50 => "Стратег",
            < 100 => "Мастер",
            _ => "Гроссмейстер"
        };

        public string RankIcon => GamesPlayed switch
        {
            0 => "⚪",
            < 5 => "🟢",
            < 15 => "🔵",
            < 30 => "🟡",
            < 50 => "🟠",
            < 100 => "🔴",
            _ => "🏆"
        };

        public void RecordWin(bool wasCheckmate)
        {
            GamesPlayed++; Wins++; CurrentStreak++;
            if (CurrentStreak > BestStreak) BestStreak = CurrentStreak;
            if (wasCheckmate) Checkmates++;
            Save();
        }

        public void RecordLoss()
        {
            GamesPlayed++; Losses++; CurrentStreak = 0; Save();
        }

        public void RecordDraw(bool wasStalemate)
        {
            GamesPlayed++; Draws++; CurrentStreak = 0;
            if (wasStalemate) Stalemates++;
            Save();
        }

        public void AddMoves(int count)
        {
            TotalMovesPlayed += count; Save();
        }

        // ── скины доски ───────────────────────────────────────────────────
        public static readonly (string Name, string Light, string Dark)[] BoardSkins =
        {
            ("Классика",   "#EED5B7", "#8B4513"),
            ("Зелёная",    "#EEEED2", "#769656"),
            ("Синяя",      "#DEE3E6", "#8CA2AD"),
            ("Розовая",    "#FFD6E0", "#C2678D"),
            ("Фиолетовая", "#E8D5FF", "#7B4FBE"),
            ("Серая",      "#D0D0D0", "#707070"),
            ("Ночная",     "#3A3A4A", "#1A1A2E"),
            ("Золотая",    "#FFF3C4", "#C8A400"),
        };

        public (string Light, string Dark) GetBoardColors()
        {
            foreach (var s in BoardSkins)
                if (s.Name == BoardSkin) return (s.Light, s.Dark);
            return ("#EED5B7", "#8B4513");
        }
    }
}
