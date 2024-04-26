using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using model = Defra.PTS.Common.Models;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IUserService
    {
        Task<model.User> GetUserModel(Stream userStream);
        Task<model.UserRequest> GetUserRequestModel(Stream userStream);
        (Guid?, Guid?, string) GetUserDetails(Guid contactId);
        Task<bool> DoesUserExists(Guid contactId);
        Task<bool> DoesAddressExists(Guid addressId);
        Task<Guid> CreateUser(model.User userModel);
        Task<Guid> AddAddress(model.UserRequest userRequestModel);
        Task<Guid> UpdateAddress(model.UserRequest userRequestModel, Guid addressId);
        Task<Guid> UpdateUser(string firstName, string lastName, string userEmail, string telephone, Guid? addressId);
    }
}
