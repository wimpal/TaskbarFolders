using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace TaskbarGrouper.Views
{
	public partial class GroupPopup : Window
	{
		public GroupPopup()
		{
			InitializeComponent();
			LoadRunningApps();
		}

		private void LoadRunningApps()
		{
			AppListPanel.Children.Clear();
			
			var windows = WindowEnumerator.GetOpenWindows();
			
			foreach (var win in windows)
			{
				var button = new Button
				{
					Style = (Style)this.FindResource("ModernButton"),
					Height = 50,
					Tag = win.Hwnd
				};
				
				// Create a grid for icon and text layout
				var grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				
				// Try to get window icon
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
						// Default icon if none found
						iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
							SystemIcons.Application.Handle,
							System.Windows.Int32Rect.Empty,
							System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
					}
				}
				catch
				{
					// Fallback to default icon
					iconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
						SystemIcons.Application.Handle,
						System.Windows.Int32Rect.Empty,
						System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
				}
				
				Grid.SetColumn(iconImage, 0);
				grid.Children.Add(iconImage);
				
				// Window title
				var textBlock = new TextBlock
				{
					Text = win.Title,
					VerticalAlignment = VerticalAlignment.Center,
					TextTrimming = TextTrimming.CharacterEllipsis,
					FontWeight = FontWeights.Normal
				};
				
				Grid.SetColumn(textBlock, 1);
				grid.Children.Add(textBlock);
				
				button.Content = grid;
				button.Click += (s, e) => WindowEnumerator.BringToFront((IntPtr)button.Tag!);
				
				AppListPanel.Children.Add(button);
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
			ShowWindow(hwnd, 5); // SW_SHOW
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

		private const uint WM_GETICON = 0x007F;
		private const int ICON_SMALL = 0;
		private const int ICON_BIG = 1;
		private const int ICON_SMALL2 = 2;
		private const int GCL_HICON = -14;
		private const int GCL_HICONSM = -34;
	}
}
