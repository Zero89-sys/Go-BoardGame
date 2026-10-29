using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Dto
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Role { get; set; }

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

    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdateUserSecurityDto
    {
        public string? Email {  get; set; }
        public string? Password {  get; set; }
    }
}
