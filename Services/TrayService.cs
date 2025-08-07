using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using TaskbarGrouper.Views;

namespace TaskbarGrouper.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private GroupPopup? _groupPopup;
        private bool _disposed = false;

        public void Initialize()
        {
            try
            {
                // Create the notify icon
                _notifyIcon = new NotifyIcon
                {
                    Icon = CreateDefaultIcon(),
                    Text = "Taskbar Grouper",
                    Visible = true
                };

                // Set up context menu
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Show Groups", null, OnShowGroups);
                contextMenu.Items.Add("-"); // Separator
                contextMenu.Items.Add("Settings", null, OnSettings);
                contextMenu.Items.Add("Exit", null, OnExit);
                
                _notifyIcon.ContextMenuStrip = contextMenu;

                // Handle left click to show groups
                _notifyIcon.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ShowGroupsPopup();
                    }
                };

                // Initialize popup window
                _groupPopup = new GroupPopup();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to initialize tray service: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Icon CreateDefaultIcon()
        {
            // Create a simple default icon
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.DodgerBlue, 2, 2, 12, 12);
                g.FillRectangle(Brushes.White, 4, 4, 8, 8);
                g.FillRectangle(Brushes.DodgerBlue, 6, 6, 4, 4);
            }
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void OnShowGroups(object? sender, EventArgs e)
        {
            // Find the main window and show it
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.WindowState = System.Windows.WindowState.Normal;
                mainWindow.Activate();
            }
        }

        private void ShowGroupsPopup()
        {
            // Delegate to main window
            OnShowGroups(null, EventArgs.Empty);
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            System.Windows.MessageBox.Show("Settings functionality will be implemented in a future version.",
                "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _notifyIcon?.Dispose();
                _groupPopup?.Close();
                _disposed = true;
            }
        }

        ~TrayService()
        {
            Dispose(false);
        }
    }
}
