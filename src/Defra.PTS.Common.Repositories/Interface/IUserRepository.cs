using entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IUserRepository : IRepository<entity.User>
    {
        Task<(Guid?, Guid?, string)> GetUserDetails(Guid contactId);
        Task<bool> DoesUserExists(Guid contactId);
        Task<bool> DoesAddresssExists(Guid addressId);

        Task<entity.User> GetUser(string userEmailAddress);
    }
}
