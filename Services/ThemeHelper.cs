using Microsoft.Win32;
using System;
using System.Windows.Media;

namespace TaskbarGrouper.Services
{
    public static class ThemeHelper
    {
        // Returns true if dark mode is enabled for apps
        public static bool IsDarkTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null && value is int)
                        {
                            return ((int)value) == 0;
                        }
                    }
                }
            }
            catch { }
            return false; // Default to light
        }

        // Gets the Windows accent color
        public static Color GetAccentColor()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\DWM"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("ColorizationColor");
                        if (value != null && value is int color)
                        {
                            byte[] bytes = BitConverter.GetBytes(color);
                            return Color.FromArgb(255, bytes[2], bytes[1], bytes[0]);
                        }
                    }
                }
            }
            catch { }
            return Colors.DodgerBlue; // Fallback
        }
    }
}
