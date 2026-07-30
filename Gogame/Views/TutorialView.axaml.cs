using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gogame.Rendering;
using Gogame.ViewModels;
using Gogame.Views;
using System.Collections.Generic;
using System.Linq;
using static Gogame.Models.GoGame;

namespace Gogame;

public partial class TutorialView : UserControl
{
    public class MoveOption
    {
        public int x { get; set; }
        public int y { get; set; }
        public IBrush Color { get; set; } = Brushes.Black;
        public bool Capture { get; set; } = true;
        public MoveOption(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public MoveOption(int x, int y, IBrush color)
        {
            this.x = x;
            this.y = y;
            Color = color;
        }
        public MoveOption(int x, int y, IBrush color, bool capture)
        {
            this.x = x;
            this.y = y;
            Color = color;
            Capture = capture;
        }
    }

    private GoBoard board = new GoBoard(9);
    private class TutorialStep
    {
        public string Instructions = "";
        public string ResponseMessage = "";
        public List<MoveOption> AllowedMoves { get; set; } = new();
        public bool AllowAnyMove = false;
        public bool IsCompleted = false;
        public Stone MoveColor = Stone.Black;
        public bool ShowGhost = true;
        public List<(int x, int y, Stone color)> InitialStones = new();
    }

    private readonly Dictionary<string, List<TutorialStep>> _sections = new()
    {
        {"Basics", new List<TutorialStep>
            {
                new TutorialStep
                {
                    Instructions = "This is the Go board. Two players, Black and White, take turns placing stones on the board. Black goes first." +
                    "You can place a stone on any empty intersection, including those on the edge.",
                    ResponseMessage = "Great! That’s how stones are placed across the board.\n" +
                    "You can now click \"Next\" below to go to the next page, or \"Previous\" to return to the previous page.",
                    AllowAnyMove = true,
                    MoveColor = Stone.Black,
                },
                new TutorialStep
                {
                    Instructions = "The intersections around a stone are called liberties. Fill one liberty of a black stone.",
                    ResponseMessage = "Good job",
                    AllowedMoves = new() { new (2, 1), new (2, 3), new(3, 2), new(1, 2) },
                    MoveColor = Stone.White,
                    InitialStones = new()
                    {
                        (2, 2, Stone.Black)
                    }
                },
                new TutorialStep
                {
                    Instructions = "A stone is captured if all its liberties are occupied by enemy stones. Capture the black stone by filling its last liberty.",
                    ResponseMessage = "Nice! Now you captured black's stone",
                    AllowedMoves = new() {new(2, 3) },
                    MoveColor = Stone.White,
                    InitialStones = new()
                    {
                        (2, 1, Stone.White),
                        (1, 2, Stone.White),
                        (3, 2, Stone.White),
                        (2, 2, Stone.Black),
                    }
                },
                new TutorialStep
                {
                    Instructions = "Stones of the same color next to each other form a chain. Fill one of the liberties of the black chain.",
                    ResponseMessage = "Great job!",
                    AllowedMoves = new() {new(1,2), new(1,3), new(2, 1), new(2, 4), new(3, 1), new(3,3), new(4, 2) },
                    MoveColor = Stone.White,
                    InitialStones = new()
                    {
                        (2, 2, Stone.Black),
                        (2, 3, Stone.Black),
                        (3, 2, Stone.Black),
                    }
                },
                new TutorialStep
                {
                    Instructions = "The black chain has only one liberty left. This is called 'atari'. Capture the black chain that is in atari.",
                    ResponseMessage = "Nice capture!\nThis is everything from basic. You can now move on Eyes",
                    AllowedMoves = new(){new(4,2) },
                    MoveColor= Stone.White,
                    InitialStones = new()
                    {
                        (2, 2, Stone.Black),
                        (2, 3, Stone.Black),
                        (3, 2, Stone.Black),
                        (1, 2, Stone.White),
                        (1, 3, Stone.White),
                        (2, 1, Stone.White),
                        (2, 4, Stone.White),
                        (3, 1, Stone.White),
                        (3, 3, Stone.White)
                    }
                }
            } 
        },
        { "Eyes", new List<TutorialStep>
            {
                new TutorialStep
                {
                    Instructions = "Red point is surrounded by white stones; it is called an 'eye'. Black can not play at A (self-capture). " +
                    "Black point is also an eye, but Black can play at B and capture. Capture the white stones.",
                    ResponseMessage = "Nice capture!",
                    AllowedMoves = new(){new (3,2, Brushes.Red, false), new (4,8)},
                    MoveColor= Stone.Black,
                    InitialStones = new()
                    {
                        (2, 2, Stone.White),
                        (3, 1, Stone.White),
                        (3, 3, Stone.White),
                        (4, 2, Stone.White),

                        (2, 8, Stone.Black),
                        (3, 7, Stone.Black),
                        (4, 6, Stone.Black),
                        (5, 7, Stone.Black),
                        (6, 8, Stone.Black),
                        (3, 8, Stone.White),
                        (4, 7, Stone.White),
                        (5, 8, Stone.White)
                    }
                }
            } 
        },
        { "Territory", new List<TutorialStep>
            {
                new TutorialStep
                {
                    Instructions = "Territory",
                }
            }
        },
        { "Ko", new List<TutorialStep>
            {
                new TutorialStep
                {
                    Instructions = "Ko",
                }
            }
        }
    };

    private string _currentSection = "Basics";
    private int _stepIndex = 0;
    public TutorialView()
    {
        InitializeComponent();
        DataContext = new TutorialViewModel();

        BoardControl.BoardPointerPressed += (s, pos) => OnBoardClicked(pos);
        BoardControl.BoardPointerLeft += (s, e) => BoardControl.RemoveGhostStone();
        BoardControl.BoardPointerMoved += (s, pos) => OnBoardMoved(pos);

        ShowStep();
    }
    //Show step
    private void ShowStep()
    {
        var step = _sections[_currentSection][_stepIndex];
        InstructionText.Text = step.Instructions;
        PreviousButton.IsEnabled = _stepIndex > 0;
        NextButton.IsEnabled = _stepIndex < _sections[_currentSection].Count - 1;

        board.Reset();
        step.IsCompleted = false;

        foreach(var stone in step.InitialStones)
        {
            board.Board[stone.x, stone.y] = stone.color;
        }

        BoardControl.DrawBoard(board);

        if (step.AllowedMoves != null && step.AllowedMoves.Count > 0)
        {
            BoardControl.DrawMarker(board, step.AllowedMoves);
        }
        else
        {
            BoardControl.RemoveMarker();
        }
    }

    private void OnBoardClicked(Point pos)
    {
        var step = _sections[_currentSection][_stepIndex];
        if (step == null)
            return;
        if (step.IsCompleted)
            return;
        if (!BoardControl.TryGetBoardCoordinates(pos, board, out int x, out int y))
            return;
        if (step.AllowAnyMove)
        {
            if (board.Board[x, y] == Stone.Empty)
            {
                board.PlaceStone(x, y, step.MoveColor);
                BoardControl.DrawBoard(board);
                InstructionText.Text = step.ResponseMessage;

                step.IsCompleted = true;
            }
        }
        if(step.AllowedMoves != null && step.AllowedMoves.Any(m => m.x == x && m.y == y && m.Capture))
        {
            board.PlaceStone(x, y, step.MoveColor);
            BoardControl.DrawBoard(board);
            InstructionText.Text = step.ResponseMessage;

            step.IsCompleted = true;
        }
        BoardControl.ShowGhostStone(board, x, y, step.MoveColor);
    }

    // Mouse move
    private void OnBoardMoved(Point pos)
    {
        var step = _sections[_currentSection][_stepIndex];

        if(!step.ShowGhost)
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
        BoardControl.ShowGhostStone(board, x, y, step.MoveColor);
    }

    private void OnPreviousStep(object? sender, RoutedEventArgs e)
    {
        if(_stepIndex > 0)
        {
            _stepIndex--;
                        board.Reset();
            BoardControl.DrawBoard(board);
            ShowStep();
        }
    }
    private void OnNextStep(object? sender, RoutedEventArgs e)
    {
        if(_stepIndex < _sections[_currentSection].Count - 1)
        {
            _stepIndex++;
            board.Reset();
            BoardControl.DrawBoard(board);
            ShowStep();
        }
    }

    // Load Section
    public void LoadSection(string sectionName)
    {
        _currentSection = sectionName;
        _stepIndex = 0;
        ShowStep();
    }

    private void OnSectionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if(SectiontListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string sectionName)
        {
            LoadSection(sectionName);
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
}