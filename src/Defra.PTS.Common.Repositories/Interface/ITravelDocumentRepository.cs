using entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface ITravelDocumentRepository :  IRepository<entity.TravelDocument>
    {
        Task<entity.TravelDocument> GetTravelDocument(Guid? applicationId, Guid? ownerId, Guid? petId);
    }
}
