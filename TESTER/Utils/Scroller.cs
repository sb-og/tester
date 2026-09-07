using System;
using System.Windows;
using System.Windows.Input;

namespace TESTER.Utils
{
    public class Scroller
    {
        private readonly Window _window;
        private readonly Func<Point, bool>? _restoreMaximizedForDrag;
        private readonly Action? _dragCompleted;

        public Scroller(Window window, Func<Point, bool>? restoreMaximizedForDrag = null, Action? dragCompleted = null)
        {
            _window = window;
            _restoreMaximizedForDrag = restoreMaximizedForDrag;
            _dragCompleted = dragCompleted;
            Initialize();
        }

        private void Initialize()
        {
            _window.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsInScrollArea(e.GetPosition(_window)))
                {
                    Point mousePosition = e.GetPosition(_window);
                    if (_restoreMaximizedForDrag?.Invoke(mousePosition) != true && _window.WindowState == WindowState.Maximized)
                    {
                        RestoreWindowForDrag(mousePosition);
                    }

                    try
                    {
                        _window.DragMove();
                    }
                    finally
                    {
                        _dragCompleted?.Invoke();
                    }
                }
            }
        }

        private void RestoreWindowForDrag(Point mousePosition)
        {
            double horizontalRatio = _window.ActualWidth > 0 ? mousePosition.X / _window.ActualWidth : 0.5;
            Point screenPosition = _window.PointToScreen(mousePosition);
            double restoredWidth = _window.RestoreBounds.Width > _window.MinWidth ? _window.RestoreBounds.Width : _window.MinWidth;

            _window.WindowState = WindowState.Normal;
            _window.Left = screenPosition.X - restoredWidth * horizontalRatio;
            _window.Top = SystemParameters.WorkArea.Top;
        }

        private bool IsInScrollArea(Point point)
        {
            double scrollAreaHeight = 24; // Ustaw wysokość obszaru przewijania (pasek tytułowy)
            double windowHeight = _window.ActualHeight;

            return point.Y <= scrollAreaHeight || point.Y >= windowHeight - scrollAreaHeight;
        }
    }
}