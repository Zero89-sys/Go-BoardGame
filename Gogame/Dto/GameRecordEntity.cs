using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Dto
{
    public class GameRecordEntity
    {
        public int Id { get; set; }
        public Guid ClientId { get; set; } = Guid.NewGuid();
        public int? ServerId { get; set; }
        public int? UserId { get; set; }
        public string? PlayerColor { get; set; } = "B";
        public bool VsBot { get; set; }
        public string? BotRating { get; set; }
        public string SgfContent { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
        public bool IsSynced { get; set; }
        public DateTime? SyncedAt { get; set; }
    }

    public class LocalUserStatsEntity
    {
        public int Id { get; set; }
        public string CurrentRank { get; set; } = "--";
        public int PuzzlesSolved { get; set; }
        public int GamesPlayed { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class GameHistoryItemDto
    {
        public int Id { get; set; }
        public string Opponent { get; set; } = string.Empty;
        public string? OpponentRating { get; set; }
        public string Result { get; set; } = string.Empty;
        public bool IsWin { get; set; }
        public int BoardSize { get; set; }
        public DateTime PlayedAtUtc { get; set; }
    }

    public class GameHistoryPage
    {
        public int Total { get; set; }
        public List<GameHistoryItemDto> Items { get; set; } = new();
    }

    public class GameHistoryRow
    {
        public string Opponent { get; init; } = "";
        public string RatingText { get; init; } = "";
        public bool HasRating => RatingText.Length > 0;
        public string Result { get; init; } = "";
        public bool IsWin { get; init; }
        public string BoardText { get; init; } = "";
        public string DateText { get; init; } = "";

        public static GameHistoryRow From(GameHistoryItemDto g)
        {
            var local = DateTime.SpecifyKind(g.PlayedAtUtc, DateTimeKind.Utc).ToLocalTime();

            return new GameHistoryRow
            {
                Opponent = $"vs. {g.Opponent}",
                RatingText = string.IsNullOrEmpty(g.OpponentRating) ? "" : $"({g.OpponentRating})",
                Result = g.Result,
                IsWin = g.IsWin,
                BoardText = $"{g.BoardSize}×{g.BoardSize}",
                DateText = local.Year == DateTime.Now.Year
                    ? local.ToString("MMM d", CultureInfo.InvariantCulture)
                    : local.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
            };
        }
    }
}
