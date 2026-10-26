using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Dto
{
    public class UserProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }

        public UserStatsDto Stats { get; set; } = new();
    }
    public class UserStatsDto
    {
        public string Winrate { get; set; } = string.Empty;
        public string CurrentRank { get; set; } = string.Empty;
        public string RankLast30Days { get; set; } = string.Empty;
        public int WinStreak { get; set; }
        public int PuzzlesSolved { get; set; }
        public int GamesPlayed { get; set; }
    }
}
