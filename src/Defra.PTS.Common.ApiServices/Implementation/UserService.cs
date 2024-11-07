using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Repositories.Interface;
using Entity = Defra.PTS.Common.Entities;
using Model = Defra.PTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Models.CustomException;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Entity.Address> _addressRepository;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


        public UserService(
            IUserRepository userRepository,
            IRepository<Entity.Address> addressRepository)
        {
            _userRepository = userRepository;
            _addressRepository = addressRepository;
        }

        public async Task<Guid> CreateUser(Model.User userModel)
        {
            var addressDB = new Entity.Address()
            {
                AddressLineOne = userModel?.Address?.AddressLineOne,
                AddressLineTwo = userModel?.Address?.AddressLineTwo,
                TownOrCity = userModel?.Address?.TownOrCity,
                County = userModel?.Address?.County,
                CountryName = userModel?.Address?.CountryName,
                PostCode = userModel?.Address?.PostCode,
                AddressType = AddressType.User.ToString(),
                IsActive = true,
                CreatedBy = userModel?.Address?.CreatedBy,
                CreatedOn = DateTime.Now
            };
            await _addressRepository.Add(addressDB);
            await _addressRepository.SaveChanges();

            var userDB = new Entity.User()
            {
                Email = userModel!.Email,
                FullName = userModel.FullName,
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                Role = userModel.Role,
                AddressId = addressDB.Id,
                Telephone = userModel.Telephone,
                ContactId = userModel.ContactId,
                Uniquereference = userModel.Uniquereference,
                SignInDateTime = userModel.SignInDateTime,
                SignOutDateTime = userModel.SignInDateTime,
                CreatedBy = userModel.CreatedBy,
                CreatedOn = DateTime.Now
            };

            await _userRepository.Add(userDB);
            await _userRepository.SaveChanges();

            return userDB.Id;
        }

        public async Task<bool> DoesUserExists(Guid contactId)
        {
            if (contactId == Guid.Empty)
            {
                throw new UserFunctionException("Invalid ContactId");
            }

            return await _userRepository.DoesUserExists(contactId);
        }

        public async Task<bool> DoesAddressExists(Guid addressId)
        {
            return await _userRepository.DoesAddresssExists(addressId);
        }

        public async Task<Model.User> GetUserModel(Stream userStream)
        {
            string user = await new StreamReader(userStream).ReadToEndAsync();

            try
            {
                Model.User? userModel = JsonSerializer.Deserialize<Model.User>(user, _jsonOptions);

                return userModel!;
            }
            catch
            {
                throw new UserFunctionException("Cannot create User as User Model Cannot be Deserialized");
            }
        }

        public async Task<Model.UserRequest> GetUserRequestModel(Stream userStream)
        {
            string userRequest = await new StreamReader(userStream).ReadToEndAsync();

            try
            {
                Model.UserRequest? userRequestModel = JsonSerializer.Deserialize<Model.UserRequest>(userRequest, _jsonOptions);

                return userRequestModel!;
            }
            catch
            {
                throw new UserFunctionException("Cannot create UserRequest as UserRequest Model Cannot be Deserialized");
            }
        }

        public (Guid?, Guid?, string) GetUserDetails(Guid contactId)
        {
            return _userRepository.GetUserDetails(contactId).Result;
        }

        public async Task<Guid> AddAddress(Model.UserRequest userRequestModel)
        {
            Entity.Address? addressDB = null;
            if (userRequestModel.Address != null)
            {
                addressDB = new Entity.Address()
                {
                    AddressLineOne = userRequestModel.Address.AddressLineOne,
                    AddressLineTwo = userRequestModel.Address.AddressLineTwo,
                    TownOrCity = userRequestModel.Address.TownOrCity,
                    County = userRequestModel.Address.County,
                    PostCode = userRequestModel.Address.PostCode,
                    AddressType = AddressType.User.ToString(),
                    IsActive = true,
                    CreatedBy = userRequestModel.Address.CreatedBy,
                    CreatedOn = DateTime.Now
                };
                await _addressRepository.Add(addressDB);
                await _addressRepository.SaveChanges();
            }

            return addressDB != null ? addressDB.Id : Guid.Empty;
        }

        public async Task<Guid> UpdateAddress(Model.UserRequest userRequestModel, Guid addressId)
        {
            Entity.Address? addressDB = await _addressRepository.Find(addressId);
            if (addressDB == null)
            {
                return await AddAddress(userRequestModel);
            }

            if (userRequestModel.Address != null)
            {
                addressDB.AddressLineOne = userRequestModel.Address.AddressLineOne;
                addressDB.AddressLineTwo = userRequestModel.Address.AddressLineTwo;
                addressDB.TownOrCity = userRequestModel.Address.TownOrCity;
                addressDB.County = userRequestModel.Address.County;
                addressDB.PostCode = userRequestModel.Address.PostCode;

                addressDB.UpdatedBy = userRequestModel.Address.UpdatedBy;
                addressDB.UpdatedOn = DateTime.Now;

                _addressRepository.Update(addressDB);
                await _addressRepository.SaveChanges();
            }

            return addressDB.Id;
        }

        public async Task<Guid> UpdateUser(string firstName, string lastName, string userEmail, string telephone, Guid? addressId)
        {

            var userDB = _userRepository.GetUser(userEmail).Result;
            userDB!.AddressId = addressId;
            userDB.Telephone = telephone;
            userDB.FirstName = firstName;
            userDB.LastName = lastName;
            userDB.FullName = firstName + " " + lastName;
            userDB.UpdatedOn = DateTime.Now;

            _userRepository.Update(userDB);
            await _userRepository.SaveChanges();

            return userDB.Id;
        }
    }
}
