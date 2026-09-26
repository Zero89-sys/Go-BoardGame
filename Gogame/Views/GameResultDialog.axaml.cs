using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Views
{
    public partial class GameResultDialog : UserControl
    {
        public GameResultDialog()
        {
            InitializeComponent();
        }

        public event EventHandler? RestartRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler? BackToMenuRequested;

        // Restart
        private void OnRestartClicked(object sender, RoutedEventArgs e)
        {
            RestartRequested?.Invoke(this, EventArgs.Empty);
        }

        // Return to menu
        private void OnBackToMenu(object? sender, RoutedEventArgs e)
        {
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        // Close window
        private void OnCloseClicked(object? sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
