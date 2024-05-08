using entity = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Defra.PTS.Common.Entities;
using Defra.PTS.Common.Models.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Repositories.Implementation
{
    [ExcludeFromCodeCoverage]
    public class AddressRepository : Repository<entity.Address>, IAddressRepository
    {
        private CommonDbContext addressContext
        {
            get
            {
                return _dbContext as CommonDbContext;
            }
        }

        public AddressRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<entity.Address> GetAddress(Guid? addressId, AddressType addressType)
        {
           return await addressContext.Address.FirstOrDefaultAsync(a => a.Id == addressId && a.AddressType == addressType.ToString() && a.IsActive == true);
        }
    }
}
