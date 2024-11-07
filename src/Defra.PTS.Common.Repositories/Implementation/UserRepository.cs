using Entity = Defra.PTS.Common.Entities;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class UserRepository : Repository<Entity.User>, IUserRepository
    {

        private CommonDbContext? userContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public UserRepository(Microsoft.EntityFrameworkCore.DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> DoesUserExists(Guid contactId)
        {
            var reu = userContext!.User!.FirstOrDefaultAsync();
           return await userContext!.User!.AnyAsync(a => a.ContactId == contactId);
        }

        public async Task<bool> DoesAddresssExists(Guid addressId)
        {
            return await userContext!.Address.AnyAsync(a => a.Id == addressId);
        }
        
        public async Task<(Guid?, Guid?, string)> GetUserDetails(Guid contactId)
        {
            var user = await userContext!.User!.FirstOrDefaultAsync(a => a.ContactId == contactId);
            return user != null ? (user.Id!, user.AddressId!, user.Email!) : (Guid.Empty, Guid.Empty, string.Empty);
        }

        public async Task<Entity.User?> GetUser(string userEmailAddress)
        {
            return await userContext.User.SingleOrDefaultAsync(a => a.Email == userEmailAddress);
        }
    }
}
