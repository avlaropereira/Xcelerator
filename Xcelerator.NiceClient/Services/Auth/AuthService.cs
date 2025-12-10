using System.Net.Http.Json;
using Xcelerator.NiceClient.Models;

namespace Xcelerator.NiceClient.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        // Use the token URL from your script
        private const string TokenUrl = "https://cxone.staging.niceincontact.com/auth/token";

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AuthToken> AuthenticateAsync(string basicAuthHeader, string username, string password)
        {
            // 1. Setup the Request
            var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);

            // 2. Add Authorization Header (Matches your $headers.Add("Authorization"...))
            // We assume 'basicAuthHeader' passed in includes "Basic " prefix, or we add it if missing
            if (!basicAuthHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                basicAuthHeader = $"Basic {basicAuthHeader}";
            }
            request.Headers.Add("Authorization", basicAuthHeader);

            // 3. Create the Body (Matches your $body string)
            // FormUrlEncodedContent automatically sets Content-Type to application/x-www-form-urlencoded
            var bodyParams = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", username },
                { "password", password }
            };
            request.Content = new FormUrlEncodedContent(bodyParams);

            // 4. Send and Check
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                // Capture the error message from NICE for easier debugging
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Auth failed: {response.StatusCode}. Details: {errorContent}");
            }

            // 5. Convert JSON Response to Object
            var tokenData = await response.Content.ReadFromJsonAsync<AuthToken>();
            return tokenData ?? new AuthToken();
        }
    }
}
