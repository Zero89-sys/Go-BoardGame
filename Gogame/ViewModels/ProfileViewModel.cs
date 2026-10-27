using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gogame.Models;
using Gogame.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly ApiClient _api;
        public string Username => App.Session.Username ?? string.Empty;
        public Bitmap Avatar => App.Session.Avatar;
        public string Winrate => App.Session.Winrate;
        public string CurrentRank => App.Session.CurrentRank ?? "--";
        public string RankLast30Days => App.Session.RankLast30Days;
        public int WinStreak => App.Session.WinStreak;
        public int PuzzlesSolved => App.Session.PuzzlesSolved;
        public int GamesPlayed => App.Session.GamesPlayed;

        public ProfileViewModel(ApiClient api)
        {
            _api = api;
        }

        // Log out
        [RelayCommand]
        private async Task Logout()
        {
            await _api.LogoutAsync();
            _api.ClearSession();
            App.Session.SignOut();
            Navigator.NavigateTo(new MenuView());
        }
    }
}
