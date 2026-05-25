using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gogame.Models;
using Gogame.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Globalization;
using static Gogame.Models.GoGame;
using FilePath = System.IO.Path;

namespace Gogame.Views;

public partial class GameView : UserControl
{
    private GoBoard board = new GoBoard(19);

    private const int BoardSizePx = 800;
    private const int Margin = 30;

    private Ellipse? ghostStone;
    private int ghostX = -1;
    private int ghostY = -1;

    private TextBlock? Turn_Text;
    private TextBlock? Capture_Text;
    private TextBlock? WhiteCapture_Text;
    private TextBlock? BlackCapture_Text;

    private Button? WhiteResign_Button;
    private Button? BlackResign_Button;

    private GtpService _botService = GtpService.Instance;
    private bool _isBotThinking = false;
    private bool _isBotReady = false;
    private bool _botInitializing = false;
    private string? _finalOwnershipData;
    private HashSet<(int x, int y)> _deadStones = new();

    enum GameState
    {
        Setup,
        Playing,
        GameOver
    }
    private GameState _gameState = GameState.Setup;
    private int _selectedBoardSize = 19;

    private Stone _playerColor = Stone.Black;
    private Stone _botColor => _playerColor == Stone.Black ? Stone.White : Stone.Black;
    public enum GameMode
    {
        PlayerVsPlayer,
        PlayerVsBot
    }
    private GameMode _currentMode = GameMode.PlayerVsPlayer;
    public GameView()
    {
        InitializeComponent();

        if (BotOnlyPanel != null)
        {
            BotOnlyPanel.IsVisible = (_currentMode == GameMode.PlayerVsBot);
        }

        BoardCanvas = this.FindControl<Canvas>("BoardCanvas");
        Turn_Text = this.FindControl<TextBlock>("TurnText");
        WhiteCapture_Text = this.FindControl<TextBlock>("WhiteCaptureText");
        BlackCapture_Text = this.FindControl<TextBlock>("BlackCaptureText");
        
        WhiteResign_Button = this.FindControl<Button>("WhiteResignButton");
        BlackResign_Button = this.FindControl<Button>("BlackResignButton");

        Avalonia.Threading.Dispatcher.UIThread.Post(() => DrawSetupStones(),
        Avalonia.Threading.DispatcherPriority.Background);

        UpdatePassVisibility();
        UpdateTurnText();
        UpdateCaptureText();
        UpdateButtons();
        DrawBoard();
    }
    // Mouse click
    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_gameState != GameState.Playing || _isBotThinking)
            return;

        if (IsPvBot && board.CurrentPlayer != _playerColor)
            return;

        var pos = e.GetPosition(BoardCanvas);
        if (!TryGetBoardCoordinates(pos, out int x, out int y))
            return;

        Stone playerNow = board.CurrentPlayer;
        if (!board.PlaceStone(x, y, playerNow))
            return;

        string colorCode = playerNow == Stone.Black ? "B" : "W";
        await _botService.SendCommand($"play {colorCode} {ConvertToGtpCoords(x, y)}");

        board.SwitchTurnAfterMove();
        SyncAndRefreshUI();

        if (board.IsGameOver)
        {
            _gameState = GameState.GameOver;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowGameResult();
            });
            return;
        }

        // bot's move
        if (IsPvBot && board.CurrentPlayer == _botColor)
        {
            _isBotThinking = true;
            try
            {
                string botColorStr = _botColor == Stone.Black ? "B" : "W";
                string response = await _botService.SendCommand($"genmove {botColorStr}");
                ApplyBotMove(response);
            }
            finally
            {
                _isBotThinking = false;
                SyncAndRefreshUI();
                if (board.IsGameOver) ShowGameResult();
            }
        }
    }

    // Render
    private void DrawBoard()
    {
        BoardCanvas.Children.Clear();

        int size = board.Size;
        double cell = (BoardSizePx - 2.0 * Margin) / (size - 1);

        // Grid
        for (int i = 0; i < size; i++)
        {
            double pos = Margin + i * cell;

            BoardCanvas.Children.Add(new Line
            {
                StartPoint = new Avalonia.Point(Margin, pos),
                EndPoint = new Avalonia.Point(BoardSizePx - Margin, pos),
                Stroke = Brushes.Black
            });

            BoardCanvas.Children.Add(new Line
            {
                StartPoint = new Avalonia.Point(pos, Margin),
                EndPoint = new Avalonia.Point(pos, BoardSizePx - Margin),
                Stroke = Brushes.Black
            });
        }

        // Stones
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (board.Board[x, y] == Stone.Empty)
                    continue;

                bool isBlack = board.Board[x, y] == Stone.Black;
                double stoneSize = cell * 0.85;
                double xPos = Margin + x * cell - stoneSize / 2;
                double yPos = Margin + y * cell - stoneSize / 2;

                double lightX = 0.25 + _rng.NextDouble() * 0.15;
                double lightY = 0.20 + _rng.NextDouble() * 0.15;
                double highlightOffsetX = stoneSize * (0.18 + _rng.NextDouble() * 0.05);
                double highlightOffsetY = stoneSize * (0.14 + _rng.NextDouble() * 0.05);

                var baseShadow = new Ellipse
                {
                    Width = cell * 0.88,
                    Height = cell * 0.88,
                    Fill = new SolidColorBrush(isBlack? Color.FromArgb(90,0,0,0):Color.FromArgb(70, 0, 0, 0)),
                    Opacity = 0.4,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(baseShadow, xPos + stoneSize * 0.06);
                Canvas.SetTop(baseShadow, yPos + stoneSize * 0.10);

                var whiteStoneBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.4, 0.38, RelativeUnit.Relative),
                    Radius = 1.0,
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#FAFAFA"),0),
                        new GradientStop(Color.Parse("#EDEDED"), 0.45),
                        new GradientStop(Color.Parse("#D8D8D8"), 0.75),
                        new GradientStop(Color.Parse("#BEBEBE"),1)
                    }
                };

                var whiteHighlight = new Ellipse
                {
                    Width = stoneSize * 0.55,
                    Height = stoneSize * 0.35,
                    Fill = new RadialGradientBrush
                    {
                        Center = new RelativePoint(0.45, 0.4, RelativeUnit.Relative),
                        Radius = 0.9,
                        GradientStops =
                        {
                            new GradientStop(Color.FromArgb(25, 255, 255, 255), 0),
                            new GradientStop(Color.FromArgb(15, 255, 255, 255), 0.5),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        }
                    },
                    IsHitTestVisible = false
                };
                
                Canvas.SetLeft(whiteHighlight, xPos + cell * 0.16);
                Canvas.SetTop(whiteHighlight, yPos + cell * 0.18);

                var blackStoneBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(lightX, lightY, RelativeUnit.Relative),
                    Radius = 0.65,
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#3A3A3A"), 0),
                        new GradientStop(Color.Parse("#1A1A1A"), 0.45),
                        new GradientStop(Color.Parse("#000000"), 1)
                    }
                };

                var blackHighlight = new Ellipse
                {
                    Width = cell * 0.45,
                    Height = cell * 0.22,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop(Color.FromArgb(55,255,255,255), 0),
                            new GradientStop(Color.FromArgb(15, 255, 255, 255), 0.4),
                            new GradientStop(Color.FromArgb(0,255,255,255), 1)
                        }
                    },
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(blackHighlight, xPos + stoneSize * 0.18);
                Canvas.SetTop(blackHighlight, yPos + stoneSize * 0.14);

                var stone = new Ellipse
                {
                    Width = stoneSize,
                    Height = stoneSize,
                    Fill = isBlack ? blackStoneBrush : whiteStoneBrush,

                    Stroke = new SolidColorBrush(
                        isBlack
                            ? Color.FromArgb(45, 255, 255, 255)
                            : Color.FromArgb(60, 0, 0, 0)
                    ),

                    StrokeThickness = 0.6,                  
                    IsHitTestVisible = false,
                    Tag = $"stone_{x}_{y}"
                };
                stone.StrokeThickness = stoneSize * 0.015;
                Canvas.SetLeft(stone, xPos);
                Canvas.SetTop(stone, yPos);
                BoardCanvas.Children.Add(baseShadow);
                BoardCanvas.Children.Add(stone);
                BoardCanvas.Children.Add(isBlack ? blackHighlight : whiteHighlight);
            }
        }
    }
    private readonly Random _rng = new();

    // Click on intersections
    private bool TryGetBoardCoordinates(Point pos, out int x, out int y)
    {
        double cell = (BoardSizePx - 2.0 * Margin) / (board.Size - 1);

        x = (int)Math.Round((pos.X - Margin) / cell);
        y = (int)Math.Round((pos.Y - Margin) / cell);

        return board.IsOnBoard(x, y);
    }

    // Ghost stone
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (BoardCanvas == null)
            return;

        if(_currentMode == GameMode.PlayerVsBot && board.CurrentPlayer != _playerColor)
        {
            RemoveGhostStone();
            return;
        }
        if (_gameState != GameState.Playing)
        {
            RemoveGhostStone();
            return;
        }

        var pos = e.GetPosition(BoardCanvas);

        if (!TryGetBoardCoordinates(pos, out int x, out int y))
        {
            RemoveGhostStone();
            return;
        }
        if (board.Board[x, y] != Stone.Empty)
        {
            RemoveGhostStone();
            return;
        }

        if (x == ghostX && y == ghostY)
            return;

        ghostX = x;
        ghostY = y;

        RemoveGhostStone();
        DrawGhostStone(x, y);
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        RemoveGhostStone();
    }

    // Rendering ghost stone
    private void DrawGhostStone(int x, int y)
    {
        if (BoardCanvas == null)
            return;

        double cell = (BoardSizePx - 2.0 * Margin) / (board.Size - 1);

        ghostStone = new Ellipse
        {
            Width = cell * 0.8,
            Height = cell * 0.8,
            Fill = board.CurrentPlayer == Stone.Black
                ? Brushes.Black
                : Brushes.White,
            Opacity = 0.4,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(ghostStone, Margin + x * cell - ghostStone.Width / 2);
        Canvas.SetTop(ghostStone, Margin + y * cell - ghostStone.Width / 2);

        BoardCanvas.Children.Add(ghostStone);
    }

    // Removing ghost stone
    private void RemoveGhostStone()
    {
        if (ghostStone != null && BoardCanvas != null)
        {
            BoardCanvas.Children.Remove(ghostStone);
            ghostStone = null;
            ghostX = ghostY = -1;
        }
    }

    // Update the text
    private void UpdateTurnText()
    {
        if (TurnText == null)
            TurnText = this.FindControl<TextBlock>("TurnText");
        if (TurnText == null) return;

        if (board.CurrentPlayer == Stone.Black)
        {
            TurnText.Text = "Black to move";
            TurnText.Foreground = Brushes.Black;
        }
        else
        {
            TurnText.Text = "White to move";
            TurnText.Foreground = Brushes.Gray;
        }
    }

    // Update text for number of captures
    private void UpdateCaptureText()
    {
        if (WhiteCapture_Text != null)
        {
            WhiteCapture_Text.Text = $"White captures: {board.WhiteCaptures}";
        }
        if (BlackCapture_Text != null)
        {
            BlackCapture_Text.Text = $"Black captures: {board.BlackCaptures}";
        }
    }

    // Pass
    private async void OnPassClicked(object? sender, RoutedEventArgs e)
    {
        if (_isBotThinking || board.IsGameOver || _gameState != GameState.Playing)
        {
            return;
        }

        Stone passer = board.CurrentPlayer;

        // Player pass
        if (!board.Pass(passer))
        {
            return;
        }

        if (_currentMode == GameMode.PlayerVsBot)
        {
            string colorCode = passer == Stone.Black ? "B" : "W";
            await _botService.SendCommand($"play {colorCode} pass");
        }


        SyncAndRefreshUI();

        if (board.IsGameOver)
        {
            await RunFinalAnalysisAsync();

            string? botScore = null;
            if (_currentMode == GameMode.PlayerVsBot)
            {
                botScore = await _botService.SendCommand("final_score");
            }
            ShowGameResult(null, botScore);
            return;
        }

        // Bot's pass
        if (_currentMode == GameMode.PlayerVsBot && board.CurrentPlayer == _botColor)
        {
            _isBotThinking = true;

            try
            {
                string botColorCode = _botColor == Stone.Black ? "B" : "W";
                string response = await _botService.SendCommand($"genmove {botColorCode}");
                ApplyBotMove(response);
            }
            finally
            {
                _isBotThinking = false;
                SyncAndRefreshUI();
            }
        }
    }

    // Pass button visibility
    private void UpdatePassVisibility()
    {
        if (IsPvBot)
        {
            PassButtonW.IsVisible = _playerColor == Stone.White;
            WhiteResign_Button!.IsVisible = _playerColor == Stone.White;

            PassButtonB.IsVisible = _playerColor == Stone.Black;
            BlackResignButton!.IsVisible = _playerColor == Stone.Black;
        }
        else
        {
            PassButtonW.IsVisible = true;
            WhiteResign_Button!.IsVisible = true;

            PassButtonB.IsVisible = true;
            BlackResign_Button!.IsVisible = true;
        }
    }

    // Resign click
    private void OnResignClicked(object? sender, RoutedEventArgs e)
    {
        var winner = board.Resign();
        ShowGameResult(winner);
    }

    // Counting territory
    public (double blackTerritory, double whiteTerritory) CalculateTerritory(double[] ownership)
    {
        double black = 0;
        double white = 0;

        foreach (var val in ownership)
        {
            if (val > 0.5) black++;
            else if (val < -0.5) white++;
        }
        return (black, white);
    }

    // Display the result
    private async void ShowGameResult(Stone? resignWinner = null, string? botScore = null)
    {
        if (_gameState == GameState.GameOver && resignWinner == null && botScore == null)
            return;

        _gameState = GameState.GameOver;
        BoardCanvas.IsHitTestVisible = false;
        await RunFinalAnalysisAsync();
        string text;

        double blackTerritory = 0;
        double whiteTerritory = 0;

        if (_botService.LastOwnership != null)
        {
            var result = CalculateTerritory(_botService.LastOwnership);
            blackTerritory = result.blackTerritory;
            whiteTerritory = result.whiteTerritory;
        }
        double komi = 6.5;
        double blackFinal = blackTerritory + board.WhiteCaptures;
        double whiteFinal = whiteTerritory + board.BlackCaptures + komi;

        Stone winningColor;
        string reason = "";
        if (resignWinner.HasValue)
        {
            winningColor = resignWinner.Value;
            reason = "by resignation";
        }
        else
        {
            winningColor = blackFinal > whiteFinal ? Stone.Black : Stone.White;
            double diff = Math.Abs(blackFinal - whiteFinal);
            reason = $"by {diff:F1} points";
        }

        string resultText;
        if (_currentMode == GameMode.PlayerVsPlayer)
        {
            resultText = $"{winningColor} wins {reason}";
        }
        else
        {
            if (winningColor == _playerColor)
            {
                resultText = $"You win {reason}";
            }
            else
            {
                resultText = $"Bot wins {reason}";
            }
        }

        // Details
        string details =
                $"BLACK:\n" +
                $"Territory: {blackTerritory}\n" +
                $"Captures: {board.WhiteCaptures}" +
                $"Total: {blackFinal}\n\n" +
                $"WHITE:\n" +
                $"Territory: {whiteTerritory}" +
                $"Captures: {board.BlackCaptures}" +
                $"Komi: {komi}\n" +
                $"Total: {whiteFinal}";

        var dialog = new Window
        {
            Width = 400,
            Height = 300,
            Title = "Game result",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brushes.AntiqueWhite,
            CanResize = false,
            SystemDecorations = SystemDecorations.BorderOnly
        };

        var closeButton = new Border
        {
            Background = Brushes.LightGray,
            Padding = new Thickness(20, 5),
            CornerRadius = new CornerRadius(3),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Child = new TextBlock
            {
                Text = "Close",
                Foreground = Brushes.Black,
                FontSize = 18,
            }
        };
        closeButton.PointerEntered += (s, e) => closeButton.Background = Brushes.DarkGray;
        closeButton.PointerExited += (s, e) => closeButton.Background = Brushes.LightGray;
        closeButton.PointerPressed += (s, e) => dialog.Close();

        var restartButton = new Border
        {
            Background = Brushes.LightGray,
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(20, 5),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Child = new TextBlock
            {
                Text = "Restart Game",
                Foreground = Brushes.Black,
                FontSize = 18,
            }
        };

        restartButton.PointerEntered += (s, e) => restartButton.Background = Brushes.DarkGray;
        restartButton.PointerExited += (s, e) => restartButton.Background = Brushes.LightGray;
        

        restartButton.PointerPressed += async (s, e) =>
        {
            await RestartGameAsync();
            dialog.Close();
        };


        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Spacing = 10,
            Children = { closeButton, restartButton }
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = resultText,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 18,
                    Foreground = Brushes.Black,
                    TextWrapping = TextWrapping.Wrap
                },
                buttonPanel
            }
        };
        var topLevel = TopLevel.GetTopLevel(this);
        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    // Restart
    private async Task RestartGameAsync()
    {
        BoardCanvas.IsHitTestVisible = true;
        RemoveGhostStone();
        await _botService.SendRawCommand("stop");
        await _botService.SendRawCommand("clear_board");
        _botService.ClearAnalysisState();

        board = new GoBoard(_selectedBoardSize);

        _gameState = GameState.Playing;
        _isBotThinking = false;

        DrawBoard();
        UpdatePassVisibility();
        SyncAndRefreshUI();
        _botService.ResetServiceState();

        if (_currentMode == GameMode.PlayerVsBot)
        {
            _isBotReady = false;
            await _botService.SendCommand("stop");
            await _botService.SendCommand($"boardsize {board.Size}");
            await _botService.SendCommand("clear_board");
            await _botService.SendCommand($"komi 6.5");

            _isBotReady = true;

            if(_botColor == Stone.Black)
            {
                _isBotThinking = true;
                string response = await _botService.SendCommand("genmove B");
                ApplyBotMove(response);
                _isBotThinking = false;
            }
        }
        SyncAndRefreshUI();
    }

    // Check for game exit
    private async void CheckGameOver() 
    {
        if (board.IsGameOver)
        {
            await RunFinalAnalysisAsync();

            string? score = null;
            if (_currentMode == GameMode.PlayerVsBot)
            {
                score = await _botService.SendCommand("final_score");
            }
            ShowGameResult(null, score);
        }
    }

    // Resign for white
    private void OnWhiteResign(object? sender, RoutedEventArgs e)
    {
        var winner = board.Resign(Stone.White);
        if (winner.HasValue)
        {
            ShowGameResult(winner.Value);
        }
    }
    // Resign for black
    private void OnBlackResign(object? sender, RoutedEventArgs e)
    {
        var winner = board.Resign(Stone.Black);
        if (winner.HasValue)
        {
            ShowGameResult(winner.Value);
        }
    }

    //Disabling buttons when it's not the player's turn
    private void UpdateButtons()
    {
        if (board.IsGameOver)
        {
            DisableAllButtons();
            return;
        }

        if (_currentMode == GameMode.PlayerVsPlayer)
        {
            WhiteResign_Button!.IsEnabled = (board.CurrentPlayer == Stone.White);
            BlackResign_Button!.IsEnabled = (board.CurrentPlayer == Stone.Black);

            PassButtonW.IsEnabled = (board.CurrentPlayer == Stone.White);
            PassButtonB.IsEnabled = (board.CurrentPlayer == Stone.Black);
        }
        else
        {
            bool isHumanTurn = board.CurrentPlayer == _playerColor;

            WhiteResign_Button!.IsEnabled = isHumanTurn && board.CurrentPlayer == Stone.White;
            BlackResign_Button!.IsEnabled = isHumanTurn && board.CurrentPlayer == Stone.Black;

            PassButtonW.IsEnabled = isHumanTurn && (_playerColor == Stone.White);
            PassButtonB.IsEnabled = isHumanTurn && (_playerColor == Stone.Black);
        }     
    }

    private void DisableAllButtons()
    {
        WhiteResign_Button!.IsEnabled = false;
        BlackResign_Button!.IsEnabled = false;
    }

    private void SyncAndRefreshUI()
    {
        UpdateTurnText();
        UpdateButtons();
        UpdateCaptureText();
        if (_gameState != GameState.GameOver)
            DrawBoard();
    }

    // Display the territory
    private void DrawTerritoryOverlay(double[] ownership)
    {
        int dataSize = (int)Math.Round(Math.Sqrt(ownership.Length));
        double cell = (BoardSizePx - 2.0 * Margin) / (board.Size - 1);

        for (int i = 0; i < ownership.Length; i++)
        {
            double val = ownership[i];
            if (Math.Abs(val) < 0.2) continue; 

            int x = i % dataSize;
            int y = i / dataSize;

            Stone ownerColor;
            if (val > 0) ownerColor = Stone.Black;
            else ownerColor = Stone.White;
            

            var rect = new Rectangle
                {
                    Width = cell * 0.6,
                    Height = cell * 0.6,
                    Fill = ownerColor == Stone.White
                    ? new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) // white territory
                    : new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Opacity = 0.35,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(rect, Margin + x * cell - rect.Width / 2);
                Canvas.SetTop(rect, Margin + y * cell - rect.Height / 2);
                BoardCanvas.Children.Add(rect);
        }
    }

    // Back to menu button
    private void OnBackToMenu(object? sender, RoutedEventArgs e)
    {
        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        if (mainWindow != null)
        {
            mainWindow.MainContent.Content = new MenuView();
        }
    }

    // Resize the board
    private void OnBoardSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BoardSizeCombo?.SelectedIndex == null)
        {
            return;
        }

        _selectedBoardSize = BoardSizeCombo.SelectedIndex switch
        {
            1 => 13,
            2 => 9,
            _ => 19
        };

        board = new GoBoard(_selectedBoardSize);
        if (_gameState == GameState.Setup)
        {
            DrawSetupStones();
        }
        else
        {
            DrawBoard();
        }
    }

    //Player color
    private void OnColorChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb)
        {
            string colorName = rb.Content?.ToString() ?? "Black";
            _playerColor = rb.Content.ToString() == "White"
                ? Stone.White
                : Stone.Black;

            if (_gameState == GameState.Setup)
            {
                DrawSetupStones();
            }

            UpdateTurnText();
        }
    }

    // Starting the game
    private async void OnStartGame(object? sender, RoutedEventArgs e)
    {
        await _botService.StopAnalysisAsync();
        _botService.ResetServiceState();
        SetupPanel.IsVisible = false;
        _gameState = GameState.Playing;
        await _botService.SetBoardSize(_selectedBoardSize);
        await _botService.SendCommand("clear_board");
        BoardCanvas.IsHitTestVisible = true;

        board = new GoBoard(_selectedBoardSize);
        board.Reset();
        _botService.ClearAnalysisState();
        board.Size = _selectedBoardSize;


        TurnText.IsVisible = true;
        var whiteRow = this.FindControl<Grid>("WhiteInfoRow");
        var blackRow = this.FindControl<Grid>("BlackInfoRow");
        if (whiteRow != null) whiteRow.IsVisible = true;
        if (blackRow != null) blackRow.IsVisible = true;

        DrawBoard();

        if (_currentMode == GameMode.PlayerVsBot)
        {
            await _botService.SendCommand($"boardsize {_selectedBoardSize}");
            await _botService.SendCommand("clear_board");
            await _botService.SendCommand("komi 6.5");

            if (_botColor == Stone.Black)
            {
                _isBotThinking = true;
                string response = await _botService.SendCommand("genmove B");
                ApplyBotMove(response);
                _isBotThinking = false;
            }
        }
        SyncAndRefreshUI();
        UpdatePassVisibility();
    }

    // Coordinates for Engine
    private string ConvertToGtpCoords(int x, int y)
    {
        char col = (char)('A' + x);
        if (col >= 'I') col++;
        int row = board.Size - y;
        return $"{col}{row}";
    }

    private (int x, int y) ConvertFromGtpCoords(string gtp)
    {
        gtp = gtp.Replace("=", "").Trim().ToUpper();

        if (string.IsNullOrEmpty(gtp) || gtp == "PASS" || gtp == "RESIGN") 
            return(-1, -1);

        char colChar = gtp[0];
        int targetX = colChar - 'A';
        if (colChar > 'I') targetX--;

        if (int.TryParse(gtp.Substring(1), out int row))
        {
            int targetY = board.Size - row;
            return (targetX, targetY);
        }
        return (-1, -1);
    }

    // Switch mode
    public void SetMode(GameMode mode)
    {
        _currentMode = mode;

        if (BotOnlyPanel != null)
        {
            BotOnlyPanel.IsVisible = (mode == GameMode.PlayerVsBot);
        }

        if (_currentMode == GameMode.PlayerVsBot)
        {

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500);
                    var response = await _botService.SendCommand("boardsize 19");

                    if (response != null)
                    {
                        await _botService.SendCommand("clear_board");
                        int startLevel = 4;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            startLevel = (int)DifficultySlider.Value;
                        });

                        await SetBotDifficulty(startLevel);

                        _isBotReady = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Chyba při startu bota: {ex.Message}");
                }
                finally
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (BotLoadingOverlay != null)
                            BotLoadingOverlay.IsVisible = false;
                        Debug.WriteLine("Bot je připraven ke hře!");
                    });
                }
            });
        }
        else
        {
            _isBotReady = false;
            if (BotLoadingOverlay != null) BotLoadingOverlay.IsVisible = false;
        }
        UpdatePassVisibility();
    }

    // Difficulty
    public async Task SetBotDifficulty (int level)
    {
        int visits = level switch
        {
            1 => 1,
            2 => 5,
            3 => 10,
            4 => 30,
            5 => 50,
            6 => 150,
            7 => 200,
            8 => 500,
            9 => 800,
            10 => 1300,
            11 => 2000,
            _ => 1
        };
        await _botService.SendCommand($"kata-set-param maxVisits {visits}");

        string check = await _botService.SendCommand("kata-get-param maxVisits");
        Debug.WriteLine($"POTVRZENO: Bot má nastaveno: {check}");
    }

    // Difficulty text
    private string GetDifficultyName(int level)
    {
        return level switch
        {
            1 => "30 kyu",
            2 => "25 kyu",
            3 => "20 kyu",
            4 => "15 kyu",
            5 => "10 kyu",
            6 => "5 kyu",
            7 => "3 kyu",
            8 => "1 kyu",
            9 => "1 dan",
            10 => "2 dan",
            11 => "3 dan",
            _ => "30 kyu"
        };
    }

    // Connection to slider
    private bool _isUpdatingDifficulty = false;
    private async void OnDifficultyChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        int level = (int)e.NewValue;

        if (DifficultyText != null)
        {
            DifficultyText.Text = GetDifficultyName(level);
        }

        if (_currentMode == GameMode.PlayerVsBot && _isBotReady && !_isUpdatingDifficulty)
        {
            _isUpdatingDifficulty = true;
            try
            {
                await SetBotDifficulty(level);
            }
            finally
            {
                _isUpdatingDifficulty = false;
            }
        }
    }

    // Bot's movement
    private void ApplyBotMove(string response)
    {
        if (string.IsNullOrEmpty(response)) 
            return;

        response = response.Trim();

        if (!response.StartsWith("="))
            return;

        string move = response.Substring(1).Trim().ToLower();

        if (move == "pass")
        {
            board.Pass(_botColor);
            board.SwitchTurnAfterMove();
        }
        else if(move == "resign")
        {
            ShowGameResult(_playerColor);
            return;
        }
        else
        {
            var(x,y)=ConvertFromGtpCoords(move);

            if (x >= 0 && y >= 0)
            {
                if(board.PlaceStone(x,y, _botColor))
                {
                    board.SwitchTurnAfterMove();
                }
            }
        }
        SyncAndRefreshUI();
        if (board.IsGameOver)
            CheckGameOver();
    }

    // Helper
    private bool IsPvP => _currentMode == GameMode.PlayerVsPlayer;
    private bool IsPvBot => _currentMode == GameMode.PlayerVsBot;

    private bool IsHumanTurn
    {
        get
        {
            if(IsPvP)
                return true;

            return board.CurrentPlayer == _playerColor;
        }
    }
    // Stones when setting
    private void DrawSetupStones()
    {
        if (board == null) return;
        board.Reset();

        int mid = board.Size / 2;
        int padding = board.Size /6;

        Stone opponentColor = (_playerColor == Stone.Black) ? Stone.White : Stone.Black;

        // Top arrow
        int topTipY = padding + 2;
        board.Board[mid, topTipY] = opponentColor;
        board.Board[mid - 1, topTipY-1] = opponentColor;
        board.Board[mid + 1, topTipY-1] = opponentColor;
        board.Board[mid - 2, topTipY - 2] = opponentColor;
        board.Board[mid + 2, topTipY - 2] = opponentColor;

        // Bottom arrow
        int bottomTipY = (board.Size - 1) - padding - 2;
        board.Board[mid, bottomTipY] = _playerColor;
        board.Board[mid - 1, bottomTipY + 1] = _playerColor;
        board.Board[mid + 1, bottomTipY + 1]= _playerColor;
        board.Board[mid - 2, bottomTipY + 2] = _playerColor;
        board.Board[mid + 2, bottomTipY + 2] = _playerColor;     

        DrawBoard();
    }

    private async Task RunFinalAnalysisAsync()
    {
        await _botService.StartOwnershipAnalysisAsync(board.Size);

        for (int i = 0; i < 50; i++)
        {
            if (_botService.LastOwnership != null)
            {
                Debug.WriteLine("Data retrieved!");
                break;
            }

            await Task.Delay(100);
        }

        await _botService.StopAnalysisAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_botService.LastOwnership != null)
            {
                DrawBoard();
                DrawTerritoryOverlay(_botService.LastOwnership);
                Debug.WriteLine("The overlay has been rendered!");
            }
            else
            {
                Debug.WriteLine("Error: Failed to get ownership data (LastOwnership is null).");
            }
        });
    }
}
