using Entity = Defra.PTS.Common.Entities;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class ColourRepository : Repository<Entity.Colour>, IColourRepository
    {
        private CommonDbContext? CommonContext
        {
            get { return _dbContext as CommonDbContext; }
        }

        public ColourRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Entity.Colour?> FindByName(string colourName)
        {
            return await CommonContext!.Colour
                .FirstOrDefaultAsync(c => c.Name!.ToLower() == colourName.ToLower());
        }

        public async Task<Entity.Colour?> FindById(int colourId)
        {
            return await CommonContext!.Colour.FindAsync(colourId);
        }
    }
}