using Entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface ITravelDocumentRepository :  IRepository<Entity.TravelDocument>
    {
        Task<Entity.TravelDocument?> GetTravelDocument(Guid? applicationId, Guid? ownerId, Guid? petId);
    }
}
