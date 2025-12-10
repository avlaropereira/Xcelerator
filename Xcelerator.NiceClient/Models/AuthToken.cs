using System.Text.Json.Serialization;

namespace Xcelerator.NiceClient.Models
{
    public class AuthToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        // NICE often returns the base URL for subsequent API calls here
        [JsonPropertyName("resource_server_base_uri")]
        public string ResourceServerBaseUri { get; set; } = string.Empty;
    }
}
