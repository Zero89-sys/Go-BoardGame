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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Threading.Tasks;
using static Gogame.Models.GoGame;
using static Gogame.Views.GameView;
using FilePath = System.IO.Path;

namespace Gogame.Views;

public partial class GameView : UserControl
{
    private GoBoard board = new GoBoard(19);

    private TextBlock? Turn_Text;
    private TextBlock? WhiteCapture_Text;
    private TextBlock? BlackCapture_Text;

    private Button? WhiteResign_Button;
    private Button? BlackResign_Button;

    private GtpService _botService = GtpService.Instance;
    private bool _isBotThinking = false;
    private bool _isBotReady = false;

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

    private int _currentLevel = 1;

    private static readonly Random _rnd = new Random();

    public GameView()
    {
        InitializeComponent();

        if (BotOnlyPanel != null)
        {
            BotOnlyPanel.IsVisible = (_currentMode == GameMode.PlayerVsBot);
        }

        Turn_Text = this.FindControl<TextBlock>("TurnText");
        WhiteCapture_Text = this.FindControl<TextBlock>("WhiteCaptureText");
        BlackCapture_Text = this.FindControl<TextBlock>("BlackCaptureText");

        WhiteResign_Button = this.FindControl<Button>("WhiteResignButton");
        BlackResign_Button = this.FindControl<Button>("BlackResignButton");

        BoardControl.BoardPointerPressed += (s, pos) => OnBoardPointerPressed(pos);
        BoardControl.BoardPointerMoved += (s, pos) => OnBoardPointerMoved(pos);
        BoardControl.BoardPointerLeft += (s, e) => BoardControl.RemoveGhostStone();

        Avalonia.Threading.Dispatcher.UIThread.Post(() => BoardControl.DrawSetupStones(board, _playerColor),
        Avalonia.Threading.DispatcherPriority.Background);

        UpdatePassVisibility();
        UpdateTurnText();
        UpdateCaptureText();
        UpdateButtons();
        BoardControl.DrawBoard(board);
    }

    // Mouse click
    private async void OnBoardPointerPressed(Point pos)
    {
        if (_gameState != GameState.Playing || _isBotThinking)
            return;

        if (IsPvBot && board.CurrentPlayer != _playerColor)
            return;

        if (!BoardControl.TryGetBoardCoordinates(pos, board, out int x, out int y))
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
                string move;

                if (_currentLevel <= 3 && _rnd.Next(0, 100) < 20)
                {
                    move = GetRandomLegalMove();

                    string blunderResponse = await _botService.SendCommand($"play {botColorStr} {move}");
                    if (blunderResponse == null || !blunderResponse.Trim().StartsWith("="))
                    {
                        string response = await _botService.SendCommand($"genmove {botColorStr}");
                        move = response.Replace("=", "").Trim().ToLower();
                    }
                }
                else
                {
                    string response = await _botService.SendCommand($"genmove {botColorStr}");
                    move = response.Replace("=", "").Trim().ToLower();
                }

                ApplyBotMove($"= {move}");
            }
            finally
            {
                _isBotThinking = false;
                SyncAndRefreshUI();
                if (board.IsGameOver) ShowGameResult();
            }
        }
    }

    // Ghost stone
    private void OnBoardPointerMoved(Point pos)
    {
        if (_currentMode == GameMode.PlayerVsBot && board.CurrentPlayer != _playerColor)
        {
            BoardControl.RemoveGhostStone();
            return;
        }
        if (_gameState != GameState.Playing)
        {
            BoardControl.RemoveGhostStone();
            return;
        }

        if (!BoardControl.TryGetBoardCoordinates(pos, board, out int x, out int y))
        {
            BoardControl.RemoveGhostStone();
            return;
        }
        if (board.Board[x, y] != Stone.Empty)
        {
            BoardControl.RemoveGhostStone();
            return;
        }

        BoardControl.ShowGhostStone(board, x, y, board.CurrentPlayer);
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
        var winner = board.Resign(board.CurrentPlayer);
        if (winner.HasValue) ShowGameResult(winner.Value);
    }

    public (double blackTerritory, double whiteTerritory) CalculateTerritory(double[] ownership)
    {
        double black = 0;
        double white = 0;

        for (int x = 0; x < board.Size; x++)
        {
            for (int y = 0; y < board.Size; y++)
            {
                int idx = y * board.Size + x;
                var val = ownership[idx];
                Stone stone = board.Board[x, y];

                if (stone == Stone.Empty)
                {
                    if (val > 0.5) black++;
                    else if (val < -0.5) white++;
                }
                else if (stone == Stone.Black && val < -0.5)
                {
                    white++;
                }
                else if (stone == Stone.White && val > 0.5)
                {
                    black++;
                }
            }
        }
        return (black, white);
    }

    // Display the result
    private async void ShowGameResult(Stone? resignWinner = null, string? botScore = null)
    {
        if (_gameState == GameState.GameOver && resignWinner == null && botScore == null)
            return;

        _gameState = GameState.GameOver;
        BoardControl.IsInteractive = false;
        await RunFinalAnalysisAsync();

        double blackTerritory = 0;
        double whiteTerritory = 0;

        if (_botService.LastOwnership != null)
        {
            var result = CalculateTerritory(_botService.LastOwnership);
            blackTerritory = result.blackTerritory;
            whiteTerritory = result.whiteTerritory;
        }
        double komi = 6.5;
        double blackFinal = blackTerritory + board.BlackCaptures;
        double whiteFinal = whiteTerritory + board.WhiteCaptures + komi;

        Stone winningColor = resignWinner ?? (blackFinal > whiteFinal ? Stone.Black : Stone.White);

        string winReason = resignWinner.HasValue
            ? "by opponent's resignation"
            : $"by {Math.Abs(blackFinal - whiteFinal):F1} points";

        string winnerText;
        if (_currentMode == GameMode.PlayerVsPlayer)
        {
            winnerText = $"{winningColor} wins {winReason}";
        }
        else
        {
            string who = winningColor == _playerColor ? "You" : "Bot";
            winnerText = $"{who} win {winReason}";
        }

        string statsText =
            "Stats:\n" +
            $"BLACK:\n" +
            $"Territory: {blackTerritory}\n" +
            $"Captures: {board.BlackCaptures}\n" +
            $"Total: {blackFinal}\n\n" +
            $"WHITE:\n" +
            $"Territory: {whiteTerritory}\n" +
            $"Captures: {board.WhiteCaptures}\n" +
            $"Komi: {komi}\n" +
            $"Total: {whiteFinal}";

        string resultText = $"{winnerText}\n{statsText}";

        var dialog = new GameResultDialog();
        dialog.ResultText.Text = resultText;

        dialog.RestartRequested += async (s, e) =>
        {
            await RestartGameAsync();
        };

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
        BoardControl.IsInteractive = true;
        BoardControl.RemoveGhostStone();
        await _botService.SendRawCommand("stop");
        await _botService.SendRawCommand("clear_board");
        _botService.ClearAnalysisState();

        board = new GoBoard(_selectedBoardSize);

        _gameState = GameState.Playing;
        _isBotThinking = false;

        BoardControl.DrawBoard(board);
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

            if (_botColor == Stone.Black)
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
            BoardControl.DrawBoard(board);
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
            BoardControl.DrawSetupStones(board, _playerColor);
        }
        else
        {
            BoardControl.DrawBoard(board);
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
                BoardControl.DrawSetupStones(board, _playerColor);
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
        BoardControl.IsInteractive = true;

        board = new GoBoard(_selectedBoardSize);
        board.Reset();
        _botService.ClearAnalysisState();
        board.Size = _selectedBoardSize;


        TurnText.IsVisible = true;
        var whiteRow = this.FindControl<Grid>("WhiteInfoRow");
        var blackRow = this.FindControl<Grid>("BlackInfoRow");
        if (whiteRow != null) whiteRow.IsVisible = true;
        if (blackRow != null) blackRow.IsVisible = true;
        var container = WhiteInfoRow.Parent as StackPanel;

        BoardControl.DrawBoard(board);

        if (_currentMode == GameMode.PlayerVsBot)
        {
            await _botService.SendCommand($"boardsize {_selectedBoardSize}");
            await _botService.SendCommand("clear_board");
            await _botService.SendCommand("komi 6.5");

            container.Children.Remove(WhiteInfoRow);
            container.Children.Remove(BlackInfoRow);

            if (_botColor == Stone.Black)
            {
                _isBotThinking = true;
                string response = await _botService.SendCommand("genmove B");
                ApplyBotMove(response);
                _isBotThinking = false;
                container.Children.Insert(2, BlackInfoRow);
                container.Children.Add(WhiteInfoRow);
                WhiteInfoRow.FlowDirection = FlowDirection.RightToLeft;
                BlackInfoRow.FlowDirection = FlowDirection.RightToLeft;
            }
            else
            {
                container.Children.Insert(2, WhiteInfoRow);
                container.Children.Add(BlackInfoRow);
            }
        }
        SyncAndRefreshUI();
        UpdatePassVisibility();
    }

    // Coordinates for Engine
    private string ConvertToGtpCoords(int x, int y)
    {
        char col = (char)('a' + x);
        if (col >= 'i') col++;
        int row = board.Size - y;
        return $"{col}{row}";
    }

    private (int x, int y) ConvertFromGtpCoords(string gtp)
    {
        gtp = gtp.Replace("=", "").Trim().ToLower();

        if (string.IsNullOrEmpty(gtp) || gtp == "pass" || gtp == "resign")
            return (-1, -1);

        char colChar = gtp[0];
        int targetX = colChar - 'a';
        if (colChar > 'i') targetX--;

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
                    Debug.WriteLine($"Error when starting the bot: {ex.Message}");
                }
                finally
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (BotLoadingOverlay != null)
                            BotLoadingOverlay.IsVisible = false;
                        Debug.WriteLine("The bot is ready to play!");
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
    public async Task SetBotDifficulty(int level)
    {
        _currentLevel = level;

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

        double entropy = level <= 5 ? 0.8 : 0.1;
        double exploration = level <= 5 ? 5.0 : 1.0;

        double temperature = level switch
        {
            1 => 4.0,
            2 => 3.8,
            3 => 3.5,
            4 => 3.0,
            5 => 2.8,
            6 => 2.3,
            7 => 2,
            8 => 1.5,
            9 => 1.3,
            10 => 1.1,
            11 => 1.0,
            _ => 3.0
        };

        bool enableNoise = level <= 7;
        double noise = level switch
        {
            1 => 0.4,
            2 => 0.35,
            3 => 0.3,
            4 => 0.25,
            5 => 0.2,
            6 => 0.15,
            7 => 0.1,
            8 => 0.05,
            9 => 0.03,
            10 => 0.01,
            11 => 0.0,
            _ => 0.4
        };

        if (level <= 3)
        {
            await _botService.SendCommand("kata-set-param ignoreAllHistory true");
            await _botService.SendCommand("kata-set-param ignorePreRootHistory true");
            await _botService.SendCommand("kata-set-param fpuParentWeight 0.0");
            await _botService.SendCommand("kata-set-param fpuLossProp 0.0");
            await _botService.SendCommand("kata-set-param rootPruneUselessMoves true");
            await _botService.SendCommand("kata-set-param rootPolicyOptimism 0.0");
        }

        await _botService.SendCommand($"kata-set-param maxVisits {visits}");
        await _botService.SendCommand($"kata-set-param rootPolicyTemperature {temperature}");
        await _botService.SendCommand($"kata-set-param rootNoiseEnabled {enableNoise.ToString().ToLower()}");
        await _botService.SendCommand($"kata-set-param rootNoiseWeight {noise}"); ;
        await _botService.SendCommand("kata-set-param rootFpuReductionMax 5.0");
        await _botService.SendCommand($"kata-set-param obviousMovesPolicyEntropyTolerance {entropy}");
        await _botService.SendCommand($"kata-set-param cpuctExploration {exploration}");
        await _botService.SendCommand($"kata-set-param enablePassingHacks true");
        await _botService.SendCommand($"kata-set-param allowResignation true");


        string check = await _botService.SendCommand("kata-get-param maxVisits");
        string check_temperature = await _botService.SendCommand("kata-get-param rootPolicyTemperature");
        string check_ex = await _botService.SendCommand("kata-get-param cpuctExploration");
        string check_tol = await _botService.SendCommand("kata-get-param obviousMovesPolicyEntropyTolerance");

        Debug.WriteLine($"CONFIRMED: Bot is set: {check}");
        Debug.WriteLine($"CONFIRMED: Bot's temp is set: {check_temperature}");
        Debug.WriteLine($"Bot's exploration: {check_ex}");
        Debug.WriteLine($"Bot's tolerance: {check_tol}");
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
        if (string.IsNullOrEmpty(response)) return;

        response = response.Trim();

        if (!response.StartsWith("=")) return;

        string move = response.Replace("=", "").Trim().ToLower();

        if (move == "resign")
        {
            Debug.WriteLine("Bot resigned");
            ShowGameResult(_playerColor);
            _gameState = GameState.GameOver;
            return;
        }

        if (move == "pass")
        {
            board.Pass(_botColor);

        }
        else
        {
            var (x, y) = ConvertFromGtpCoords(move);
            if (x >= 0 && y >= 0)
            {
                if (!board.PlaceStone(x, y, _botColor))
                {
                    Debug.WriteLine($"WARNING: local board rejected engine move {move} at ({x},{y}) — board desync!");
                    SyncAndRefreshUI();
                    return;
                }
            }
        }

        board.SwitchTurnAfterMove();
        SyncAndRefreshUI();
        if (board.IsGameOver) CheckGameOver();
    }

    // Randomizer for engine's moves
    private string GetRandomLegalMove()
    {
        var legalPoints = new List<string>();

        for (int x = 0; x < board.Size; x++)
        {
            for (int y = 0; y < board.Size; y++)
            {
                if (board.IsLegalMove(x, y, _botColor))
                {
                    legalPoints.Add(ConvertToGtpCoords(x, y));
                }
            }
        }
        if (legalPoints.Count == 0) return "pass";
        return legalPoints[_rnd.Next(legalPoints.Count)];
    }

    // Helper
    private bool IsPvBot => _currentMode == GameMode.PlayerVsBot;

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
                BoardControl.DrawBoard(board);
                BoardControl.DrawTerritoryOverlay(board, _botService.LastOwnership);
                Debug.WriteLine("The overlay has been rendered!");
            }
            else
            {
                Debug.WriteLine("Error: Failed to get ownership data (LastOwnership is null).");
            }
        });
    }
}
