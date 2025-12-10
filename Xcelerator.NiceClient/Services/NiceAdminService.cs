using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xcelerator.NiceClient.Models;

namespace Xcelerator.NiceClient.Services
{
    public class NiceAdminService : INiceAdminService
    {
        private readonly HttpClient _httpClient;

        // HttpClient is injected automatically by the framework
        public NiceAdminService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<TokenDto>> GetAgentsAsync(string baseUrl, string sessionToken)
        {
            // 1. Construct the URL (Example version v25.0)
            // Ensure baseUrl doesn't have a trailing slash or handle it safely
            var requestUrl = $"{baseUrl.TrimEnd('/')}/services/v25.0/agents";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // 2. Add Authorization
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

            // 3. Send Request
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // 4. Parse Response
            var result = await response.Content.ReadFromJsonAsync<NiceApiResponse<TokenDto>>();
            return result?.resultSet ?? Enumerable.Empty<TokenDto>();
        }
    }
}
