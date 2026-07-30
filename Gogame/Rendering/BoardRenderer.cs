using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Gogame.Models;
using System;
using System.Globalization;
using System.Collections.Generic;
using static Gogame.Models.GoGame;

namespace Gogame.Rendering;

public class BoardRenderer
{
    private readonly Canvas _canvas;
    private readonly double _boardSizePx;
    private readonly double _margin;
    private readonly Random _rng = new();

    private Ellipse? _ghostStone;

    public int GhostX { get; private set; } = -1;
    public int GhostY { get; private set; } = -1;

    public BoardRenderer(Canvas canvas, double boardSizePx = 800, double margin = 30)
    {
        _canvas = canvas;
        _boardSizePx = boardSizePx;
        _margin = margin;
    }

    private double CellSize(int boardSize) => (_boardSizePx - 2.0 * _margin) / (boardSize - 1);

    // Board + stones
    public void DrawBoard(GoBoard board)
    {
        _canvas.Children.Clear();

        int size = board.Size;
        double cell = CellSize(size);

        // Grid
        for (int i = 0; i < size; i++)
        {
            double pos = _margin + i * cell;

            _canvas.Children.Add(new Line
            {
                StartPoint = new Point(_margin, pos),
                EndPoint = new Point(_boardSizePx - _margin, pos),
                Stroke = Brushes.Black
            });

            _canvas.Children.Add(new Line
            {
                StartPoint = new Point(pos, _margin),
                EndPoint = new Point(pos, _boardSizePx - _margin),
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

                DrawStone(x, y, board.Board[x, y] == Stone.Black, cell);
            }
        }
    }

    private void DrawStone(int x, int y, bool isBlack, double cell)
    {
        double stoneSize = cell * 0.85;
        double xPos = _margin + x * cell - stoneSize / 2;
        double yPos = _margin + y * cell - stoneSize / 2;

        double lightX = 0.25 + _rng.NextDouble() * 0.15;
        double lightY = 0.20 + _rng.NextDouble() * 0.15;

        var baseShadow = new Ellipse
        {
            Width = cell * 0.88,
            Height = cell * 0.88,
            Fill = new SolidColorBrush(isBlack ? Color.FromArgb(90, 0, 0, 0) : Color.FromArgb(70, 0, 0, 0)),
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
                new GradientStop(Color.Parse("#FAFAFA"), 0),
                new GradientStop(Color.Parse("#EDEDED"), 0.45),
                new GradientStop(Color.Parse("#D8D8D8"), 0.75),
                new GradientStop(Color.Parse("#BEBEBE"), 1)
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
                    new GradientStop(Color.FromArgb(55, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(15, 255, 255, 255), 0.4),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
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
            Stroke = new SolidColorBrush(isBlack ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(60, 0, 0, 0)),
            StrokeThickness = stoneSize * 0.015,
            IsHitTestVisible = false,
            Tag = $"stone_{x}_{y}"
        };
        Canvas.SetLeft(stone, xPos);
        Canvas.SetTop(stone, yPos);

        _canvas.Children.Add(baseShadow);
        _canvas.Children.Add(stone);
        _canvas.Children.Add(isBlack ? blackHighlight : whiteHighlight);
    }

    // Hit testing
    public bool TryGetBoardCoordinates(Point pos, GoBoard board, out int x, out int y)
    {
        double cell = CellSize(board.Size);

        x = (int)Math.Round((pos.X - _margin) / cell);
        y = (int)Math.Round((pos.Y - _margin) / cell);

        return board.IsOnBoard(x, y);
    }

    // Ghost (hover) stone
    public void ShowGhostStone(GoBoard board, int x, int y, Stone playerToShow)
    {
        if (x == GhostX && y == GhostY)
            return;

        RemoveGhostStone();

        double cell = CellSize(board.Size);

        _ghostStone = new Ellipse
        {
            Width = cell * 0.8,
            Height = cell * 0.8,
            Fill = playerToShow == Stone.Black ? Brushes.Black : Brushes.White,
            Opacity = 0.4,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(_ghostStone, _margin + x * cell - _ghostStone.Width / 2);
        Canvas.SetTop(_ghostStone, _margin + y * cell - _ghostStone.Width / 2);

        _canvas.Children.Add(_ghostStone);
        GhostX = x;
        GhostY = y;
    }

    public void RemoveGhostStone()
    {
        if (_ghostStone != null)
        {
            _canvas.Children.Remove(_ghostStone);
            _ghostStone = null;
        }
        GhostX = GhostY = -1;
    }

    // Territory overlay
    public void DrawTerritoryOverlay(GoBoard board, double[] ownership)
    {
        int dataSize = (int)Math.Round(Math.Sqrt(ownership.Length));
        double cell = CellSize(board.Size);

        for (int i = 0; i < ownership.Length; i++)
        {
            double val = ownership[i];
            if (Math.Abs(val) < 0.2) continue;

            int x = i % dataSize;
            int y = i / dataSize;

            Stone ownerColor = val > 0 ? Stone.Black : Stone.White;

            var rect = new Rectangle
            {
                Width = cell * 0.6,
                Height = cell * 0.6,
                Fill = ownerColor == Stone.White
                    ? new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
                    : new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Opacity = 0.35,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rect, _margin + x * cell - rect.Width / 2);
            Canvas.SetTop(rect, _margin + y * cell - rect.Height / 2);
            _canvas.Children.Add(rect);
        }
    }

    // Decorative arrows shown on the setup/lobby screen before a game starts
    public void DrawSetupStones(GoBoard board, Stone playerColor)
    {
        board.Reset();

        int mid = board.Size / 2;
        int padding = board.Size / 6;

        Stone opponentColor = (playerColor == Stone.Black) ? Stone.White : Stone.Black;

        // Top arrow
        int topTipY = padding + 2;
        board.Board[mid, topTipY] = opponentColor;
        board.Board[mid - 1, topTipY - 1] = opponentColor;
        board.Board[mid + 1, topTipY - 1] = opponentColor;
        board.Board[mid - 2, topTipY - 2] = opponentColor;
        board.Board[mid + 2, topTipY - 2] = opponentColor;

        // Bottom arrow
        int bottomTipY = (board.Size - 1) - padding - 2;
        board.Board[mid, bottomTipY] = playerColor;
        board.Board[mid - 1, bottomTipY + 1] = playerColor;
        board.Board[mid + 1, bottomTipY + 1] = playerColor;
        board.Board[mid - 2, bottomTipY + 2] = playerColor;
        board.Board[mid + 2, bottomTipY + 2] = playerColor;

        DrawBoard(board);
    }

    // Mark
    private List<Shape> _mark = new();
    public void DrawMarker(GoBoard board, List<(int x, int y)> positions)
    {
        RemoveMark();

        double cell = CellSize(board.Size);
        double size = cell * 0.4;

        foreach(var pos in positions)
        {
            double centerX = _margin + pos.x * cell;
            double centerY = _margin + pos.y * cell;

            double x1 = centerX - size;
            double x2 = centerX + size;
            double y1 = centerY - size;
            double y2 = centerY + size;

            var geometryString = string.Format(
                CultureInfo.InvariantCulture,
                "M {0},{1} L {2},{3} M {0},{3} L {2},{1}",
                x1, y1, x2, y2
            );

            var mark = new Path
            {
                Data = StreamGeometry.Parse(geometryString),
                Stroke = Brushes.Black,
                StrokeThickness = 6,
                IsHitTestVisible = false,
            };

            _canvas.Children.Add(mark);
            _mark.Add(mark);
        }
    }

    public void RemoveMark()
    {
        foreach(var marker in _mark)
        {
            _canvas.Children.Remove(marker);
        }
        _mark.Clear();
    }
}
