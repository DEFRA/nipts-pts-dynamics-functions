using Entity = Defra.PTS.Common.Entities;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class OwnerRepository : Repository<Entity.Owner>, IOwnerRepository
    {

        private CommonDbContext? userContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public OwnerRepository(Microsoft.EntityFrameworkCore.DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> DoesOwnerExists(string ownerEmailAddress)
        {
           return await userContext!.Owner.AnyAsync(a => a.Email == ownerEmailAddress);
        }

        public async Task<Owner?> GetOwner(Guid ownerId)
        {
            return await userContext!.Owner.FirstOrDefaultAsync(a => a.Id == ownerId);
        }
    }
}
