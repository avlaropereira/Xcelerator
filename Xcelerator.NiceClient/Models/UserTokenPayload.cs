namespace Xcelerator.NiceClient.Models
{
    /// <summary>
    /// Contains decoded JWT token claims and user information
    /// </summary>
    using System.Text.Json.Serialization;

    public class UserTokenPayload
    {
        [JsonPropertyName("role")]
        public RoleInfo Role { get; set; }

        [JsonPropertyName("views")]
        public Dictionary<string, object> Views { get; set; } // Empty in example, but usually a Dict

        [JsonPropertyName("icSPId")]
        public string IcSPId { get; set; }

        [JsonPropertyName("icAgentId")]
        public string IcAgentId { get; set; }

        [JsonPropertyName("sub")]
        public string Subject { get; set; }

        [JsonPropertyName("iss")]
        public string Issuer { get; set; }

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; }

        [JsonPropertyName("aud")]
        public string Audience { get; set; }

        [JsonPropertyName("icBUId")]
        public long IcBUId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; }

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; }

        [JsonPropertyName("tenant")]
        public string Tenant { get; set; }

        [JsonPropertyName("icClusterId")]
        public string IcClusterId { get; set; }

        [JsonPropertyName("securityContextId")]
        public string SecurityContextId { get; set; }

        [JsonPropertyName("iat")]
        public long IssuedAt { get; set; }

        [JsonPropertyName("exp")]
        public long Expiration { get; set; }
    }

    public class RoleInfo
    {
        [JsonPropertyName("legacyId")]
        public string LegacyId { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; } // Using Guid type for automatic validation

        [JsonPropertyName("lastUpdateTime")]
        public long LastUpdateTime { get; set; }

        [JsonPropertyName("secondaryRoles")]
        public List<string> SecondaryRoles { get; set; }
    }
}
