namespace Xcelerator.Models
{
    public class Cluster
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }

        public Cluster(string name, string displayName = "")
        {
            Name = name;
            DisplayName = string.IsNullOrEmpty(displayName) ? name : displayName;
        }
    }
}
