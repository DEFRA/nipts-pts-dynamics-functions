using Entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Defra.PTS.Common.Entities;
using Defra.PTS.Common.Models.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class PetRepository : Repository<Entity.Pet>, IPetRepository
    {
        private CommonDbContext petContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public PetRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
