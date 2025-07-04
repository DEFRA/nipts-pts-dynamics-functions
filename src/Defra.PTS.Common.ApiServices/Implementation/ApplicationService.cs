﻿using Defra.PTS.Common.ApiServices.Interface;
using Models = Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;
using Defra.PTS.Common.Models.Helper;
using Defra.PTS.Common.Repositories.Implementation;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using System.Text.Json;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly IPetRepository _petRepository;
        private readonly IBreedRepository _breedRepository;
        private readonly IColourRepository _colourRepository;
        private readonly ITravelDocumentRepository _travelDocumentRepository;
        private readonly IUserRepository _userRepository;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


        public ApplicationService(
              IApplicationRepository applicationRepository
            , IOwnerRepository ownerRepository
            , IAddressRepository addressRepository
            , IPetRepository petRepository
            , IBreedRepository breedRepository
            , IColourRepository colourRepository
            , ITravelDocumentRepository travelDocumentRepository
            , IUserRepository userRepository)
        {
            _applicationRepository = applicationRepository;
            _ownerRepository = ownerRepository;
            _addressRepository = addressRepository;
            _petRepository = petRepository;
            _breedRepository = breedRepository;
            _colourRepository = colourRepository;
            _travelDocumentRepository = travelDocumentRepository;
            _userRepository = userRepository;
        }

        public async Task<Models.Application> GetApplication(Guid applicationId)
        {
            var payloadObject = new Models.Application();
            var application = await _applicationRepository.Find(applicationId);

            if (application != null)
            {
                var owner = await _ownerRepository.Find(application.OwnerId);
                var address = await _addressRepository.GetAddress(application.OwnerAddressId, AddressType.Owner);
                var pet = await _petRepository.Find(application.PetId);
                var travelDocumentReference = await _travelDocumentRepository.GetTravelDocument(application.Id, owner!.Id, pet!.Id);
                var user = await _userRepository.Find(application.UserId);   

                payloadObject.NiptsApplicantId = "contacts(" + user!.ContactId.ToString() + ")";
                payloadObject.NiptsPortalApplicationId = applicationId.ToString();
                payloadObject.NiptsApplicationReference = application.ReferenceNumber;
                payloadObject.NiptsDocumentReference = travelDocumentReference?.DocumentReferenceNumber;
                payloadObject.NiptsSubmissionDate = application.DateOfApplication.ToString("s") + "Z";

                payloadObject.NiptsOwnerType = GetDynamicsOwnerType();
                payloadObject.NiptsOwnerName = application.OwnerNewName;
                payloadObject.NiptsOwnerEmail = owner.Email;
                payloadObject.NiptsOwnerphone = application.OwnerNewTelephone;
                payloadObject.NiptsCharityName = string.Empty;


                if (address != null)
                {
                    payloadObject.NiptsOwnerAddressLine1 = address.AddressLineOne;
                    payloadObject.NiptsOwnerAddressLine2 = address.AddressLineTwo;
                    payloadObject.NiptsOwnerTown = address.TownOrCity;
                    payloadObject.NiptsOwnerPostcode = address.PostCode;
                    payloadObject.NiptsOwnerCounty = address.County;
                    payloadObject.NiptsOwnerCountry = address!.CountryName!;
                }

                PetSpeciesType petSpecies = (PetSpeciesType)Enum.Parse(typeof(PetSpeciesType), pet.SpeciesId.ToString());
                PetGenderType petGender = (PetGenderType)Enum.Parse(typeof(PetGenderType), pet.SexId.ToString());     
                
                payloadObject.NiptsPetname = pet.Name;
                payloadObject.NiptsPetSpecies = petSpecies.GetDescription();
                payloadObject.NiptsPetSex = petGender.GetDescription(); 
                payloadObject.NiptsPetDob = pet.DOB?.ToString("yyyy-MM-dd");                  
                payloadObject.NiptsPetUniqueFeatures = pet.UniqueFeatureDescription;
                payloadObject.NiptsMicrochipnum = pet.MicrochipNumber;
                payloadObject.NiptsMicrochippedDate = pet.MicrochippedDate?.ToString("yyyy-MM-dd");

                var breed = await _breedRepository.Find(pet.BreedId!);
                if (breed != null)
                {
                    payloadObject.NiptsPetBreed = breed.Name;
                    if (!string.IsNullOrEmpty(pet.AdditionalInfoMixedBreedOrUnknown))
                    {
                        payloadObject.NiptsPetBreed = pet.AdditionalInfoMixedBreedOrUnknown;
                    }
                }

                var colour = await _colourRepository.Find(pet.ColourId);
                if (colour != null)
                {
                    payloadObject.NiptsPetColour = colour.Name;
                    if(!string.IsNullOrEmpty(pet.OtherColour))
                    {
                        payloadObject.NiptsPetOtherColour = pet.OtherColour;
                    }                        
                }
            }
            return payloadObject;
        }

        public async Task<Guid?> UpdateApplicationStatus(ApplicationUpdateQueueModel applicationUpdateQueueModel)
        {
            Guid? id = null;
            Guid? applicationId = applicationUpdateQueueModel.Id;

            var application = await _applicationRepository.Find(applicationId!);

            if (application == null)
            {
                application = await _applicationRepository.GetApplicationByDynamicId(applicationUpdateQueueModel.DynamicId);
            }

            if (application != null)
            {
                var status = (Models.Enums.Status)Enum.Parse(typeof(Models.Enums.Status), applicationUpdateQueueModel.StatusId!);

                //Check if application is suspended before the update runs
                if (application.Status == Models.Enums.Status.Suspended.ToString()) 
                { 
                    //if the current status is now not suspended, we update the DateUnsuspended
                    if (status != Models.Enums.Status.Suspended)
                    {
                        application.DateUnsuspended = DateTime.Now;
                    }
                }

                else
                {
                    //if the current status is now suspended, we update the DateSuspended
                    if (status == Models.Enums.Status.Suspended)
                    {
                        application.DateSuspended = DateTime.Now;
                    }
                }

                application.DynamicId = applicationUpdateQueueModel.DynamicId;
                application.Status = status.ToString();
                application.DateAuthorised = applicationUpdateQueueModel.DateAuthorised;
                application.DateRejected = applicationUpdateQueueModel.DateRejected;
                application.DateRevoked = applicationUpdateQueueModel.DateRevoked;
                application.UpdatedBy = applicationUpdateQueueModel.Id;
                application.UpdatedOn = DateTime.Now;
                _applicationRepository.Update(application);
                await _applicationRepository.SaveChanges();
                id = application.Id;
            }
            return id;
        }

        private static string GetDynamicsOwnerType()
        {
            return ((int)DynamicsOwnerType.Self).ToString();
        }

        public async Task<bool> PerformHealthCheckLogic()
        {
            return await _applicationRepository.PerformHealthCheckLogic();
        }

        public async Task<ApplicationSubmittedMessageQueueModel> GetApplicationQueueModel(Stream applicationStream)
        {
            string application = await new StreamReader(applicationStream).ReadToEndAsync();
            try
            {
                Models.ApplicationSubmittedMessageQueueModel? applicationSubmittedMessageQueueModel = JsonSerializer.Deserialize<Models.ApplicationSubmittedMessageQueueModel>(application, _jsonOptions);
                return applicationSubmittedMessageQueueModel!;
            }

            catch
            {
                throw new UserFunctionException("Cannot create ApplicationQueueMessage as ApplicationSubmittedMessageQueueModel Model Cannot be Deserialized");
            }
        }
    }
}
