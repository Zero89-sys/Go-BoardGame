using Avalonia.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using Gogame.Models;
using Gogame.Views;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Gogame.Models.GoGame;

namespace Gogame.ViewModels;

public partial class MainViewModel : ObservableObject
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
        CurrentView = new LoadingScreen();
    }

    public void Initialize(Func<Task> startBot)
    {
        _ = RunInitAsync(startBot);
    }

    private async Task RunInitAsync(Func<Task> startBot)
    {
        try
        {
            await startBot();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"KataGo failed to start: {ex.Message}");
        }
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
