using Entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IUserRepository : IRepository<Entity.User>
    {
        Task<(Guid?, Guid?, string)> GetUserDetails(Guid contactId);
        Task<bool> DoesUserExists(Guid contactId);
        Task<bool> DoesAddresssExists(Guid addressId);

        Task<Entity.User?> GetUser(string userEmailAddress);
    }
}
