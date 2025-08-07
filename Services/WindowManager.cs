using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskbarGrouper.Services
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public ImageSource? Icon { get; set; }
        public bool IsVisible { get; set; }
    }

    public class WindowManager
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const uint WM_GETICON = 0x7F;
        private const uint ICON_BIG = 1;
        private const uint ICON_SMALL = 0;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public List<WindowInfo> GetVisibleWindows()
        {
            var windows = new List<WindowInfo>();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsValidWindow(hWnd))
                {
                    var windowInfo = CreateWindowInfo(hWnd);
                    if (windowInfo != null)
                    {
                        windows.Add(windowInfo);
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows.Where(w => !string.IsNullOrWhiteSpace(w.Title) && 
                                     !IsSystemWindow(w.ProcessName))
                          .OrderBy(w => w.Title)
                          .ToList();
        }

        private bool IsValidWindow(IntPtr hWnd)
        {
            if (!IsWindowVisible(hWnd))
                return false;

            // Skip tool windows
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return false;

            return true;
        }

        private WindowInfo? CreateWindowInfo(IntPtr hWnd)
        {
            try
            {
                int length = GetWindowTextLength(hWnd);
                if (length == 0) return null;

                var buffer = new StringBuilder(length + 1);
                GetWindowText(hWnd, buffer, buffer.Capacity);

                GetWindowThreadProcessId(hWnd, out uint processId);
                var process = Process.GetProcessById((int)processId);

                return new WindowInfo
                {
                    Handle = hWnd,
                    Title = buffer.ToString(),
                    ProcessName = process.ProcessName,
                    ProcessId = (int)processId,
                    Icon = GetWindowIcon(hWnd, process),
                    IsVisible = IsWindowVisible(hWnd)
                };
            }
            catch
            {
                return null;
            }
        }

        private ImageSource? GetWindowIcon(IntPtr hWnd, Process process)
        {
            try
            {
                // Try to get icon from window
                IntPtr hIcon = SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_BIG, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                {
                    hIcon = SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
                }

                if (hIcon == IntPtr.Zero && !string.IsNullOrEmpty(process.MainModule?.FileName))
                {
                    // Extract icon from executable
                    hIcon = ExtractIcon(IntPtr.Zero, process.MainModule.FileName, 0);
                }

                if (hIcon != IntPtr.Zero)
                {
                    var icon = Icon.FromHandle(hIcon);
                    var bitmap = icon.ToBitmap();
                    
                    var hBitmap = bitmap.GetHbitmap();
                    var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty, 
                        BitmapSizeOptions.FromEmptyOptions());

                    // Clean up
                    DeleteObject(hBitmap);
                    DestroyIcon(hIcon);

                    return imageSource;
                }
            }
            catch
            {
                // Ignore errors and return null
            }

            return null;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private bool IsSystemWindow(string processName)
        {
            var systemProcesses = new[]
            {
                "dwm", "winlogon", "csrss", "smss", "wininit", "services",
                "lsass", "explorer", "svchost", "conhost", "dllhost"
            };

            return systemProcesses.Contains(processName.ToLowerInvariant());
        }

        public void BringWindowToFront(IntPtr hWnd)
        {
            try
            {
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, SW_RESTORE);
                }
                else
                {
                    ShowWindow(hWnd, SW_SHOW);
                }

                SetForegroundWindow(hWnd);
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
