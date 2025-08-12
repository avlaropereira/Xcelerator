namespace Xcelerator.Models
{
    public class LoginCredentials
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public List<Cluster> SelectedClusters { get; set; } = new();
    }
}
