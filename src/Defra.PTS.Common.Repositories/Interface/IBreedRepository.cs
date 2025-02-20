using Entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IBreedRepository : IRepository<Entity.Breed>
    {
        Task<Entity.Breed?> FindByName(string breedName);
        Task<Entity.Breed?> FindById(int breedId);
        Task<Entity.Breed?> FindByNameAndSpecies(string breedName, int speciesId);
        Task<IEnumerable<Entity.Breed>> GetBreedsBySpecies(int speciesId);
    }
}