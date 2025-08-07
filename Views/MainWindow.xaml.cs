using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TaskbarGrouper.Models;
using TaskbarGrouper.Services;

namespace TaskbarGrouper.Views
{
    public partial class MainWindow : Window
    {
        private readonly WindowManager _windowManager;
        private readonly GroupManager _groupManager;
        private GroupPopup? _groupPopup;
        private bool _suppressNextPopup = false;
        private DispatcherTimer? _suppressionTimer;

        public MainWindow()
        {
            InitializeComponent();
            _windowManager = new WindowManager();
            _groupManager = new GroupManager();
            
            // Start minimized so it appears only in taskbar
            WindowState = WindowState.Minimized;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // When clicked from taskbar, show popup instead of the main window
            if (WindowState == WindowState.Normal)
            {
                if (_suppressNextPopup)
                {
                    _suppressNextPopup = false;
                    WindowState = WindowState.Minimized;
                    return;
                }
                // Immediately minimize back and show popup
                WindowState = WindowState.Minimized;
                ShowFixedPopup();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Instead of closing, just minimize to taskbar
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            _groupPopup?.Hide();
        }

        private void ShowFixedPopup()
        {
            try
            {
                if (_groupPopup != null && _groupPopup.IsVisible)
                {
                    // Hide the popup and suppress next popup for 500ms
                    _groupPopup.Hide();
                    _suppressNextPopup = true;
                    
                    // Use a timer to clear the suppression flag
                    _suppressionTimer?.Stop();
                    _suppressionTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    _suppressionTimer.Tick += (s, e) =>
                    {
                        _suppressNextPopup = false;
                        _suppressionTimer.Stop();
                    };
                    _suppressionTimer.Start();
                    return;
                }

                if (_groupPopup == null)
                {
                    _groupPopup = new GroupPopup();
                }

                // Use cursor position as the most accurate method
                var cursorPos = System.Windows.Forms.Cursor.Position;
                var popupWidth = 350;
                var popupHeight = 450;
                var popupX = (double)cursorPos.X - (popupWidth / 2.0);
                var popupY = (double)cursorPos.Y - popupHeight - 20;

                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                if (popupX < 10)
                    popupX = 10;
                else if (popupX + popupWidth > screenWidth - 10)
                    popupX = screenWidth - popupWidth - 10;
                if (popupY < 10)
                    popupY = 10;

                _groupPopup.ShowAtPosition(new Point(popupX, popupY));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error showing popup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Point GetDefaultTaskbarPosition()
        {
            // Simple fallback to bottom-center of screen
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            return new Point(screenWidth / 2, screenHeight - 40);
        }

        private void LoadGroups()
        {
            try
            {
                var windows = _windowManager.GetVisibleWindows();
                var groupedWindows = _groupManager.GetWindowsByGroup(windows);

                var groupViewModels = groupedWindows.Select(kvp => new GroupViewModel
                {
                    GroupName = kvp.Key.Name,
                    GroupColorBrush = new SolidColorBrush(kvp.Key.Color),
                    Windows = kvp.Value,
                    WindowCount = kvp.Value.Count.ToString()
                }).ToList();

                GroupsItemsControl.ItemsSource = groupViewModels;
                
                // Update window title with count
                var totalApps = groupViewModels.Sum(g => g.Windows.Count);
                Title = $"App Groups ({totalApps} apps)";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading groups: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is WindowInfo windowInfo)
            {
                try
                {
                    _windowManager.BringWindowToFront(windowInfo.Handle);
                    // Hide popup after switching to the window
                    _groupPopup?.Hide();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error switching to window: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadGroups();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            _groupPopup?.Hide();
        }

        private void ShowGroupsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowFixedPopup();
        }

        private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadGroups();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Settings functionality will be implemented in a future version.",
                "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Actually close the application
            _groupPopup?.Close();
            System.Windows.Application.Current.Shutdown();
        }

        #region Windows API for taskbar positioning
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion
    }

    public class GroupViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public SolidColorBrush GroupColorBrush { get; set; } = new SolidColorBrush(Colors.Gray);
        public List<WindowInfo> Windows { get; set; } = new();
        public string WindowCount { get; set; } = "0";
    }
}
