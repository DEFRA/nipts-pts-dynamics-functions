using Entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IOwnerRepository : IRepository<Entity.Owner>
    {
        Task<bool> DoesOwnerExists(string ownerEmailAddress);

        Task<Entity.Owner?> GetOwner(Guid ownerId);
    }
}
