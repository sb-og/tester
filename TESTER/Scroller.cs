using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TESTER
{
    public class Scroller
    {
        private readonly Window _window;

        public Scroller(Window window)
        {
            _window = window;
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
                    _window.DragMove();
                }
            }
        }

        private bool IsInScrollArea(Point point)
        {
            double scrollAreaHeight = 23; // Ustaw wysokość obszaru przewijania (pasek tytułowy)
            double windowHeight = _window.ActualHeight;

            return point.Y <= scrollAreaHeight || point.Y >= windowHeight - scrollAreaHeight;
        }
    }
}