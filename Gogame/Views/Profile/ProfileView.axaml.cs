using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gogame.Views;

namespace Gogame;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    // Back to menu button
    private void OnBackToMenu(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new MenuView());
}