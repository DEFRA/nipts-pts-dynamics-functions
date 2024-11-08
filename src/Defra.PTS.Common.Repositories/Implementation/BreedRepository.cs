using Entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class BreedRepository : Repository<Entity.Breed>, IBreedRepository
    {

        private CommonDbContext breedContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public BreedRepository(DbContext dbContext) : base(dbContext)
        {

        }
    }
}
