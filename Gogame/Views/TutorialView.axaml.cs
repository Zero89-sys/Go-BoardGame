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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Gogame.Models.GoGame;

namespace Gogame;

public partial class TutorialView : UserControl
{
    public class MoveOption
    {
        public int x { get; set; }
        public int y { get; set; }
        public string Color { get; set; } = "Black";
        public bool Capture { get; set; } = true;
        public bool HasOpponentResponse { get; set; } = false;
        public int OpponentX { get; set; }
        public int OpponentY { get; set; }
        public string OpponentColor { get; set; } = "White";
        public List<MoveOption> NextAllowedMoves { get; set; } = new();
        public string? NextInstructions { get; set; }
        public MoveOption(){ }
        public MoveOption(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public MoveOption(int x, int y, string color, bool capture)
        {
            this.x = x;
            this.y = y;
            Color = color;
            Capture = capture;
        }

        [JsonIgnore]
        public IBrush AvaloniaColor => Color switch
        {
            "Red" => Brushes.Red,
            "Blue" => Brushes.Blue,
            "White" => Brushes.White,
            _ => Brushes.Black
        };
    }

    public class InitialStone
    {
        public int x { get; set; }
        public int y { get; set; }
        public string Color { get; set; } = "Black";
    }

    private GoBoard board = new GoBoard(9);
    public class TutorialStep
    {
        public string Instructions { get; set; } = "";
        public string ResponseMessage { get; set; } = "";
        public List<MoveOption> AllowedMoves { get; set; } = new();
        public bool AllowAnyMove { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public string MoveColor { get; set; } = "Black";
        public bool ShowGhost { get; set; } = true;
        public bool HideMarkers { get; set; } = false;

        public List<InitialStone> InitialStones { get; set; } = new();

        public bool HasOpponentResponse { get; set; } = false;
        public int OpponentX { get; set; }
        public int OpponentY { get; set; }
        public string OpponentColor { get; set; } = "White";
    }

    public class TutorialSectionData
    {
        public string SectionName { get; set; } = "";
        public List<TutorialStep> Steps { get; set; } = new();
    }

    private readonly Dictionary<string, List<TutorialStep>> _sections = new();

    private string _currentSection = string.Empty;
    private int _stepIndex = 0;

    private List<MoveOption> _activeAllowedMoves = new();
    private bool _isWaitingForOpponent = false;
    public TutorialView()
    {
        InitializeComponent();
        DataContext = new TutorialViewModel();

        BoardControl.BoardPointerPressed += (s, pos) => OnBoardClicked(pos);
        BoardControl.BoardPointerLeft += (s, e) => BoardControl.RemoveGhostStone();
        BoardControl.BoardPointerMoved += (s, pos) => OnBoardMoved(pos);

        LoadSectionFromJson("Assets/Tutorial/basics.json");
    }
    //Show step
    private void ShowStep()
    {
        if (string.IsNullOrEmpty(_currentSection) || !_sections.ContainsKey(_currentSection) || _sections[_currentSection].Count == 0)
            return;

        var step = _sections[_currentSection][_stepIndex];
        InstructionText.Text = step.Instructions;
        PreviousButton.IsEnabled = _stepIndex > 0;
        NextButton.IsEnabled = _stepIndex < _sections[_currentSection].Count - 1;

        board.Reset();
        step.IsCompleted = false;
        _isWaitingForOpponent = false;

        foreach(var stone in step.InitialStones)
        {
            Stone stoneEnum = stone.Color == "Black" ? Stone.Black : Stone.White;
            board.Board[stone.x, stone.y] = stoneEnum;
        }

        BoardControl.DrawBoard(board);

        _activeAllowedMoves = step.AllowedMoves ?? new();

        if (step.AllowedMoves != null && step.AllowedMoves.Count > 0 && !step.HideMarkers)
        {
            BoardControl.DrawMarker(board, step.AllowedMoves);
        }
        else
        {
            BoardControl.RemoveMarker();
        }
    }

    // Clicking on board
    private async void OnBoardClicked(Point pos)
    {
        if (string.IsNullOrEmpty(_currentSection) || !_sections.ContainsKey(_currentSection))
            return;

        var step = _sections[_currentSection][_stepIndex];
        if (step == null || step.IsCompleted || _isWaitingForOpponent)
            return;
        if (!BoardControl.TryGetBoardCoordinates(pos, board, out int x, out int y))
            return;

        Stone playerStone = step.MoveColor == "White" ? Stone.White : Stone.Black;
        bool moveValid = false;
        MoveOption? matchedMove = null;

        if (step.AllowAnyMove)
        {
            if (board.Board[x, y] == Stone.Empty)
            {
                moveValid = true;
            }
        }
        else if(step.AllowedMoves != null)
        {
            matchedMove = _activeAllowedMoves.FirstOrDefault(m => m.x == x && m.y == y && m.Capture);
            
            if(matchedMove != null)
            {
                moveValid = true;
            }
        }
        BoardControl.ShowGhostStone(board, x, y, playerStone);

        if (moveValid)
        {
            _isWaitingForOpponent = true;

            board.PlaceStone(x, y, playerStone);
            BoardControl.DrawBoard(board);
            BoardControl.RemoveMarker();

            bool hasResponse = false;
            int oppX = 0, oppY = 0;
            string oppColor = "White";

            if(matchedMove != null && matchedMove.HasOpponentResponse)
            {
                hasResponse = true;
                oppX = matchedMove.OpponentX;
                oppY = matchedMove.OpponentY;
                oppColor = matchedMove.OpponentColor;
            }else if (step.HasOpponentResponse)
            {
                hasResponse = true;
                oppX = step.OpponentX;
                oppY = step.OpponentY;
                oppColor = step.OpponentColor;
            }

            if (hasResponse)
            {
                Stone opponentStone = oppColor == "White" ? Stone.White : Stone.Black;
                await Task.Delay(600);
                board.PlaceStone(oppX, oppY, opponentStone);
                BoardControl.DrawBoard(board);
            }

            if(matchedMove != null && matchedMove.NextAllowedMoves != null && matchedMove.NextAllowedMoves.Count > 0)
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
        else
        {
            if (board.Board[x, y] == Stone.Empty)
            {
                _isWaitingForOpponent = true;

                board.PlaceStone(x, y, playerStone);
                BoardControl.DrawBoard(board);

                InstructionText.Text = "Wrong move! Try again.";

                await Task.Delay(1000);
                
                InstructionText.Text = step.Instructions;
                board.Board[x, y] = Stone.Empty;
                BoardControl.DrawBoard(board);

                _isWaitingForOpponent = false;
            }
            else
            {
                BoardControl.ShowGhostStone(board, x, y, playerStone);
            }
        }
    }

    // Mouse move
    private void OnBoardMoved(Point pos)
    {
        if (string.IsNullOrEmpty(_currentSection) || !_sections.ContainsKey(_currentSection))
            return;

        var step = _sections[_currentSection][_stepIndex];
        Stone playerStone = step.MoveColor == "White" ? Stone.White : Stone.Black;
        if (!step.ShowGhost)
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

    private void OnPreviousStep(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSection) || !_sections.ContainsKey(_currentSection)) return;
        if (_stepIndex > 0)
        {
            _stepIndex--;
                        board.Reset();
            BoardControl.DrawBoard(board);
            ShowStep();
        }
    }
    private void OnNextStep(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSection) || !_sections.ContainsKey(_currentSection)) return;

        if (_stepIndex < _sections[_currentSection].Count - 1)
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
        string filePath = $"Assets/Tutorial/{sectionName.ToLower()}.json";
        _currentSection = sectionName;
        _stepIndex = 0;
        LoadSectionFromJson(filePath);
        ShowStep();
    }
    // Sections
    private void OnSectionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if(SectiontListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string sectionName)
        {
            LoadSection(sectionName);
        }
    }

    // Tutorial
    public void LoadSectionFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            InstructionText.Text = $"Error: File not found.\nSearched in: {Path.GetFullPath(filePath)}";
            return;
        }

        string jsonString = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var sectionData = JsonSerializer.Deserialize<TutorialSectionData>(jsonString, options);

        if (sectionData != null && sectionData.Steps.Count > 0)
        {
            _sections[sectionData.SectionName] = sectionData.Steps;
            _currentSection = sectionData.SectionName;
            _stepIndex = 0;
            ShowStep();
        }
        else
        {
            InstructionText.Text = "Error: The file was found, but no steps could be loaded from it. Check the JSON structure.";
        }
    }

    // Back to menu button
    private void OnBackToMenu(object? sender, RoutedEventArgs e)
    => Navigator.NavigateTo(new MenuView());
}