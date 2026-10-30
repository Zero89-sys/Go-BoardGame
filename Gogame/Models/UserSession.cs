using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Gogame.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Models
{
    public partial class UserSession : ObservableObject
    {
        [ObservableProperty]
        private Bitmap? _avatar;

        [ObservableProperty] private int? _userId;
        [ObservableProperty] private bool _isLoggedIn;
        [ObservableProperty] private string? _username;
        [ObservableProperty] private string? _email;
        [ObservableProperty] private string? _bio;
        [ObservableProperty] private string? _role;
        [ObservableProperty] private string? _currentRank;

        // Stats
        [ObservableProperty] private string _winrate = "--";
        [ObservableProperty] private string _rankLast30Days = "--";
        [ObservableProperty] private int _winStreak;
        [ObservableProperty] private int _puzzlesSolved;
        [ObservableProperty] private int _gamesPlayed;

        public async Task SignIn(UserProfileDto profile, ApiClient api)
        {
            IsLoggedIn = true;
            UserId = profile.Id;
            Role = profile.Role;
            Username = profile.Username;
            Email = profile.Email;
            Bio = profile.Bio;
            Avatar = await api.LoadImageAsync(profile.ProfilePicture);
            CurrentRank = profile.Stats.CurrentRank ?? "--";
            Winrate = profile.Stats.Winrate ?? "--";
            RankLast30Days = profile.Stats.RankLast30Days ?? "--";
            WinStreak = profile.Stats.WinStreak;
            PuzzlesSolved = profile.Stats.PuzzlesSolved;
            GamesPlayed = profile.Stats.GamesPlayed;
            IsLoggedIn = true;
        }

        public void SignOut()
        {
            UserId = null;
            IsLoggedIn = false;
            Username = null;
            Email = null;
            Bio = null;
            Role = null;
            Avatar = null;
            CurrentRank = "--";
            Winrate = "--";
            RankLast30Days = "--";
            WinStreak = 0;
            PuzzlesSolved = 0;
            GamesPlayed = 0;
        }
    }
}
