using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gogame.Dto;
using Gogame.Models;
using Gogame.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly ApiClient _api;
        public UserSession Session => App.Session;
        public string Username => Session.Username ?? string.Empty;
        public string Email => Session.Email ?? string.Empty;
        public string Bio => Session.Bio ?? string.Empty;
        public string Role => Session.Role ?? string.Empty;
        public string Winrate => Session.Winrate;
        public string CurrentRank => Session.CurrentRank ?? "--";
        public string RankLast30Days => Session.RankLast30Days;
        public int WinStreak => Session.WinStreak;
        public int PuzzlesSolved => Session.PuzzlesSolved;
        public int GamesPlayed => Session.GamesPlayed;

        [ObservableProperty]
        private Bitmap? _avatarPreview;

        public ProfileViewModel(ApiClient api)
        {
            _api = api;

            UserName = Session.Username ?? string.Empty;
            EditBio = Session.Bio ?? string.Empty;
            SecurityEmail = Session.Email ?? string.Empty;
            AvatarPreview = Session.Avatar;

            Session.PropertyChanged += OnSessionChanged;
        }
        // Session update
        private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserSession.Avatar))
                AvatarPreview = Session.Avatar;
        }
        public bool IsAdmin => Role.Equals("Admin");

        // Edit profile window
        [ObservableProperty]
        private bool _editModeHidden = true;
        [ObservableProperty]
        private bool _securityModeHidden = true;

        // Edit profile inputs
        [ObservableProperty]
        private string _userName = string.Empty;
        [ObservableProperty]
        private string _editBio = string.Empty;

        // Path to the new profile picture
        private string? _selectedAvatarPath;

        // Edit profilie errors
        [ObservableProperty]
        private string _usernameError = string.Empty;
        [ObservableProperty]
        private string _bioError = string.Empty;
        [ObservableProperty]
        private string _generalError = string.Empty;

        // Edit security inputs
        [ObservableProperty]
        private string _securityEmail = string.Empty;
        [ObservableProperty]
        private string _securityPassword = string.Empty;
        [ObservableProperty]
        private string _securityConfirmPassword = string.Empty;

        // Edit security errors
        [ObservableProperty]
        private string _securityEmailError = string.Empty;
        [ObservableProperty]
        private string _securityPasswordError = string.Empty;
        [ObservableProperty]
        private string _securityConfirmPasswordError = string.Empty;

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

        // Save profile changes
        [RelayCommand]
        public async Task SaveEditProfileAsync()
        {
            ClearErrors();
            var updateDto = new UpdateUserDto
            {
                Username = UserName,
                Bio = EditBio,
            };

            var result = await _api.EditProfileAsync(updateDto, _selectedAvatarPath);

            if(result.Success && result.Data != null)
            {
                await App.Session.SignIn(result.Data, _api);
                Navigator.NavigateTo(new ProfileView());
            }
            else
            {
                UsernameError = Join(result, "Username");
                BioError = Join(result, "Bio");
                GeneralError = "Failed to update profile";
            }
        }

        // Avatar selection
        [RelayCommand]
        private async Task SelectAvatarAsync()
        {
            var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = desktop?.MainWindow;

            if (mainWindow == null) return;

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a profile picture",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Pictures")
                    {
                        Patterns = new[] {"*.png", "*.jpg", "*.jpeg", "*.webp"}
                    }
                }
            });

            if(files.Count > 0)
            {
                var file = files[0];
                _selectedAvatarPath = file.Path.LocalPath;

                // Avatar preview
                AvatarPreview = new Bitmap(_selectedAvatarPath);
            }
        }

        // Save security changes
        [RelayCommand]
        public async Task SaveSecurityProfileAsync()
        {
            ClearErrors();

            if (SecurityPassword != SecurityConfirmPassword)
            {
                SecurityConfirmPasswordError = "Mismatched password";
                return;
            }

            var result = await _api.UpdateSecurityAsync(new UpdateUserSecurityDto
            {
                Email = SecurityEmail,
                Password = SecurityPassword,
            });

            if (result.Success)
            {
                App.Session.Email = SecurityEmail;
                Navigator.NavigateTo(new ProfileView());
            }
            else
            {
                SecurityEmailError = Join(result, "Email");
                SecurityPasswordError = Join(result, "Password");
                SecurityConfirmPasswordError = Join(result, "ConfirmPassword");
                GeneralError = Join(result, "");
            }
        }

        private static string Join<T>(ApiResult<T> r, string key)
            => r.Errors.TryGetValue(key, out var list) ? string.Join("\n", list) : "";

        // Log out
        [RelayCommand]
        private async Task Logout()
        {
            await _api.LogoutAsync();
            _api.ClearSession();
            App.Session.SignOut();
            Navigator.NavigateTo(new MenuView());
        }

        // Clear errors
        private void ClearErrors()
        {
            UsernameError = SecurityEmailError = SecurityPasswordError = SecurityConfirmPasswordError = GeneralError = "";
        }
    }
}
