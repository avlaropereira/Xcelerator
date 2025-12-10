using Xcelerator.NiceClient.Models;

namespace Xcelerator.NiceClient.Services.Nice.Admin
{
    public interface IAdminService
    {
        // We pass the credentials/url dynamically since you support multi-cluster
        Task<IEnumerable<TokenDto>> GetAgentsAsync(string baseUrl, string sessionToken);
    }
}
