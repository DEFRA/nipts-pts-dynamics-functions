using Entity = Defra.PTS.Common.Entities;
using Defra.PTS.Common.Entities;
using Defra.PTS.Common.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class ApplicationRepository : Repository<Entity.Application>, IApplicationRepository
    {
        private CommonDbContext? CommonContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public ApplicationRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Entity.Application> GetApplicationById(Guid applicationId)
        {
            return await CommonContext.Application.FirstOrDefaultAsync(a => a.Id == applicationId);
        }

        public async Task<bool> PerformHealthCheckLogic()
        {
            // Attempt to open a connection to the database
            await _dbContext.Database.OpenConnectionAsync();

            // Check if the connection is open
            if (_dbContext.Database.GetDbConnection().State == ConnectionState.Open)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
