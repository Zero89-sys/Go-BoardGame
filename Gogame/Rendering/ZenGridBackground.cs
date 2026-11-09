using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Gogame.Rendering
{
    public class ZenGridBackground : UserControl
    {
        // Visual values
        private const double Spacing = 40;
        private const double InfluenceRadius = 170;
        private const double MaxPull = 9;
        private const double Follow = 0.14;

        private static readonly Color IdleColor = Colors.White;
        private static readonly Color GlowColor = Color.Parse("#D9C7A0");

        private readonly DispatcherTimer _timer;
        private Point _target = new Point(-10000, -10000);
        private Point _smooth = new Point(-10000, -10000);
        private double _intensity;
        private bool _inside;
        private TopLevel? _topLevel;

        public ZenGridBackground()
        {
            IsHitTestVisible = false;
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, OnTick);
        }
        // Registers mouse pointer event listeners
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _topLevel = TopLevel.GetTopLevel(this);
            _topLevel?.AddHandler(PointerMovedEvent, OnPointerMoved,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            _topLevel?.AddHandler(PointerExitedEvent, OnPointerExited,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        }

        // Stops the timer and removes mouse event registration.
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _timer.Stop();
            _topLevel?.RemoveHandler(PointerMovedEvent, OnPointerMoved);
            _topLevel?.RemoveHandler(PointerExitedEvent, OnPointerExited);
            _topLevel = null;
        }
        // Updates the target cursor position
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            _target = e.GetPosition(this);
            if (!_inside)
            {
                _inside = true;
                _smooth = _target;
            }
            if (!_timer.IsEnabled) _timer.Start();
        }
        // Detects the cursor leaving the window
        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            _inside = false;
            if (!_timer.IsEnabled) _timer.Start();
        }

        // Draws a grid of points onto the canvas
        private void OnTick(object? sender, EventArgs e)
        {
            _smooth = new Point(_smooth.X + (_target.X - _smooth.X)*Follow,
                _smooth.Y + (_target.Y - _smooth.Y) * Follow);

            var goal = _inside ? 1.0 : 0.0;
            _intensity += (goal - _intensity) * 0.1;

            InvalidateVisual();

            // CPU background optimalization
            var settled = Math.Abs(_target.X - _smooth.X) < 0.2
                && Math.Abs(_target.Y - _smooth.Y) < 0.2
                && Math.Abs(goal - _intensity) < 0.005;

            if (settled)
            {
                _intensity = goal;
                _timer.Stop();
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext ctx)
        {
            var idleBrush = new SolidColorBrush(IdleColor, 0.06);

            for(var y = Spacing /2; y < Bounds.Height; y += Spacing)
            {
                for(var x = Spacing/2; x < Bounds.Width; x += Spacing)
                {
                    var dx = _smooth.X - x;
                    var dy = _smooth.Y - y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);

                    if(_intensity < 0.01 || dist >= InfluenceRadius)
                    {
                        ctx.DrawEllipse(idleBrush, null, new Point(x, y), 1, 1);
                        continue;
                    }

                    var t = 1 - dist / InfluenceRadius;
                    t = t * t * _intensity;

                    var pull = t * MaxPull;
                    var px = dist > 0.001 ? x + dx / dist * pull : x;
                    var py = dist > 0.001 ? y + dy / dist * pull : y;

                    var color = Lerp(IdleColor, GlowColor, t);
                    var brush = new SolidColorBrush(color, 0.06 + 0.7 * t);
                    var r = 1 + 1.8 * t;

                    ctx.DrawEllipse(brush, null, new Point(px, py), r, r);
                }
            }
        }

        // Creates a color gradient
        private static Color Lerp(Color a, Color b, double t) => Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }
}
