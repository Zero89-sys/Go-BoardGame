using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gogame.Dto;
using Gogame.Models;
using Gogame.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gogame.ViewModels
{
    internal partial class LoginFormViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoginVisible = true;

        [RelayCommand]
        private void ToggleMode()
        {
            ClearErrors();
            IsLoginVisible = !IsLoginVisible;
        }

        // API using
        private readonly ApiClient _api;

        // Inputs
        [ObservableProperty]
        private string _username = string.Empty;
        [ObservableProperty]
        private string _email = string.Empty;
        [ObservableProperty]
        private string _password = string.Empty;
        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        //Errors
        [ObservableProperty]
        private string _usernameError = string.Empty;
        [ObservableProperty]
        private string _emailError = string.Empty;
        [ObservableProperty]
        private string _passwordError = string.Empty;
        [ObservableProperty]
        private string _confirmPasswordError = string.Empty;
        [ObservableProperty]
        private string _generalError = string.Empty;


        public bool isLoggedIn = false;

        public LoginFormViewModel(ApiClient api)
        {
            _api = api;
        }

        // Login validation
        [RelayCommand]
        private async Task LoginAsync()
        {
            ClearErrors();

            var result = await _api.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password
            });

            if (result.Success)
            {
                var profile = await _api.GetProfileAsync();

                if (profile.Success && profile.Data != null)
                {
                    App.Session.SignIn(profile.Data, _api);
                    Navigator.NavigateTo(new ProfileView());
                }
                else
                {
                    GeneralError = "Login worked, but the profile could not be loaded.";
                }
            }
            else
            {
                EmailError = Join(result, "Email");
                PasswordError = Join(result, "Password");
                GeneralError = Join(result, "");
            }
        }

        // Register validation
        [RelayCommand]
        private async Task RegisterAsync()
        {
            ClearErrors();

            if (Password != ConfirmPassword)
            {
                ConfirmPasswordError = "Passwords do not match";
                return;
            }

                var result = await _api.RegisterAsync(new RegisterRequest
            {
                Username = Username,
                Email = Email,
                Password = Password,
            });

            if (result.Success && ConfirmPasswordError.Length == 0)
            {
                Password = "";
                ConfirmPassword = "";
                IsLoginVisible = true;
            }
            else
            {
                UsernameError = Join(result, "Username");
                EmailError = Join(result, "Email");
                PasswordError = Join(result, "Password");
                ConfirmPasswordError = Join(result, "ConfirmPassword");
                GeneralError = Join(result, "");
            }
        }

        private static string Join(ApiResult<AuthResponse> r, string key)
            => r.Errors.TryGetValue(key, out var list) ? string.Join("\n", list) : "";

        // Clear errors
        private void ClearErrors()
        {
            UsernameError = EmailError = PasswordError = ConfirmPasswordError = GeneralError = "";
        }
    }
}
