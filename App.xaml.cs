using System;
using System.Windows;
using TaskbarGrouper.Views;

namespace TaskbarGrouper
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent multiple instances
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var runningProcesses = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
            
            if (runningProcesses.Length > 1)
            {
                MessageBox.Show("Taskbar Grouper is already running.", "Already Running", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            try
            {
                // Create the main window - it will start minimized
                MainWindow = new MainWindow();
                MainWindow.Show(); // This will show it in the taskbar even when minimized
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start Taskbar Grouper: {ex.Message}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
