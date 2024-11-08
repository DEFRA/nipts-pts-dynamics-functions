using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Defra.PTS.Common.Models;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IUserService
    {
        Task<Model.User> GetUserModel(Stream userStream);
        Task<Model.UserRequest> GetUserRequestModel(Stream userStream);
        (Guid?, Guid?, string) GetUserDetails(Guid contactId);
        Task<bool> DoesUserExists(Guid contactId);
        Task<bool> DoesAddressExists(Guid addressId);
        Task<Guid> CreateUser(Model.User userModel);
        Task<Guid> AddAddress(Model.UserRequest userRequestModel);
        Task<Guid> UpdateAddress(Model.UserRequest userRequestModel, Guid addressId);
        Task<Guid> UpdateUser(string firstName, string lastName, string userEmail, string telephone, Guid? addressId);
    }
}
