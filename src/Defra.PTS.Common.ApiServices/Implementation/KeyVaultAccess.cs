using Azure.Security.KeyVault.Secrets;
using Defra.PTS.Common.ApiServices.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    [ExcludeFromCodeCoverage]
    public class KeyVaultAccess : IKeyVaultAccess
    {
        private readonly SecretClient _secretClient;
        public KeyVaultAccess(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }


        public async Task<string> GetSecretAsync(string key)
        {
            KeyVaultSecret keyVaultServiceUrl = await _secretClient.GetSecretAsync(key);
           return keyVaultServiceUrl.Value.ToString();
        }
    }
}
