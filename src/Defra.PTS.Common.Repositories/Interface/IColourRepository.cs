using Entity = Defra.PTS.Common.Entities;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IColourRepository : IRepository<Entity.Colour>
    {
        Task<Entity.Colour?> FindByName(string colourName);
        Task<Entity.Colour?> FindById(int colourId);
    }
}