using System;
using System.Collections.Generic;
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
}
