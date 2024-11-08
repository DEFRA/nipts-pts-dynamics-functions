using Entity = Defra.PTS.Common.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Models.Enums;

namespace Defra.PTS.Common.Repositories.Interface
{
    public interface IAddressRepository : IRepository<Entity.Address>
    {
        Task<Entity.Address?> GetAddress(Guid? addressId, AddressType addressType);
    }
}
