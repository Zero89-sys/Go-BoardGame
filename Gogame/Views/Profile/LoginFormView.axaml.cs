using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gogame.ViewModels;
using Gogame.Views;

namespace Gogame;

public partial class LoginFormView : UserControl
{
    public LoginFormView()
    {
        InitializeComponent();
        DataContext = new LoginFormViewModel(App.Api);
    }
    // Back to menu button
    private void OnBackToMenu(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new MenuView());
}