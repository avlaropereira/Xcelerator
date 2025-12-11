using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Xcelerator.NiceClient.Models;

namespace Xcelerator.NiceClient.Services.Auth
{
    /// <summary>
    /// Service for decoding JWT tokens
    /// </summary>
    public class JwtDecoder
    {
        public static UserTokenPayload DecodeToken(string jwtString)
        {
            // 1. Clean the string if it contains "Bearer "
            var token = jwtString.Replace("Bearer ", "").Trim();

            var handler = new JwtSecurityTokenHandler();

            // 2. Validate format before reading
            if (!handler.CanReadToken(token))
            {
                throw new ArgumentException("The string provided is not a valid JWT.");
            }

            // 3. Read the token
            var jwtToken = handler.ReadJwtToken(token);

            // 4. Extract the payload (claims) to a JSON string
            // We serialize the payload dictionary back to JSON so we can 
            // strictly type it into our UserTokenPayload class.
            var payloadJson = JsonSerializer.Serialize(jwtToken.Payload);

            // 5. Deserialize into our strong type
            var result = JsonSerializer.Deserialize<UserTokenPayload>(payloadJson);

            return result;
        }
    }
}
