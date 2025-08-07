using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskbarGrouper.Models;
using TaskbarGrouper.Services;

namespace TaskbarGrouper.Views
{
    public partial class GroupPopup : Window
    {
        private readonly WindowManager _windowManager;
        private readonly GroupManager _groupManager;

        public GroupPopup()
        {
            InitializeComponent();
            _windowManager = new WindowManager();
            _groupManager = new GroupManager();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Hide the popup when it loses focus
            Hide();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading groups: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WindowInfo windowInfo)
            {
                try
                {
                    _windowManager.BringWindowToFront(windowInfo.Handle);
                    Hide(); // Hide popup after switching to window
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error switching to window: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadGroups();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings functionality will be implemented in a future version.",
                "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void ShowAtPosition(Point position)
        {
            Left = position.X;
            Top = position.Y;
            
            // Ensure the window is within screen bounds
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            if (Left + Width > screenWidth)
                Left = screenWidth - Width - 10;
            
            if (Top + Height > screenHeight)
                Top = screenHeight - Height - 10;
            
            if (Left < 0) Left = 10;
            if (Top < 0) Top = 10;
            
            LoadGroups(); // Always reload when showing
            Show();
            Activate();
            Focus();
        }
    }
}
