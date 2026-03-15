using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChessWPF.Models
{
    public class GameRecord
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Result { get; set; } = ""; // "Победа", "Поражение", "Ничья"
        public string EndReason { get; set; } = ""; // "Мат", "Пат", "Время"
        public int MovesCount { get; set; } = 0;
        public string Mode { get; set; } = ""; // "ИИ", "LAN"
        public string MoveHistory { get; set; } = "";
        public List<MoveAnalysis> Analysis { get; set; } = new();
    }

    public class MoveAnalysis
    {
        public int MoveNumber { get; set; }
        public string MoveText { get; set; } = "";
        public string Quality { get; set; } = ""; // "Отлично","Хорошо","Неточность","Ошибка","Зевок"
        public int ScoreBefore { get; set; }
        public int ScoreAfter { get; set; }
        public string BestMove { get; set; } = "";
    }

    public static class GameHistory
    {
        private static readonly string SavePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChessWPF", "history.json");

        private static List<GameRecord> _records;

        public static List<GameRecord> Records
        {
            get
            {
                if (_records == null) Load();
                return _records;
            }
        }

        public static void AddRecord(GameRecord record)
        {
            if (_records == null) Load();
            _records.Insert(0, record);
            if (_records.Count > 10) _records.RemoveAt(_records.Count - 1);
            Save();
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    _records = JsonSerializer.Deserialize<List<GameRecord>>(
                                   File.ReadAllText(SavePath))
                               ?? new List<GameRecord>();
                    return;
                }
            }
            catch { }
            _records = new List<GameRecord>();
        }

        private static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            File.WriteAllText(SavePath,
                JsonSerializer.Serialize(_records,
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
