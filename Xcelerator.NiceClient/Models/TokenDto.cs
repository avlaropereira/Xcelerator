namespace Xcelerator.NiceClient.Models
{
    public class TokenDto
    {
        public int AgentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
    public class NiceApiResponse<T>
    {
        public List<T> resultSet { get; set; } = new List<T>();
    }
}
