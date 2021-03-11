using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Lounge.Web.Authentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly string apiUsername;
        private readonly string apiPassword;

        public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IConfiguration configuration) : base(options, logger, encoder, clock)
        {
            apiUsername = configuration["APICredentials:Username"];
            apiPassword = configuration["APICredentials:Password"];
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization") || !Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            try
            {
                var authenticationHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authenticationHeader.Parameter!);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];

                if (username == this.apiUsername && password == this.apiPassword)
                {
                    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, username) };
                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);
                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }

                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
            }
            catch
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
            }
        }
    }
}
