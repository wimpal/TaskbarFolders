using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Text.Json;
using System.Linq;


namespace TaskbarGrouper.Views
{
    public partial class GroupPopup : Window
    {
        private static string GetDataFilePath()
        {
            string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarGrouper");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return System.IO.Path.Combine(folder, "categories.json");
        }

        private void SaveCategories()
        {
            try
            {
                var data = categories.Select(c => new Models.AppGroup { Name = c.Name, Items = new List<string>(c.Items) }).ToList();
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(GetDataFilePath(), json);
            }
            catch { /* Ignore errors for now */ }
        }

        private void LoadCategories()
        {
            try
            {
                string path = GetDataFilePath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<List<Models.AppGroup>>(json);
                    if (data != null)
                    {
                        categories.Clear();
                        categories.AddRange(data);
                    }
                }
            }
            catch { /* Ignore errors for now */ }
        }

    private readonly List<Models.AppGroup> categories = new();


        private double? initialLeft = null;
        private System.Windows.Forms.Screen? initialScreen = null;

        public GroupPopup()
        {
            InitializeComponent();
            ApplyTheme();
            LoadCategories();
            LoadRunningApps();
            // Initialize with one default category if none loaded
            if (categories.Count == 0)
            {
                categories.Add(new Models.AppGroup { Name = "Default" });
            }
            RenderCategories();
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

        private void ApplyTheme()
        {
            var isDark = TaskbarGrouper.Services.ThemeHelper.IsDarkTheme();
            var accent = TaskbarGrouper.Services.ThemeHelper.GetAccentColor();

            // Try to enable Mica effect (Windows 11+)
            bool micaApplied = false;
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero && IsWindows11OrGreater())
                {
                    const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
                    int micaValue = 2; // DWMSBT_MAINWINDOW
                    IntPtr ptrMicaValue = System.Runtime.InteropServices.Marshal.AllocHGlobal(sizeof(int));
                    System.Runtime.InteropServices.Marshal.WriteInt32(ptrMicaValue, micaValue);
                    DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ptrMicaValue, sizeof(int));
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(ptrMicaValue);
                    micaApplied = true;
                }
            }
            catch { /* Ignore if not supported */ }

            // Fallback: use system Mica color if available, else dark/light
                System.Windows.Media.Color micaColor = isDark ? System.Windows.Media.Color.FromRgb(32, 32, 32) : System.Windows.Media.Colors.White;
            if (!micaApplied)
            {
                // Try to get the system's Mica fallback color (Windows 11)
                try
                {
                    var micaBrush = System.Windows.Application.Current.TryFindResource("SolidBackgroundFillColorBase") as System.Windows.Media.SolidColorBrush;
                    if (micaBrush != null)
                        micaColor = micaBrush.Color;
                }
                catch { }
            }

            this.Background = new System.Windows.Media.SolidColorBrush(micaColor);
            this.Foreground = new System.Windows.Media.SolidColorBrush(isDark ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black);
            this.Resources["AccentColor"] = new System.Windows.Media.SolidColorBrush(accent);
        }

        // Helper: check if running on Windows 11+
        private static bool IsWindows11OrGreater()
        {
            var os = Environment.OSVersion.Version;
            return (os.Major >= 10 && os.Build >= 22000) || (os.Major > 10);
        }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, IntPtr attrValue, int attrSize);

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
            // Find which category this button belongs to
            if (sender is Button btn && btn.Tag is Models.AppGroup group)
            {
                var dlg = new OpenFileDialog
                {
                    Multiselect = true,
                    Title = $"Add files to {group.Name}"
                };
                if (dlg.ShowDialog() == true)
                {
                    foreach (var file in dlg.FileNames)
                    {
                        if (!group.Items.Contains(file))
                            group.Items.Add(file);
                    }
                    SaveCategories();
                    RenderCategories();
                }
            }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Always add new category at the end, default name is 'Category'
            categories.Add(new Models.AppGroup { Name = "Category" });
            SaveCategories();
            RenderCategories();
        }

        private void RenderCategories()
        {
            CategoriesPanel.Children.Clear();
            for (int i = 0; i < categories.Count; i++)
            {
                var group = categories[i];
                // Category header: [Title] [Divider] [+]
                var headerGrid = new Grid { Margin = new Thickness(0, 8, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Title
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Divider
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // +

                var headerTextBox = new TextBox
                {
                    Text = group.Name,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11, // Very small for compactness
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 2, 4, 2),
                    BorderThickness = new Thickness(0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)),
                    MinWidth = 60,
                    MaxWidth = 120,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                headerTextBox.LostFocus += (s, e) => {
                    if (headerTextBox.Text.Trim().Length == 0)
                        headerTextBox.Text = "Category";
                    group.Name = headerTextBox.Text.Trim();
                    SaveCategories();
                };
                headerTextBox.KeyDown += (s, e) => {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        if (headerTextBox.Text.Trim().Length == 0)
                            headerTextBox.Text = "Category";
                        group.Name = headerTextBox.Text.Trim();
                        SaveCategories();
                        // Optionally move focus away to commit
                        headerTextBox.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
                    }
                };
                Grid.SetColumn(headerTextBox, 0);
                headerGrid.Children.Add(headerTextBox);

                var divider = new Separator
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    Height = 1,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.5
                };
                Grid.SetColumn(divider, 1);
                headerGrid.Children.Add(divider);

                var addFileBtn = new Button
                {
                    Content = "+",
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(4, 0, 8, 0),
                    Padding = new Thickness(0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(208, 208, 208)),
                    BorderThickness = new System.Windows.Thickness(1),
                    Foreground = (System.Windows.Media.Brush)this.Resources["AccentColor"],
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    ToolTip = "Add file/app to this category",
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = group,
                    VerticalAlignment = VerticalAlignment.Center
                };
                addFileBtn.Click += AddFileButton_Click;
                Grid.SetColumn(addFileBtn, 2);
                headerGrid.Children.Add(addFileBtn);
                CategoriesPanel.Children.Add(headerGrid);

                // List of files/apps in this category
                var itemsPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(8, 2, 0, 2) };
                if (group.Items.Count == 0)
                {
                    itemsPanel.Children.Add(new TextBlock
                    {
                        Text = "(No files/apps)",
                        FontStyle = FontStyles.Italic,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                        Margin = new System.Windows.Thickness(0, 2, 0, 2)
                    });
                }
                else
                {
                    foreach (var file in group.Items)
                    {
                        var btn = new Button
                        {
                            Style = (Style)this.FindResource("ModernButton"),
                            Height = 40,
                            Tag = file,
                            Margin = new Thickness(0, 2, 0, 2)
                        };
                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        // File icon
                        var iconImage = new System.Windows.Controls.Image
                        {
                            Width = 24,
                            Height = 24,
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        try
                        {
                            var icon = GetFileIcon(file);
                            if (icon != null)
                            {
                                iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                    icon.Handle,
                                    System.Windows.Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                                icon.Dispose();
                            }
                        }
                        catch { }
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
                        btn.Content = grid;
                        btn.Click += (s, e) => OpenFile(file);
                        itemsPanel.Children.Add(btn);
                    }
                }
                CategoriesPanel.Children.Add(itemsPanel);
            }

            // Add the 'Add category' button at the end
            var addCategoryBtn = new Button
            {
                Content = "Add category",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(16, 6, 16, 6),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(208, 208, 208)),
                BorderThickness = new System.Windows.Thickness(1),
                Foreground = (System.Windows.Media.Brush)this.Resources["AccentColor"],
                Cursor = System.Windows.Input.Cursors.Hand
            };
            addCategoryBtn.Click += AddCategoryButton_Click;
            CategoriesPanel.Children.Add(addCategoryBtn);
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
    private static System.Drawing.Icon? GetFileIcon(string filePath)
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
                            System.Drawing.SystemIcons.Application.Handle,
                            System.Windows.Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                }
                catch
                {
                    iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        System.Drawing.SystemIcons.Application.Handle,
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
                    BorderThickness = new System.Windows.Thickness(1),
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
                    BorderThickness = new System.Windows.Thickness(0),
                    Padding = new System.Windows.Thickness(0),
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
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                    Margin = new System.Windows.Thickness(0, 20, 0, 20)
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
                    proc.Kill();
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

    public static System.Drawing.Icon? GetWindowIcon(IntPtr hwnd)
        {
            try
            {
                IntPtr hIcon = SendMessage(hwnd, WM_GETICON, ICON_BIG, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = SendMessage(hwnd, WM_GETICON, ICON_SMALL, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = SendMessage(hwnd, WM_GETICON, ICON_SMALL2, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hwnd, GCL_HICONSM);

                if (hIcon != IntPtr.Zero)
                {
            return System.Drawing.Icon.FromHandle(hIcon);
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
