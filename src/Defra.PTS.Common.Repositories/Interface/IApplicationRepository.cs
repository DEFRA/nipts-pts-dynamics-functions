using Entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IApplicationRepository : IRepository<Entity.Application>
    {
        Task<Entity.Application?> GetApplicationById(Guid applicationId);

        Task<bool> PerformHealthCheckLogic();
    }
}
