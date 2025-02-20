using Entity = Defra.PTS.Common.Entities;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class BreedRepository : Repository<Entity.Breed>, IBreedRepository
    {
        private CommonDbContext? CommonContext
        {
            get { return _dbContext as CommonDbContext; }
        }

        public BreedRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Entity.Breed?> FindByName(string breedName)
        {
            return await CommonContext!.Breed
                .FirstOrDefaultAsync(b => b.Name!.ToLower() == breedName.ToLower());
        }

        public async Task<Entity.Breed?> FindById(int breedId)
        {
            return await CommonContext!.Breed.FindAsync(breedId);
        }

        public async Task<Entity.Breed?> FindByNameAndSpecies(string breedName, int speciesId)
        {
            return await CommonContext!.Breed
                .FirstOrDefaultAsync(b => b.Name!.ToLower() == breedName.ToLower() &&
                                        b.SpeciesId == speciesId);
        }

        public async Task<IEnumerable<Entity.Breed>> GetBreedsBySpecies(int speciesId)
        {
            return await CommonContext!.Breed
                .Where(b => b.SpeciesId == speciesId)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }
    }
}