using Xcelerator.Models.Topology;

namespace Xcelerator.Models
{
    public class Cluster
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ApiBaseURL { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string TypeOfCluster { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string SelectedModule { get; set; } = string.Empty;
        public bool IsInDashboardMode { get; set; } = false;

        // Authentication token information
        public string AuthToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string ResourceServerBaseUri { get; set; } = string.Empty;
        public DateTime? TokenExpirationTime { get; set; }

        // Infrastructure Topology
        public ClusterNode? Topology { get; set; }

        public Cluster(string name, string displayName = "")
        {
            Name = name;
            DisplayName = string.IsNullOrEmpty(displayName) ? name : displayName;
        }

        public bool HasCredentials => !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
        
        public bool HasValidToken => !string.IsNullOrWhiteSpace(AuthToken) && 
                                      (TokenExpirationTime == null || TokenExpirationTime > DateTime.UtcNow);
    }
}
