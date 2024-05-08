using entity = Defra.PTS.Common.Entities;
using Defra.PTS.Common.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Defra.PTS.Common.Models.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class TravelDocumentRepository : Repository<entity.TravelDocument>, ITravelDocumentRepository
    {
        private CommonDbContext travelDocumentContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public TravelDocumentRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<entity.TravelDocument> GetTravelDocument(Guid? applicationId, Guid? ownerId, Guid? petId)
        {
                return await travelDocumentContext.TravelDocument.FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.OwnerId == ownerId && a.PetId == petId);           
        }
    }
}
