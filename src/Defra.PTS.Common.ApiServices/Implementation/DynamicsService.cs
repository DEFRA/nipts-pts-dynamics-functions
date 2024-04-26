using Defra.PTS.Common.ApiServices.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    [ExcludeFromCodeCoverage]
    public class DynamicsService : IDynamicsService
    {
        private readonly ILogger<DynamicsService> _log;
        private readonly IConfidentialClientApplication _confidentialClient;
        private readonly IKeyVaultAccess _keyVaultAccess;
        public DynamicsService(
              IConfidentialClientApplication confidentialClient
            , IKeyVaultAccess keyVaultAccess
            , ILogger<DynamicsService> log) 
        {
            _confidentialClient = confidentialClient;
            _keyVaultAccess = keyVaultAccess;
            _log = log;
        }

        
        public async Task<string> GetTokenForClient(string[] values)
        {
            var authResult = await _confidentialClient.AcquireTokenForClient(values).ExecuteAsync();
            
            return authResult.AccessToken;
        }

        public async Task<bool> PerformHealthCheckLogic()
        {
            try
            {
                string serviceUrl = await _keyVaultAccess.GetSecretAsync("Pts-Dynamics-Tenant-ServiceUrl");
                var authResult = await _confidentialClient.AcquireTokenForClient(new[] { $"{serviceUrl}/.default" }).ExecuteAsync();

                if(authResult != null &&
                    authResult.TokenType == "Bearer" &&
                    !string.IsNullOrEmpty(authResult.AccessToken)) 
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Dynamics Health Check Error: ", ex.Message);
                return false;
            }            
        }
    }
}
