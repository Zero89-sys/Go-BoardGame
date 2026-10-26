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
        private static readonly Lazy<Bitmap> DefaultAvatar = new(() =>
            new Bitmap(AssetLoader.Open(new Uri("avares://Gogame/Assets/default-avatar.jpg"))));
        [ObservableProperty]
        private Bitmap? _avatar = DefaultAvatar.Value;

        [ObservableProperty] private bool _isLoggedIn;
        [ObservableProperty] private string? _username;
        [ObservableProperty] private string? _currentRank;

        // Stats
        [ObservableProperty] private string _winrate = "--";
        [ObservableProperty] private string _rankLast30Days = "--";
        [ObservableProperty] private int _winStreak;
        [ObservableProperty] private int _puzzlesSolved;
        [ObservableProperty] private int _gamesPlayed;

        public void SignIn(UserProfileDto profile, ApiClient api)
        {
            Username = profile.Username;
            CurrentRank = profile.Stats.CurrentRank ?? "--";
            Winrate = profile.Stats.Winrate ?? "--";
            RankLast30Days = profile.Stats.RankLast30Days ?? "--";
            WinStreak = profile.Stats.WinStreak;
            PuzzlesSolved = profile.Stats.PuzzlesSolved;
            GamesPlayed = profile.Stats.GamesPlayed;
            IsLoggedIn = true;

            _ = LoadAvatarAsync(api, profile.ProfilePicture);
        }

        private async Task LoadAvatarAsync(ApiClient api, string? path)
        {
            var bitmap = await api.LoadImageAsync(path);
            Avatar = bitmap ?? DefaultAvatar.Value;
        }

        public void SignOut()
        {
            IsLoggedIn = false;
            Username = null;
            Avatar = DefaultAvatar.Value;
            CurrentRank = "--";
            Winrate = "--";
            RankLast30Days = "--";
            WinStreak = 0;
            PuzzlesSolved = 0;
            GamesPlayed = 0;
        }
    }
}
