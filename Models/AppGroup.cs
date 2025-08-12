using System.Collections.Generic;

namespace TaskbarGrouper.Models
{
    public class AppGroup
    {
        public string Name { get; set; }
        public List<string> Items { get; set; } = new List<string>(); // File or app paths
    }
}
