namespace Xcelerator.Models
{
    /// <summary>
    /// Configuration class for deserializing cluster data from JSON
    /// </summary>
    public class ClusterConfig
    {
        public string Name { get; set; } = string.Empty;
        public string ApiBaseURL { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string TypeOfCluster { get; set; } = string.Empty;
    }
}
