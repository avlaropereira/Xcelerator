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
        /// <summary>
        /// Decodes a JWT token and extracts claims
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <returns>TokenData containing decoded claims</returns>
        //public static UserTokenPayload DecodeToken(string token)
        //{
        //    var tokenData = new TokenData();

        //    if (string.IsNullOrWhiteSpace(token))
        //    {
        //        return tokenData;
        //    }

        //    try
        //    {
        //        // JWT structure: header.payload.signature
        //        var parts = token.Split('.');
        //        if (parts.Length != 3)
        //        {
        //            return tokenData;
        //        }

        //        // Decode the payload (second part)
        //        var payload = parts[1];

        //        // JWT uses Base64Url encoding, which needs to be converted to standard Base64
        //        payload = payload.Replace('-', '+').Replace('_', '/');

        //        // Add padding if needed
        //        switch (payload.Length % 4)
        //        {
        //            case 2: payload += "=="; break;
        //            case 3: payload += "="; break;
        //        }

        //        // Decode from Base64
        //        var payloadBytes = Convert.FromBase64String(payload);
        //        var payloadJson = Encoding.UTF8.GetString(payloadBytes);

        //        // Parse JSON
        //        using var document = JsonDocument.Parse(payloadJson);
        //        var root = document.RootElement;

        //        // Extract standard JWT claims
        //        tokenData.Subject = GetStringValue(root, "sub");
        //        tokenData.Issuer = GetStringValue(root, "iss");
        //        tokenData.Audience = GetStringValue(root, "aud");
        //        tokenData.UserId = GetStringValue(root, "user_id") ?? GetStringValue(root, "uid");
        //        tokenData.UserName = GetStringValue(root, "username") ?? GetStringValue(root, "name");
        //        tokenData.Email = GetStringValue(root, "email");
        //        tokenData.TenantId = GetStringValue(root, "tenant_id") ?? GetStringValue(root, "tid");
        //        tokenData.BusinessUnit = GetStringValue(root, "business_unit") ?? GetStringValue(root, "bu");

        //        // Extract time-based claims (Unix timestamps)
        //        tokenData.ExpirationTime = GetDateTimeFromUnixTimestamp(root, "exp");
        //        tokenData.IssuedAt = GetDateTimeFromUnixTimestamp(root, "iat");
        //        tokenData.NotBefore = GetDateTimeFromUnixTimestamp(root, "nbf");

        //        // Extract roles
        //        if (root.TryGetProperty("roles", out var rolesElement))
        //        {
        //            if (rolesElement.ValueKind == JsonValueKind.Array)
        //            {
        //                foreach (var role in rolesElement.EnumerateArray())
        //                {
        //                    tokenData.Roles.Add(role.GetString() ?? string.Empty);
        //                }
        //            }
        //            else if (rolesElement.ValueKind == JsonValueKind.String)
        //            {
        //                tokenData.Roles.Add(rolesElement.GetString() ?? string.Empty);
        //            }
        //        }

        //        // Extract scopes
        //        var scopeString = GetStringValue(root, "scope");
        //        if (!string.IsNullOrWhiteSpace(scopeString))
        //        {
        //            tokenData.Scopes = scopeString.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        //        }

        //        // Store all claims as additional data
        //        var standardClaims = new HashSet<string> 
        //        { 
        //            "sub", "iss", "aud", "exp", "iat", "nbf", "user_id", "uid", 
        //            "username", "name", "email", "tenant_id", "tid", "business_unit", 
        //            "bu", "roles", "scope" 
        //        };

        //        foreach (var property in root.EnumerateObject())
        //        {
        //            if (!standardClaims.Contains(property.Name))
        //            {
        //                tokenData.AdditionalClaims[property.Name] = property.Value.ToString();
        //            }
        //        }

        //        return tokenData;
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error decoding JWT token: {ex.Message}");
        //        return tokenData;
        //    }
        //}

        //private static string? GetStringValue(JsonElement element, string propertyName)
        //{
        //    if (element.TryGetProperty(propertyName, out var property))
        //    {
        //        return property.GetString();
        //    }
        //    return null;
        //}

        //private static DateTime? GetDateTimeFromUnixTimestamp(JsonElement element, string propertyName)
        //{
        //    if (element.TryGetProperty(propertyName, out var property))
        //    {
        //        if (property.TryGetInt64(out var timestamp))
        //        {
        //            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        //        }
        //    }
        //    return null;
        //}
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
