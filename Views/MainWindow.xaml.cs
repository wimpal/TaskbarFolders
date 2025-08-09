using System;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;

namespace TaskbarGrouper.Views
{
    public partial class MainWindow : Window
    {
        private static GroupPopup? globalPopup;
        private static bool isCreatingPopup = false;
        private DateTime lastActivation = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = true;
            this.Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object? sender, System.EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            
            // Prevent rapid successive activations (debounce)
            var now = DateTime.Now;
            if ((now - lastActivation).TotalMilliseconds < 500)
                return;
            lastActivation = now;
            
            // Toggle behavior: if popup is open, close it
            if (globalPopup != null && globalPopup.IsVisible && !isCreatingPopup)
            {
                globalPopup.Close();
                return;
            }
            
            // Prevent multiple popups
            if (isCreatingPopup) 
                return;
            
            isCreatingPopup = true;
            
            // Close any existing popup
            globalPopup?.Close();
            
            // Create and position new popup
            globalPopup = new GroupPopup();
            globalPopup.Owner = this;
            globalPopup.ShowInTaskbar = false;
            globalPopup.Closed += (s, e) => { globalPopup = null; isCreatingPopup = false; };
            
            // Position popup above cursor
            var cursorPos = GetCursorPosition();
            PositionPopup(cursorPos);
            
            globalPopup.Activate();
            isCreatingPopup = false;
        }

        private void PositionPopup(POINT cursorPos)
        {
            globalPopup!.WindowStartupLocation = WindowStartupLocation.Manual;
            globalPopup.Left = -2000; // Off screen temporarily
            globalPopup.Top = -2000;
            globalPopup.Show();
            globalPopup.UpdateLayout();
            
            // Get popup size
            double width = globalPopup.ActualWidth > 0 ? globalPopup.ActualWidth : 350;
            double height = globalPopup.ActualHeight > 0 ? globalPopup.ActualHeight : 200;
            
            // Position above cursor
            double left = cursorPos.X - (width / 2);
            double top = cursorPos.Y - height - 10;
            
            // Keep on screen
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            left = Math.Max(0, Math.Min(left, screenWidth - width));
            top = Math.Max(0, top);
            
            globalPopup.Left = left;
            globalPopup.Top = top;
        }

        private POINT GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return point;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
