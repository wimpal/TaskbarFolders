 using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using TaskbarGrouper.Services;

namespace TaskbarGrouper.Models
{
    public class AppGroup
    {
        public string Name { get; set; } = string.Empty;
        public List<string> ProcessNames { get; set; } = new();
        public System.Windows.Media.Color Color { get; set; } = Colors.Blue;
        public string? IconPath { get; set; }

        public bool ContainsProcess(string processName)
        {
            return ProcessNames.Any(p => string.Equals(p, processName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class GroupedWindow
    {
        public WindowInfo WindowInfo { get; set; } = null!;
        public AppGroup Group { get; set; } = null!;
    }

    public class GroupManager
    {
        private List<AppGroup> _groups = new();

        public GroupManager()
        {
            InitializeDefaultGroups();
        }

        private void InitializeDefaultGroups()
        {
            _groups = new List<AppGroup>
            {
                new AppGroup
                {
                    Name = "Browsers",
                    ProcessNames = new List<string> { "chrome", "firefox", "msedge", "opera", "brave" },
                    Color = Colors.DodgerBlue
                },
                new AppGroup
                {
                    Name = "Development",
                    ProcessNames = new List<string> { "devenv", "Code", "rider", "webstorm", "idea64", "notepad++" },
                    Color = Colors.Green
                },
                new AppGroup
                {
                    Name = "Media",
                    ProcessNames = new List<string> { "vlc", "wmplayer", "spotify", "itunes", "foobar2000" },
                    Color = Colors.Purple
                },
                new AppGroup
                {
                    Name = "Office",
                    ProcessNames = new List<string> { "winword", "excel", "powerpnt", "outlook", "teams" },
                    Color = Colors.Orange
                },
                new AppGroup
                {
                    Name = "Communication",
                    ProcessNames = new List<string> { "discord", "slack", "telegram", "whatsapp", "skype" },
                    Color = Colors.Teal
                }
            };
        }

        public List<AppGroup> GetGroups()
        {
            return _groups.ToList();
        }

        public void AddGroup(AppGroup group)
        {
            _groups.Add(group);
        }

        public void RemoveGroup(string groupName)
        {
            _groups.RemoveAll(g => g.Name == groupName);
        }

        public AppGroup? FindGroupForProcess(string processName)
        {
            return _groups.FirstOrDefault(g => g.ContainsProcess(processName));
        }

        public List<GroupedWindow> GroupWindows(List<WindowInfo> windows)
        {
            var groupedWindows = new List<GroupedWindow>();

            foreach (var window in windows)
            {
                var group = FindGroupForProcess(window.ProcessName);
                if (group != null)
                {
                    groupedWindows.Add(new GroupedWindow
                    {
                        WindowInfo = window,
                        Group = group
                    });
                }
            }

            return groupedWindows;
        }

        public Dictionary<AppGroup, List<WindowInfo>> GetWindowsByGroup(List<WindowInfo> windows)
        {
            var result = new Dictionary<AppGroup, List<WindowInfo>>();

            var groupedWindows = GroupWindows(windows);
            
            foreach (var groupedWindow in groupedWindows)
            {
                if (!result.ContainsKey(groupedWindow.Group))
                {
                    result[groupedWindow.Group] = new List<WindowInfo>();
                }
                result[groupedWindow.Group].Add(groupedWindow.WindowInfo);
            }

            return result;
        }
    }
}
