using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Microsoft.Win32;


namespace TaskbarGrouper.Views
{
    public partial class GroupPopup : Window
    {

        private readonly List<string> addedFiles = new();


        private double? initialLeft = null;
        private System.Windows.Forms.Screen? initialScreen = null;

        public GroupPopup()
        {
            InitializeComponent();
            LoadRunningApps();
            LoadFileList();
            FileListPanel.AllowDrop = true;
            this.ContentRendered += (s, e) =>
            {
                // Store the initial Left and the screen where the popup is shown, only once, after layout is complete
                if (initialLeft == null)
                {
                    initialLeft = this.Left;
                    initialScreen = GetScreenFromPoint((int)this.Left, (int)this.Top);
                }
                PositionAboveTaskbar();
            };
            this.SizeChanged += (s, e) =>
            {
                PositionAboveTaskbar();
            };
        }

        private void PositionAboveTaskbar()
        {
            // Use the screen where the popup was first shown
            var screen = initialScreen ?? System.Windows.Forms.Screen.PrimaryScreen;
            if (screen == null)
                return;
            var wa = screen.WorkingArea;
            // Only update Top, keep initial Left
            if (initialLeft != null)
                this.Left = initialLeft.Value;
            // Place the popup so its bottom is just above the taskbar (never overlapping)
            this.Top = wa.Bottom - this.ActualHeight;
        }

        // Helper: get the screen containing a point
        private static System.Windows.Forms.Screen? GetScreenFromPoint(int x, int y)
        {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen.Bounds.Contains(x, y))
                    return screen;
            }
            return System.Windows.Forms.Screen.PrimaryScreen;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Add files to open"
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (!addedFiles.Contains(file))
                        addedFiles.Add(file);
                }
                LoadFileList();
            }
        }

        private void FileListPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void FileListPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (File.Exists(file) && !addedFiles.Contains(file))
                        addedFiles.Add(file);
                }
                LoadFileList();
            }
        }

        private void LoadFileList()
        {
            FileListPanel.Children.Clear();
            foreach (var file in addedFiles)
            {
                var button = new Button
                {
                    Style = (Style)this.FindResource("ModernButton"),
                    Height = 50,
                    Tag = file
                };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                // File icon
                var iconImage = new System.Windows.Controls.Image
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                try
                {
                    var icon = GetFileIcon(file);
                    if (icon != null)
                    {
                        iconImage.Source = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        icon.Dispose();
                    }
                }
                catch
                {
                    // fallback: no icon
                }
                Grid.SetColumn(iconImage, 0);
                grid.Children.Add(iconImage);
                // File name
                var textBlock = new TextBlock
                {
                    Text = System.IO.Path.GetFileName(file),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontWeight = FontWeights.Normal
                };
                Grid.SetColumn(textBlock, 1);
                grid.Children.Add(textBlock);
                button.Content = grid;
                button.Click += (s, e) => OpenFile(file);
                FileListPanel.Children.Add(button);
            }
            if (addedFiles.Count == 0)
            {
                var noFilesText = new TextBlock
                {
                    Text = "Drag files here or use + to add",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(0, 10, 0, 10)
                };
                FileListPanel.Children.Add(noFilesText);
            }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Get the icon for a file (returns System.Drawing.Icon)
        private static Icon? GetFileIcon(string filePath)
        {
            try
            {
                return System.Drawing.Icon.ExtractAssociatedIcon(filePath);
            }
            catch
            {
                return null;
            }
        }

        private void LoadRunningApps()
        {
            AppListPanel.Children.Clear();
            var windows = WindowEnumerator.GetOpenWindows();
            foreach (var win in windows)
            {

                var rowGrid = new Grid
                {
                    Background = System.Windows.Media.Brushes.Transparent // Allow button hover background to show
                };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // icon
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // title
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) }); // close btn

                // Icon
                var iconImage = new System.Windows.Controls.Image
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                try
                {
                    var icon = WindowEnumerator.GetWindowIcon(win.Hwnd);
                    if (icon != null)
                    {
                        iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            System.Windows.Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        icon.Dispose();
                    }
                    else
                    {
                        iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            SystemIcons.Application.Handle,
                            System.Windows.Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                }
                catch
                {
                    iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        SystemIcons.Application.Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                Grid.SetColumn(iconImage, 0);
                rowGrid.Children.Add(iconImage);

                // Title
                var textBlock = new TextBlock
                {
                    Text = win.Title,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontWeight = FontWeights.Normal
                };
                Grid.SetColumn(textBlock, 1);
                rowGrid.Children.Add(textBlock);

                // Close button
                var closeBtn = new Button
                {
                    Content = "✕",
                    Width = 28,
                    Height = 28,
                    Margin = new Thickness(4, 0, 0, 0),
                    Padding = new Thickness(0),
                    ToolTip = "Force close program",
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 0, 0)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = win.Hwnd
                };
                closeBtn.Click += (s, e) => ForceCloseWindow((IntPtr)closeBtn.Tag!);
                Grid.SetColumn(closeBtn, 2);
                rowGrid.Children.Add(closeBtn);

                // Main button for focusing
                var focusBtn = new Button
                {
                    Style = (Style)this.FindResource("ModernButton"),
                    Height = 50,
                    Content = rowGrid,
                    Tag = win.Hwnd,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };
                focusBtn.Click += (s, e) => WindowEnumerator.BringToFront((IntPtr)focusBtn.Tag!);
                AppListPanel.Children.Add(focusBtn);
            }
            // If no windows found, show a message
            if (windows.Count == 0)
            {
                var noWindowsText = new TextBlock
                {
                    Text = "No windows found",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(0, 20, 0, 20)
                };
                AppListPanel.Children.Add(noWindowsText);
            }
        }

        // Force close a window by killing its process
        private void ForceCloseWindow(IntPtr hwnd)
        {
            try
            {
                uint pid = 0;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != 0)
                {
                    var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                    proc.Kill(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to force close program:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }

    public static class WindowEnumerator
    {
        public class WindowInfo
        {
            public IntPtr Hwnd { get; set; }
            public string Title { get; set; } = string.Empty;
        }

        public static List<WindowInfo> GetOpenWindows()
        {
            var windows = new List<WindowInfo>();
            EnumWindows((hwnd, lParam) =>
            {
                if (IsWindowVisible(hwnd))
                {
                    int length = GetWindowTextLength(hwnd);
                    if (length > 0)
                    {
                        var builder = new System.Text.StringBuilder(length + 1);
                        GetWindowText(hwnd, builder, builder.Capacity);
                        string title = builder.ToString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            windows.Add(new WindowInfo { Hwnd = hwnd, Title = title });
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public static void BringToFront(IntPtr hwnd)
        {
            SetForegroundWindow(hwnd);
        }

        public static Icon? GetWindowIcon(IntPtr hwnd)
        {
            try
            {
                IntPtr hIcon = SendMessage(hwnd, WM_GETICON, ICON_BIG, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = SendMessage(hwnd, WM_GETICON, ICON_SMALL, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = SendMessage(hwnd, WM_GETICON, ICON_SMALL2, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hwnd, GCL_HICON);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hwnd, GCL_HICONSM);

                if (hIcon != IntPtr.Zero)
                {
                    return Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                // Ignore exceptions when getting icons
            }
            return null;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        private const uint WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int ICON_SMALL2 = 2;
        private const int GCL_HICON = -14;
        private const int GCL_HICONSM = -34;
    }
}
