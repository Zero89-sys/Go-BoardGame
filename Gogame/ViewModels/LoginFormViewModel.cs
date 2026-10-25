using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.ViewModels
{
    internal partial class LoginFormViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoginVisible = true;

        [RelayCommand]
        private void ToggleMode()
        {
            IsLoginVisible = !IsLoginVisible;
        }
    }
}
