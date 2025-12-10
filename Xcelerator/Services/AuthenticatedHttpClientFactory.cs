using System.Net.Http;
using System.Net.Http.Headers;
using Xcelerator.Models;

namespace Xcelerator.Services
{
    /// <summary>
    /// Service to provide authenticated HttpClient instances for API calls
    /// </summary>
    public interface IAuthenticatedHttpClientFactory
    {
        /// <summary>
        /// Creates an HttpClient configured with the authentication token from the specified cluster
        /// </summary>
        /// <param name="cluster">The cluster containing the authentication token</param>
        /// <returns>An HttpClient configured with the authentication token</returns>
        HttpClient CreateClient(Cluster cluster);

        /// <summary>
        /// Checks if the cluster has a valid token
        /// </summary>
        /// <param name="cluster">The cluster to check</param>
        /// <returns>True if the cluster has a valid token, false otherwise</returns>
        bool HasValidToken(Cluster cluster);
    }

    /// <summary>
    /// Factory for creating authenticated HttpClient instances
    /// </summary>
    public class AuthenticatedHttpClientFactory : IAuthenticatedHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthenticatedHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient CreateClient(Cluster cluster)
        {
            if (cluster == null)
            {
                throw new ArgumentNullException(nameof(cluster));
            }

            if (!HasValidToken(cluster))
            {
                throw new InvalidOperationException($"Cluster '{cluster.DisplayName}' does not have a valid authentication token.");
            }

            var client = _httpClientFactory.CreateClient();
            
            // Set the base address if available
            if (!string.IsNullOrEmpty(cluster.ResourceServerBaseUri))
            {
                client.BaseAddress = new Uri(cluster.ResourceServerBaseUri);
            }

            // Set the authorization header with the token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                cluster.TokenType ?? "Bearer",
                cluster.AuthToken
            );

            return client;
        }

        public bool HasValidToken(Cluster cluster)
        {
            if (cluster == null) return false;
            
            return !string.IsNullOrWhiteSpace(cluster.AuthToken) && 
                   (cluster.TokenExpirationTime == null || cluster.TokenExpirationTime > DateTime.UtcNow);
        }
    }
}
