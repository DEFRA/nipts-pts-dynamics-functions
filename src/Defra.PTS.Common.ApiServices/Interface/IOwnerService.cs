using model = Defra.PTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IOwnerService
    {
        Task<model.Owner> GetOwnerModel(Stream userStream);
        Task<bool> DoesOwnerExists(string ownerEmail);
        Task<Guid> CreateOwner(model.Owner ownerModel);
    }
}
