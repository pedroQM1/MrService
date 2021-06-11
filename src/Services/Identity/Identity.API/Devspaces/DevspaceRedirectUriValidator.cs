using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;

namespace Identity.API.Devspaces
{
    public class DevspaceRedirectUriValidator : IRedirectUriValidator
    {
        private readonly ILogger _logger;

        public DevspaceRedirectUriValidator(ILogger<DevspaceRedirectUriValidator> logger){
            _logger = logger;
        }

        public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            _logger.LogInformation("Client {ClientName} used post logout uri {RequestUri}.",client.ClientName,requestedUri);
            return Task.FromResult(true);
        }

        public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            _logger.LogInformation("Client {ClienteName} ised post logout uri {RequestedUri}.",client.ClientName,requestedUri);
            return Task.FromResult(true);
        }
    }
}