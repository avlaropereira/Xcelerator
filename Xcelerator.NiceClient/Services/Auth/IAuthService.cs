using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcelerator.NiceClient.Models;

namespace Xcelerator.NiceClient.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates using the "password" grant type.
        /// </summary>
        /// <param name="basicAuthHeader">The base64 encoded string "Basic XXXXX"</param>
        /// <param name="username">NICE Username</param>
        /// <param name="password">NICE Password</param>
        /// <returns>The authentication token and base URL</returns>
        Task<AuthToken> AuthenticateAsync(string basicAuthHeader, string username, string password);
    }
}
    