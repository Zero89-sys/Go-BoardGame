using Avalonia.Metadata;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gogame.Views;
using static Gogame.Models.GoGame;

namespace Gogame.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }
    public MainViewModel()
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        CurrentView = new MenuView();
    }

    public void ShowGame()
    {
        CurrentView = new GameView();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

}
