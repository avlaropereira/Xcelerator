using System.IdentityModel.Tokens.Jwt;

namespace Xcelerator.NiceClient.Services.Auth
{
    /// <summary>
    /// Service for decoding JWT tokens
    /// </summary>
    public class JwtDecoder
    {
        private static readonly string[] CentralPriority = { "icBUId", "icSPId", "icAgentId", "icClusterId", "name" };
        private static readonly string[] UhPriority = { "icSPId", "icAgentId", "icBUId", "icClusterId", "name" };

        public static Dictionary<string, string> DecodeToken(string jwtString, string clusterType)
        {
            var results = new Dictionary<string, string>();
            // 1. Clean the string if it contains "Bearer "
            if (string.IsNullOrWhiteSpace(jwtString)) return results;
            var token = jwtString.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            var handler = new JwtSecurityTokenHandler();

            // 2. Validate format before reading
            if (!handler.CanReadToken(token))
            {
                throw new ArgumentException("The string provided is not a valid JWT.");
            }

            // 3. Read the token
            var jwtToken = handler.ReadJwtToken(token);

            // 4. Extract values directly (No Serialization needed!)
            string[] priorityList = clusterType.Equals("UH") ? UhPriority : CentralPriority;
            foreach (var key in priorityList)
            {
                // .TryGetValue is O(1) - extremely fast
                if (jwtToken.Payload.TryGetValue(key, out var value) && value != null)
                {
                    // .ToString() here safely handles both:
                    // - The Number 1008005
                    // - The String "10738"
                    results[key] = value.ToString();
                }
            }
            return results;
        }
    }
}
