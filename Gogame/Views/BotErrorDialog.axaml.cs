using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gogame.Views;

namespace Gogame;

public partial class BotErrorDialog : Window
{
    public BotErrorDialog()
    {
        InitializeComponent();
    }

    // Return to menu
    private void OnBackToMenu(object? sender, RoutedEventArgs e)
    {
        if (this.Owner is MainWindow mainWindow)
        {
            mainWindow.MainContent.Content = new MenuView();
        }
        Close();
    }
}