using System.Collections.Generic;

namespace ChessWPF.Models
{
    public class Puzzle
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Difficulty { get; set; } = ""; // Лёгкая / Средняя / Сложная
        public string FEN { get; set; } = ""; // позиция
        public string Solution { get; set; } = ""; // "e2e4" — ход решения
        public string Hint { get; set; } = "";
    }

    public static class PuzzleData
    {
        public static readonly List<Puzzle> All = new()
        {
            new Puzzle
            {
                Title       = "Мат в 1 ход #1",
                Description = "Белые начинают и ставят мат",
                Difficulty  = "Лёгкая",
                FEN         = "6k1/5ppp/8/8/8/8/5PPP/4R1K1 w - - 0 1",
                Solution    = "e1e8",
                Hint        = "Ладья на последней линии"
            },
            new Puzzle
            {
                Title       = "Мат в 1 ход #2",
                Description = "Белые начинают и ставят мат",
                Difficulty  = "Лёгкая",
                FEN         = "r1bqkb1r/pppp1Qpp/2n2n2/4p3/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 0 1",
                Solution    = "f7e8",
                Hint        = "Ферзь атакует короля"
            },
            new Puzzle
            {
                Title       = "Вилка конём",
                Description = "Найди ход который атакует короля и ферзя",
                Difficulty  = "Средняя",
                FEN         = "r1bqkb1r/pppp1ppp/2n2n2/4p3/4P3/2N2N2/PPPP1PPP/R1BQKB1R w KQkq - 0 1",
                Solution    = "f3e5",
                Hint        = "Конь бьёт пешку с вилкой"
            },
            new Puzzle
            {
                Title       = "Мат Дурака",
                Description = "Самый быстрый мат в шахматах",
                Difficulty  = "Лёгкая",
                FEN         = "rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 1",
                Solution    = "d8h4",
                Hint        = "Ферзь выходит на диагональ"
            },
            new Puzzle
            {
                Title       = "Спёртый мат",
                Description = "Конь ставит мат королю окружённому своими фигурами",
                Difficulty  = "Средняя",
                FEN         = "6rk/6pp/7N/8/8/8/8/6K1 w - - 0 1",
                Solution    = "h6f7",
                Hint        = "Конь прыгает — куда?"
            },
            new Puzzle
            {
                Title       = "Двойной шах",
                Description = "Найди ход с двойным шахом",
                Difficulty  = "Сложная",
                FEN         = "r1bqk2r/ppp2ppp/2np1n2/2b1p3/2B1P3/2NP1N2/PPP2PPP/R1BQK2R w KQkq - 0 1",
                Solution    = "c4f7",
                Hint        = "Слон бьёт с шахом и открывает линию"
            },
            new Puzzle
            {
                Title       = "Мат в 1 ход #3",
                Description = "Белые ставят мат одним ходом",
                Difficulty  = "Лёгкая",
                FEN         = "4k3/4Q3/4K3/8/8/8/8/8 w - - 0 1",
                Solution    = "e7e8",
                Hint        = "Ферзь на e8"
            },
            new Puzzle
            {
                Title       = "Вилка пешкой",
                Description = "Пешка атакует две фигуры одновременно",
                Difficulty  = "Лёгкая",
                FEN         = "8/8/8/3n1r2/4P3/8/8/4K2k w - - 0 1",
                Solution    = "e4e5",
                Hint        = "Пешка вперёд с вилкой"
            },
        };
    }
}
