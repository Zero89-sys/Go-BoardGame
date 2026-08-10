using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gogame.Models;
using Gogame.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Gogame.Models.GoGame;
using static Gogame.TutorialView;

namespace Gogame;

public partial class PuzzleView : UserControl
{
    private GoBoard board = new GoBoard(19);

    public class PuzzleItem
    {
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    private List<MoveOption> _activeAllowedMoves = new();
    private bool _isWaitingForOpponent = false;
    private TutorialStep? _currentPuzzleStep;
    public ObservableCollection<PuzzleItem> PuzzleList { get; set; } = new();
    private List<string> _puzzleFiles = new();
    private int _currentPuzzleIndex = 0;

    public PuzzleView()
    {
        InitializeComponent();
        DataContext = this;

        BoardControl.BoardPointerPressed += (s, pos) => OnBoardClicked(pos);
        BoardControl.BoardPointerMoved += (s, pos) => OnBoardMoved(pos);
        BoardControl.BoardPointerLeft += (s, e) => BoardControl.RemoveGhostStone();

        LoadPuzzleDirectory();
    }

    private void LoadPuzzleDirectory()
    {
        string puzzleFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Puzzles");
        if (!Directory.Exists(puzzleFolder))
        {
            Directory.CreateDirectory(puzzleFolder);
            ShowEmptyBoard($"No puzzles found. Add .sgf files to:\n{puzzleFolder}");
            return;
        }

        var files = Directory.GetFiles(puzzleFolder, "*.sgf", SearchOption.AllDirectories);

        PuzzleList.Clear();
        _puzzleFiles.Clear();

        foreach (var filePath in files)
        {
            _puzzleFiles.Add(filePath);

            PuzzleList.Add(new PuzzleItem
            {
                Title = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
            });
        }

        if (_puzzleFiles.Count > 0)
        {
            LoadPuzzleByIndex(0);
        }
        else
        {
            ShowEmptyBoard($"No .sgf puzzle files found in:\n{puzzleFolder}");
        }
    }

    private void ShowEmptyBoard(string message)
    {
        board.Reset();
        BoardControl.DrawBoard(board);
        BoardControl.RemoveMarker();
        InstructionText.Text = message;
    }

    private void LoadPuzzleByIndex(int index)
    {
        if (index < 0 || index >= _puzzleFiles.Count)
            return;
        _currentPuzzleIndex = index;
        string selectedFile = _puzzleFiles[index];

        try
        {
            TutorialStep step = SgfParser.LoadFromFile(selectedFile);

            DisplayStep(step);
        }
        catch (Exception ex)
        {
            InstructionText.Text = $"Error loading task {Path.GetFileName(selectedFile)}: {ex.Message}";
            ShowEmptyBoard(InstructionText.Text);
        }
    }

    private void OnNexPuzzleClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentPuzzleIndex + 1 < _puzzleFiles.Count)
        {
            LoadPuzzleByIndex(_currentPuzzleIndex + 1);
        }
    }

    private void OnPuzzleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is PuzzleItem selectedItem)
        {
            int index = _puzzleFiles.IndexOf(selectedItem.FilePath);
            if (index >= 0)
            {
                LoadPuzzleByIndex(index);
            }
        }
    }

    private void DisplayStep(TutorialStep step)
    {
        if (step == null) return;

        _currentPuzzleStep = step;
        _isWaitingForOpponent = false;

        board.Reset();

        if (step.InitialStones != null)
        {
            foreach (var stone in step.InitialStones)
            {
                Stone stoneColor = stone.Color == "White" ? Stone.White : Stone.Black;
                board.PlaceStone(stone.x, stone.y, stoneColor);
            }
        }

        if (!string.IsNullOrEmpty(step.Instructions))
        {
            InstructionText.Text = step.Instructions;
        }
        else
        {
            InstructionText.Text = step.MoveColor == "White"
                ? "White to move. Find the right move!"
                : "Black to move. Find the right move!";
        }

        _activeAllowedMoves = step.AllowedMoves ?? new List<MoveOption>();

        BoardControl.DrawBoard(board);

        if (_activeAllowedMoves.Count > 0 && !step.HideMarkers)
        {
            BoardControl.DrawMarker(board, _activeAllowedMoves);
        }
        else
        {
            BoardControl.RemoveMarker();
        }
    }

    // Clicking on the board
    private async void OnBoardClicked(Point pos)
    {
        if (_currentPuzzleStep == null || _currentPuzzleStep.IsCompleted || _isWaitingForOpponent)
            return;

        if (!BoardControl.TryGetBoardCoordinates(pos, board, out int x, out int y))
            return;

        var step = _currentPuzzleStep;
        Stone playerStone = step.MoveColor == "White" ? Stone.White : Stone.Black;

        var matchedMove = _activeAllowedMoves.FirstOrDefault(m => m.x == x && m.y == y && m.Capture);

        if (matchedMove != null)
        {
            _isWaitingForOpponent = true;

            board.PlaceStone(x, y, playerStone);
            BoardControl.DrawBoard(board);
            BoardControl.RemoveMarker();

            if (matchedMove.HasOpponentResponse)
            {
                Stone opponentStone = matchedMove.OpponentColor == "White" ? Stone.White : Stone.Black;
                await Task.Delay(600);
                board.PlaceStone(matchedMove.OpponentX, matchedMove.OpponentY, opponentStone);
                BoardControl.DrawBoard(board);
            }

            if (matchedMove.NextAllowedMoves != null && matchedMove.NextAllowedMoves.Count > 0)
            {
                _activeAllowedMoves = matchedMove.NextAllowedMoves;

                if (!string.IsNullOrEmpty(matchedMove.NextInstructions))
                {
                    InstructionText.Text = matchedMove.NextInstructions;
                }

                if (!step.HideMarkers)
                {
                    BoardControl.DrawMarker(board, _activeAllowedMoves);
                }

                _isWaitingForOpponent = false;
            }
            else
            {
                step.IsCompleted = true;
                InstructionText.Text = step.ResponseMessage;
                _isWaitingForOpponent = false;
            }
        }
        else if (board.Board[x, y] == Stone.Empty)
        {
            _isWaitingForOpponent = true;

            board.PlaceStone(x, y, playerStone);
            BoardControl.DrawBoard(board);

            string previousInstructions = step.Instructions;
            InstructionText.Text = "Wrong move! Try again.";

            await Task.Delay(1000);

            InstructionText.Text = previousInstructions;
            board.Board[x, y] = Stone.Empty;
            BoardControl.DrawBoard(board);

            if (!step.HideMarkers)
            {
                BoardControl.DrawMarker(board, _activeAllowedMoves);
            }

            _isWaitingForOpponent = false;
        }
        else
        {
            BoardControl.ShowGhostStone(board, x, y, playerStone);
        }
    }

    // Hovering over the board
    private void OnBoardMoved(Point pos)
    {
        if (_currentPuzzleStep == null || _currentPuzzleStep.IsCompleted || _isWaitingForOpponent)
        {
            BoardControl.RemoveGhostStone();
            return;
        }

        Stone playerStone = _currentPuzzleStep.MoveColor == "White" ? Stone.White : Stone.Black;

        if (!_currentPuzzleStep.ShowGhost)
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

        BoardControl.ShowGhostStone(board, x, y, playerStone);
    }
    // Toggle for menu
    private void OnTogglePane(object? sender, RoutedEventArgs e)
    {
        PuzzleSplitView.IsPaneOpen = !PuzzleSplitView.IsPaneOpen;
    }

    // Back to menu button
    private void OnBackToMenu(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new MenuView());
}
