using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using System.Transactions;
using Entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class OfflineApplicationServiceOptions
    {
        public required IApplicationRepository ApplicationRepository { get; set; }
        public required IOwnerRepository OwnerRepository { get; set; }
        public required IAddressRepository AddressRepository { get; set; }
        public required IPetRepository PetRepository { get; set; }
        public required IUserRepository UserRepository { get; set; }
        public required ITravelDocumentRepository TravelDocumentRepository { get; set; }
        public required IdcomsMappingValidator MappingValidator { get; set; }
        public required IBreedRepository BreedRepository { get; set; }
        public required ILogger<OfflineApplicationService> Logger { get; set; }
    }

    public class OfflineApplicationService : IOfflineApplicationService
    {
        private readonly OfflineApplicationServiceOptions _options;
        private const string PROCESSING_START_MESSAGE = "Processing offline application {ReferenceNumber}";
        private const string PROCESSING_COMPLETE_MESSAGE = "Completed processing application {ReferenceNumber}";

        // Field length constants
        private const int MAX_NAME_LENGTH = 300;
        private const int MAX_EMAIL_LENGTH = 100;
        private const int MAX_PHONE_LENGTH = 50;
        private const int MAX_ADDRESS_LINE_LENGTH = 250;
        private const int MAX_COUNTY_LENGTH = 100;
        private const int MAX_POSTCODE_LENGTH = 20;
        private const int MAX_REFERENCE_LENGTH = 20;
        private const int MAX_MICROCHIP_LENGTH = 15;
        private const int MIXED_BREED_ID = 99;
        private const int UNKNOWN_BREED_ID = 100;
        private const int OTHER_COLOR_ID_1 = 11;
        private const int OTHER_COLOR_ID_2 = 20;
        private const int OTHER_COLOR_ID_3 = 29;

        public OfflineApplicationService(OfflineApplicationServiceOptions options)
        {
            _options = options;
        }

        public void ProcessOfflineApplication(OfflineApplicationQueueModel queueModel)
        {
            var validationResult = _options.MappingValidator.ValidateMapping(queueModel);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => $"{e.Field}: {e.Message}"));
                _options.Logger.LogError("Validation failed for application {ReferenceNumber}. Errors: {Errors}",
                    queueModel.Application.ReferenceNumber, errors);
                throw new OfflineApplicationProcessingException($"Validation failed: {errors}");
            }

            using var scope = new TransactionScope(TransactionScopeOption.Required);
            try
            {
                _options.Logger.LogInformation(PROCESSING_START_MESSAGE, queueModel.Application.ReferenceNumber);

                var ownerAddress = ProcessAddress(queueModel.OwnerAddress, "Owner");
                Entity.Address? applicantAddress = null;
                if (queueModel.ApplicantAddress != null)
                {
                    applicantAddress = ProcessAddress(queueModel.ApplicantAddress, "User");
                }

                var user = ProcessUser(queueModel);
                if (applicantAddress != null)
                {
                    user.AddressId = applicantAddress.Id;
                    _options.UserRepository.SaveChanges().Wait();
                }

                var owner = ProcessOwner(queueModel, ownerAddress);
                var pet = ProcessPet(queueModel);
                var application = ProcessApplication(queueModel, pet, owner, user, ownerAddress);
                ProcessTravelDocument(queueModel, application, owner, pet);

                scope.Complete();
                _options.Logger.LogInformation(PROCESSING_COMPLETE_MESSAGE, queueModel.Application.ReferenceNumber);
            }
            catch (Exception ex)
            {
                _options.Logger.LogError(ex, "Error processing application {ReferenceNumber}",
                    queueModel.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Failed to process application {queueModel.Application.ReferenceNumber}", ex);
            }
        }

        private Entity.User ProcessUser(OfflineApplicationQueueModel queueModel)
        {
            var user = _options.UserRepository.GetUser(queueModel.Applicant.Email).Result;
            if (user == null)
            {
                user = new Entity.User
                {
                    Id = Guid.NewGuid(),
                    Email = TruncateString(queueModel.Applicant.Email, MAX_EMAIL_LENGTH),
                    FullName = TruncateString(queueModel.Applicant.FullName, MAX_NAME_LENGTH),
                    FirstName = TruncateString(queueModel.Applicant.FirstName, MAX_EMAIL_LENGTH),
                    LastName = TruncateString(queueModel.Applicant.LastName, MAX_EMAIL_LENGTH),
                    Telephone = TruncateString(queueModel.Applicant.Telephone ?? string.Empty, MAX_PHONE_LENGTH),
                    Role = $"{Guid.NewGuid()}:Applicant:3",
                    ContactId = !string.IsNullOrEmpty(queueModel.Applicant.ContactId) ?
                        Guid.Parse(queueModel.Applicant.ContactId) : null,
                    Uniquereference = null,
                    SignInDateTime = null,
                    SignOutDateTime = null,
                    CreatedBy = null,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = null,
                    UpdatedOn = null
                };
                _options.UserRepository.Add(user).Wait();
                _options.UserRepository.SaveChanges().Wait();
            }
            return user;
        }

        private Entity.Address ProcessAddress(AddressInfo address, string addressType)
        {
            var newAddress = new Entity.Address
            {
                Id = Guid.NewGuid(),
                AddressLineOne = TruncateString(address.AddressLineOne, MAX_ADDRESS_LINE_LENGTH),
                AddressLineTwo = address.AddressLineTwo == "NULL" ? null : TruncateString(address.AddressLineTwo, MAX_ADDRESS_LINE_LENGTH),
                TownOrCity = TruncateString(address.TownOrCity, MAX_ADDRESS_LINE_LENGTH),
                County = TruncateString(address.County, MAX_COUNTY_LENGTH),
                PostCode = TruncateString(address.PostCode, MAX_POSTCODE_LENGTH),
                CountryName = null,
                AddressType = addressType,
                IsActive = true,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            _options.AddressRepository.Add(newAddress).Wait();
            _options.AddressRepository.SaveChanges().Wait();
            return newAddress;
        }

        private Entity.Owner ProcessOwner(OfflineApplicationQueueModel queueModel, Entity.Address ownerAddress)
        {
            var owner = new Entity.Owner
            {
                Id = Guid.NewGuid(),
                FullName = TruncateString(queueModel.Owner.FullName, MAX_NAME_LENGTH),
                Email = TruncateString(queueModel.Owner.Email, MAX_EMAIL_LENGTH),
                Telephone = TruncateString(queueModel.Owner.Telephone ?? string.Empty, MAX_PHONE_LENGTH),
                OwnerType = null,
                CharityName = null,
                AddressId = ownerAddress.Id,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            _options.OwnerRepository.Add(owner).Wait();
            _options.OwnerRepository.SaveChanges().Wait();
            return owner;
        }

        private Entity.Application ProcessApplication(
            OfflineApplicationQueueModel queueModel,
            Entity.Pet pet,
            Entity.Owner owner,
            Entity.User user,
            Entity.Address ownerAddress)
        {
            var application = new Entity.Application
            {
                Id = Guid.NewGuid(),
                PetId = pet.Id,
                OwnerId = owner.Id,
                UserId = user.Id,
                OwnerAddressId = ownerAddress.Id,
                OwnerNewName = TruncateString(queueModel.Owner.FullName, MAX_NAME_LENGTH),
                OwnerNewTelephone = TruncateString(queueModel.Owner.Telephone, MAX_PHONE_LENGTH),
                Status = Status.Authorised.ToString(),
                ReferenceNumber = TruncateString(queueModel.Application.ReferenceNumber, MAX_REFERENCE_LENGTH),
                DateOfApplication = queueModel.Application.DateOfApplication,
                DateAuthorised = queueModel.Application.DateAuthorised ?? DateTime.UtcNow,
                DateRejected = null,
                DateRevoked = null,
                DynamicId = Guid.TryParse(queueModel.Application.DynamicId, out var dynamicId) ? dynamicId : null,
                IsDeclarationSigned = false,
                IsConsentAgreed = false,
                IsPrivacyPolicyAgreed = false,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            _options.ApplicationRepository.Add(application).Wait();
            _options.ApplicationRepository.SaveChanges().Wait();
            return application;
        }

        private void ProcessTravelDocument(
            OfflineApplicationQueueModel queueModel,
            Entity.Application application,
            Entity.Owner owner,
            Entity.Pet pet)
        {
            var travelDocument = new Entity.TravelDocument
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                OwnerId = owner.Id,
                PetId = pet.Id,
                DocumentReferenceNumber = TruncateString(queueModel.Ptd.DocumentReferenceNumber, MAX_REFERENCE_LENGTH),
                QRCode = null,
                IsLifeTIme = true,
                ValidityStartDate = null,
                ValidityEndDate = null,
                StatusId = null,
                DateOfIssue = null,
                IssuingAuthorityId = null,
                DocumentSignedBy = null,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            _options.TravelDocumentRepository.Add(travelDocument).Wait();
            _options.TravelDocumentRepository.SaveChanges().Wait();
        }

        private Entity.Pet ProcessPet(OfflineApplicationQueueModel queueModel)
        {
            int? finalBreedId;
            string? additionalBreedInfo = null;

            if (queueModel.Pet.BreedId == MIXED_BREED_ID || queueModel.Pet.BreedId == UNKNOWN_BREED_ID)
            {
                finalBreedId = queueModel.Pet.BreedId;
                additionalBreedInfo = queueModel.Pet.AdditionalInfoMixedBreedOrUnknown;
            }
            else
            {
                var breed = _options.BreedRepository.FindById(queueModel.Pet.BreedId).Result;
                if (breed == null)
                {
                    throw new OfflineApplicationProcessingException($"Invalid breed id: {queueModel.Pet.BreedId}");
                }
                finalBreedId = breed.Id;
            }

            string? otherColour = null;
            if (queueModel.Pet.ColourId == OTHER_COLOR_ID_1 ||
                queueModel.Pet.ColourId == OTHER_COLOR_ID_2 ||
                queueModel.Pet.ColourId == OTHER_COLOR_ID_3)
            {
                otherColour = TruncateString(queueModel.Pet.OtherColour, MAX_NAME_LENGTH);
            }

            var pet = new Entity.Pet
            {
                Id = Guid.NewGuid(),
                IdentificationType = 1,
                Name = TruncateString(queueModel.Pet.Name, MAX_NAME_LENGTH),
                SpeciesId = queueModel.Pet.SpeciesId,
                BreedId = finalBreedId,
                BreedTypeId = 0,
                SexId = queueModel.Pet.SexId,
                IsDateOfBirthKnown = 0,
                DOB = queueModel.Pet.DOB,
                ApproximateAge = null,
                ColourId = queueModel.Pet.ColourId,
                OtherColour = otherColour,
                MicrochipNumber = TruncateString(queueModel.Pet.MicrochipNumber, MAX_MICROCHIP_LENGTH),
                MicrochippedDate = queueModel.Pet.MicrochippedDate,
                AdditionalInfoMixedBreedOrUnknown = finalBreedId == MIXED_BREED_ID || finalBreedId == UNKNOWN_BREED_ID
                    ? TruncateString(additionalBreedInfo ?? string.Empty, MAX_NAME_LENGTH)
                    : null,
                HasUniqueFeature = string.IsNullOrEmpty(queueModel.Pet.UniqueFeatureDescription) ? 2 : 1,
                UniqueFeatureDescription = string.IsNullOrEmpty(queueModel.Pet.UniqueFeatureDescription)
                    ? null
                    : TruncateString(queueModel.Pet.UniqueFeatureDescription, MAX_NAME_LENGTH),
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            _options.PetRepository.Add(pet).Wait();
            _options.PetRepository.SaveChanges().Wait();
            return pet;
        }

        private static string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.Length > maxLength ? input[..maxLength] : input;
        }
    }
}