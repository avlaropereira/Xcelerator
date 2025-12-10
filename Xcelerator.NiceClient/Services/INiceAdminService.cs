using Xcelerator.NiceClient.Models;

namespace Xcelerator.NiceClient.Services
{
    public interface INiceAdminService
    {
        // We pass the credentials/url dynamically since you support multi-cluster
        Task<IEnumerable<TokenDto>> GetAgentsAsync(string baseUrl, string sessionToken);
    }
}
