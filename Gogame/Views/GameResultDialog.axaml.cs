using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Views
{
    public partial class GameResultDialog : Window
    {
        public GameResultDialog()
        {
            InitializeComponent();
        }

        // Restart
        public event EventHandler? RestartRequested;
        private void OnRestartClicked(object sender, RoutedEventArgs e)
        {
            RestartRequested.Invoke(this, EventArgs.Empty);
            Close();
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

        // Close window
        private void OnCloseClicked(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
