using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using entity = Defra.PTS.Common.Entities;
using model = Defra.PTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Repositories.Implementation;
using Defra.PTS.Common.Models.Helper;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class OwnerService : IOwnerService
    {
        private ILogger<OwnerService> _log;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IRepository<entity.Address> _addressRepository;
        public OwnerService(
            ILogger<OwnerService> log,
            IOwnerRepository ownerRepository,
            IRepository<entity.Address> addressRepository)
        {
            _log = log;
            _ownerRepository = ownerRepository;
            _addressRepository = addressRepository;
        }

        public async Task<Guid> CreateOwner(model.Owner ownerModel)
        {
            var addressDB = new entity.Address()
            {
                AddressLineOne = ownerModel?.Address?.AddressLineOne,
                AddressLineTwo = ownerModel?.Address?.AddressLineTwo,
                TownOrCity = ownerModel?.Address?.TownOrCity,
                County = ownerModel?.Address?.County,
                CountryName = ownerModel?.Address?.CountryName,
                PostCode = ownerModel?.Address?.PostCode,
                AddressType = AddressType.Owner.ToString(),
                IsActive = true,
                CreatedBy = ownerModel?.Address?.CreatedBy,
                CreatedOn = DateTime.Now
            };

            await _addressRepository.Add(addressDB);
            await _addressRepository.SaveChanges();

            OwnerType ownerType = (OwnerType)Enum.Parse(typeof(OwnerType), ownerModel!.OwnerTypeId.ToString());
            var ownerDB = new entity.Owner()
            {
                Email = ownerModel.Email,
                FullName = ownerModel.FullName,
                Telephone = ownerModel.Telephone,
                AddressId = addressDB.Id,
                CreatedBy = ownerModel.CreatedBy,
                CreatedOn = DateTime.Now
            };
            await _ownerRepository.Add(ownerDB);
            await _ownerRepository.SaveChanges();

            return ownerDB.Id;
        }

        public async Task<bool> DoesOwnerExists(string ownerEmail)
        {
            if (string.IsNullOrEmpty(ownerEmail))
            {
                throw new UserFunctionException("Invalid Owner Email Address");
            }

            return await _ownerRepository.DoesOwnerExists(ownerEmail);
        }

        public async Task<model.Owner> GetOwnerModel(Stream ownerStream)
        {
            string owner = await new StreamReader(ownerStream).ReadToEndAsync();
            try
            {
                model.Owner? ownerModel = JsonSerializer.Deserialize<model.Owner>(owner, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return ownerModel!;
            }

            catch
            {
                throw new UserFunctionException("Cannot create Owner as Owner Model Cannot be Deserialized");
            }
        }
    }
}
