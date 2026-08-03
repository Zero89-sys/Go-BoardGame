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
using static Gogame.Models.GoGame;
using static Gogame.TutorialView;

namespace Gogame;

public partial class PuzzleView : UserControl
{
    private GoBoard board = new GoBoard();

    public class PuzzleItem
    {
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    private List<MoveOption> _activeAllowedMoves = new();
    private bool _isWatingForOpponent = false;
    private TutorialStep _currentPuzzleStep;
    public ObservableCollection<PuzzleItem> PuzzleList { get; set; } = new();
    private List<string> _puzzleFiles = new();
    private int _currentPuzzleIndex = 0;

    public PuzzleView()
    {
        InitializeComponent();
        DataContext = this;

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
        _isWatingForOpponent = false;

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
    // Toggle
    private void OnTogglePane(object? sender, RoutedEventArgs e)
    {
        PuzzleSplitView.IsPaneOpen = !PuzzleSplitView.IsPaneOpen;
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
}
