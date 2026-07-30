using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gogame.Rendering;
using Gogame.ViewModels;
using Gogame.Views;
using System.Collections.Generic;
using static Gogame.Models.GoGame;

namespace Gogame;

public partial class TutorialView : UserControl
{
    private GoBoard board = new GoBoard(9);
    private class TutorialStep
    {
        public string Instructions = "";
        public string ResponseMessage = "";
        public (int x, int y)? RequiredMove;
        public Stone MoveColor = Stone.Black;
    }

    private readonly List<TutorialStep> _steps = new()
    {
        new TutorialStep
        {
            Instructions = "This is the Go board. Click the highlighted point to place a black stone.",
            ResponseMessage = "Great! That’s how stones are placed across the board, provided there is an empty spot.\n" +
            "You can now click \"Next\" below to go to the next page, or \"Previous\" to return to the previous page.",
            RequiredMove = (4,4),
            MoveColor = Stone.Black,
        },
        new TutorialStep
        {
            Instructions = "Now try placing a white stone here.",
            ResponseMessage = "Good job",
            RequiredMove = (2, 2),
            MoveColor = Stone.White,
        },
        new TutorialStep
        {
            Instructions = "Nicely done — that's everything for now.",
            RequiredMove = null
        }
    };
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
        var step = _steps[_stepIndex];
        InstructionText.Text = _steps[_stepIndex].Instructions;
        PreviousButton.IsEnabled = _stepIndex > 0;
        NextButton.IsEnabled = _stepIndex < _steps.Count - 1;
        BoardControl.DrawBoard(board);

        if (step.RequiredMove.HasValue)
        {
            var(targetX, targetY) = step.RequiredMove.Value;
            BoardControl.DrawMarker(board, targetX, targetY);
        }
        else
        {
            BoardControl.RemoveMarker();
        }
    }

    private void OnBoardClicked(Point pos)
    {
        var step = _steps[_stepIndex];
        if (step == null)
            return;
        if (!BoardControl.TryGetBoardCoordinates(pos, board, out int x, out int y))
            return;
        if ((x, y) == step.RequiredMove.Value)
        {
            board.PlaceStone(x, y, step.MoveColor);
            BoardControl.DrawBoard(board);

            InstructionText.Text = step.ResponseMessage;
        }
        BoardControl.ShowGhostStone(board, x, y, step.MoveColor);
    }

    // Mouse move
    private void OnBoardMoved(Point pos)
    {
        var step = _steps[_stepIndex];

        if(step.RequiredMove == null)
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
        if(_stepIndex < _steps.Count - 1)
        {
            _stepIndex++;
            board.Reset();
            BoardControl.DrawBoard(board);
            ShowStep();
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