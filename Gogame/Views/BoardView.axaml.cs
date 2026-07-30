using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Gogame.Models;
using Gogame.Rendering;
using System;
using System.Collections.Generic;
using static Gogame.Models.GoGame;
using static Gogame.TutorialView;

namespace Gogame.Views;

public partial class BoardView : UserControl
{
    private BoardRenderer _renderer;

    public BoardView()
    {
        InitializeComponent();
        _renderer = new BoardRenderer(BoardCanvas);
    }

    public event EventHandler<Point>? BoardPointerPressed;
    public event EventHandler<Point>? BoardPointerMoved;
    public event EventHandler? BoardPointerLeft;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BoardPointerPressed?.Invoke(this, e.GetPosition(BoardCanvas));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        BoardPointerMoved?.Invoke(this, e.GetPosition(BoardCanvas));
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        RemoveGhostStone();
        BoardPointerLeft?.Invoke(this, EventArgs.Empty);
    }

    public bool TryGetBoardCoordinates(Point pos, GoBoard board, out int x, out int y) =>
        _renderer.TryGetBoardCoordinates(pos, board, out x, out y);

    public void DrawBoard(GoBoard board) => _renderer.DrawBoard(board);

    public void DrawSetupStones(GoBoard board, Stone playerColor) =>
        _renderer.DrawSetupStones(board, playerColor);

    public void DrawTerritoryOverlay(GoBoard board, double[] ownership) =>
        _renderer.DrawTerritoryOverlay(board, ownership);

    public void ShowGhostStone(GoBoard board, int x, int y, Stone colorToShow) =>
        _renderer.ShowGhostStone(board, x, y, colorToShow);

    public void RemoveGhostStone() => _renderer.RemoveGhostStone();

    public void DrawMarker(GoBoard board, List<MoveOption> positions) => _renderer.DrawMarker(board, positions);
    public void RemoveMarker() => _renderer.RemoveMark();

    public bool IsInteractive
    {
        get => BoardCanvas.IsHitTestVisible;
        set => BoardCanvas.IsHitTestVisible = value;
    }
}
