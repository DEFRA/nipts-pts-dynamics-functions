using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IKeyVaultAccess
    {
        Task<string> GetSecretAsync(string key);
    }
}
