using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gogame.ViewModels;
using System;
using System.Threading.Tasks;

namespace Gogame.Views;

public partial class MenuView : UserControl
{

    public MenuView()
    {
        InitializeComponent();


    }

    //New Player vs Player game
    private void OnPlayervsPlayer(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new GameView());

    // Exit
    private void OnExit(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.Close();
    }

    //Player vs Bot
    private void OnPlayerVsBotClick(object? sender, RoutedEventArgs e)
    {
        var gameView = new GameView();
        gameView.SetMode(GameView.GameMode.PlayerVsBot);
        Navigator.NavigateTo(gameView);
    }
    // Tutorial
    private void OnTutorialClick(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new TutorialView());

    // Puzzle
    private void OnPuzzleClick(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new PuzzleView());
}