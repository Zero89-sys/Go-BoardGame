using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Gogame.ViewModels;
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
    {
        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;

        if (mainWindow != null)
        {
            mainWindow.MainContent.Content = new GameView();

            if (mainWindow.DataContext is MainViewModel vm)
            {
                vm.CurrentView = mainWindow.MainContent.Content;
            }
        }
    }

    // Exit
    private void OnExit(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.Close();
    }

    //Player vs Bot
    private void OnPlayerVsBotClick(object? sender, RoutedEventArgs e)
    {
        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        if (mainWindow != null)
        {
            var gameView = new GameView();
            gameView.SetMode(GameView.GameMode.PlayerVsBot);
            mainWindow.MainContent.Content = gameView;
        }
    }
}