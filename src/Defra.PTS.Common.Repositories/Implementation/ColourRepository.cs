using entity = Defra.PTS.Common.Entities;
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
    public class ColourRepository : Repository<entity.Colour>, IColourRepository
    {
        private CommonDbContext colourContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public ColourRepository(DbContext dbContext) : base(dbContext)
        {

        }
    }
}
