using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using Gogame.ViewModels;
using System;
using System.Threading.Tasks;

namespace Gogame.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Method for cross-fade animation
    public async Task FadeOverlayAsync(object newView)
    {
        FadeOverlay.Opacity = 1.0;
        await Task.Delay(400);

        if (DataContext is MainViewModel vm)
        {
            vm.CurrentView = newView;
        }

        await Task.Delay(100);

        FadeOverlay.Opacity = 0;
    }
}
