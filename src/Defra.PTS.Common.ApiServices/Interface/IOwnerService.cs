using Model = Defra.PTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IOwnerService
    {
        Task<Model.Owner> GetOwnerModel(Stream ownerStream);
        Task<bool> DoesOwnerExists(string ownerEmail);
        Task<Guid> CreateOwner(Model.Owner ownerModel);
    }
}
