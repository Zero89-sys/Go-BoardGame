using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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
        public string Email => App.Session.Email ?? string.Empty;
        public string Bio => App.Session.Bio ?? string.Empty;
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

        // Edit profile window
        [ObservableProperty]
        private bool _editModeHidden = true;
        [ObservableProperty]
        private bool _securityModeHidden = true;

        [RelayCommand]
        private void ToggleEditMode()
        {
            EditModeHidden = !EditModeHidden;
            SecurityModeHidden = true;
        }
        [RelayCommand]
        private void ToggleSecurityMode()
        {
            SecurityModeHidden = !SecurityModeHidden;
            EditModeHidden = true;
        }
        [RelayCommand]
        private void CloseAll()
        {
            EditModeHidden = true;
            SecurityModeHidden = true;
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
