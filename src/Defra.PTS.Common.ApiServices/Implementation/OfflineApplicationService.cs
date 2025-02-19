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
        public RepositoryGroup Repositories { get; set; } = null!;
        public IdcomsMappingValidator MappingValidator { get; set; } = null!;
        public ILogger<OfflineApplicationService> Logger { get; set; } = null!;
    }

    public class RepositoryGroup
    {
        public IApplicationRepository ApplicationRepository { get; set; } = null!;
        public IOwnerRepository OwnerRepository { get; set; } = null!;
        public IAddressRepository AddressRepository { get; set; } = null!;
        public IPetRepository PetRepository { get; set; } = null!;
        public IUserRepository UserRepository { get; set; } = null!;
        public ITravelDocumentRepository TravelDocumentRepository { get; set; } = null!;
        public IBreedRepository BreedRepository { get; set; } = null!;
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
                ProcessValidatedApplication(queueModel);
                scope.Complete();
            }
            catch (ValidationException ex)
            {
                _options.Logger.LogError(ex, "Validation error processing application {ReferenceNumber}",
                    queueModel.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Validation error processing application {queueModel.Application.ReferenceNumber}", ex);
            }
            catch (RepositoryException ex)
            {
                _options.Logger.LogError(ex, "Repository error processing application {ReferenceNumber}",
                    queueModel.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Repository error processing application {queueModel.Application.ReferenceNumber}", ex);
            }
            catch (Exception ex)
            {
                _options.Logger.LogError(ex, "Unexpected error processing application {ReferenceNumber}",
                    queueModel.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Unexpected error processing application {queueModel.Application.ReferenceNumber}", ex);
            }
        }

        private void ProcessValidatedApplication(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Application == null) throw new ArgumentNullException(nameof(queueModel.Application));

            _options.Logger.LogInformation(PROCESSING_START_MESSAGE, queueModel.Application.ReferenceNumber);

            var ownerAddress = ProcessAddress(queueModel.OwnerAddress, "Owner");
            var applicantAddress = ProcessApplicantAddress(queueModel);
            var user = ProcessUserWithAddress(queueModel, applicantAddress);
            var owner = ProcessOwner(queueModel, ownerAddress);
            var pet = ProcessPet(queueModel);
            var application = ProcessApplication(queueModel, pet, owner, user, ownerAddress);
            ProcessTravelDocument(queueModel, application, owner, pet);

            _options.Logger.LogInformation(PROCESSING_COMPLETE_MESSAGE, queueModel.Application.ReferenceNumber);
        }

        private Entity.Address? ProcessApplicantAddress(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));

            return queueModel.ApplicantAddress != null
                ? ProcessAddress(queueModel.ApplicantAddress, "User")
                : null;
        }

        private Entity.User ProcessUserWithAddress(OfflineApplicationQueueModel queueModel, Entity.Address? applicantAddress)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));

            var user = ProcessUser(queueModel);
            if (applicantAddress != null)
            {
                user.AddressId = applicantAddress.Id;
                _options.Repositories.UserRepository.SaveChanges().GetAwaiter().GetResult();
            }
            return user;
        }


        private Entity.User ProcessUser(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Applicant == null) throw new ArgumentNullException(nameof(queueModel.Applicant));

            var user = _options.Repositories.UserRepository.GetUser(queueModel.Applicant.Email).GetAwaiter().GetResult();
            if (user == null)
            {
                user = CreateNewUser(queueModel);
                _options.Repositories.UserRepository.Add(user).GetAwaiter().GetResult();
                _options.Repositories.UserRepository.SaveChanges().GetAwaiter().GetResult();
            }
            return user;
        }

        private static Entity.User CreateNewUser(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Applicant == null) throw new ArgumentNullException(nameof(queueModel.Applicant));

            return new Entity.User
            {
                Id = Guid.NewGuid(),
                Email = TruncateString(queueModel.Applicant.Email, MAX_EMAIL_LENGTH),
                FullName = TruncateString(queueModel.Applicant.FullName, MAX_NAME_LENGTH),
                FirstName = TruncateString(queueModel.Applicant.FirstName, MAX_EMAIL_LENGTH),
                LastName = TruncateString(queueModel.Applicant.LastName, MAX_EMAIL_LENGTH),
                Telephone = TruncateString(queueModel.Applicant.Telephone ?? string.Empty, MAX_PHONE_LENGTH),
                Role = $"{Guid.NewGuid()}:Applicant:3",
                ContactId = !string.IsNullOrEmpty(queueModel.Applicant.ContactId) ? Guid.Parse(queueModel.Applicant.ContactId) : (Guid?)null,
                Uniquereference = null,
                SignInDateTime = null,
                SignOutDateTime = null,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };
        }


        private Entity.Address ProcessAddress(AddressInfo address, string addressType)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(addressType)) throw new ArgumentException("Address type cannot be null or empty", nameof(addressType));

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

            _options.Repositories.AddressRepository.Add(newAddress).GetAwaiter().GetResult();
            _options.Repositories.AddressRepository.SaveChanges().GetAwaiter().GetResult();
            return newAddress;
        }

        private Entity.Owner ProcessOwner(OfflineApplicationQueueModel queueModel, Entity.Address ownerAddress)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Owner == null) throw new ArgumentNullException(nameof(queueModel.Owner));
            if (ownerAddress == null) throw new ArgumentNullException(nameof(ownerAddress));

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

            _options.Repositories.OwnerRepository.Add(owner).GetAwaiter().GetResult();
            _options.Repositories.OwnerRepository.SaveChanges().GetAwaiter().GetResult();
            return owner;
        }


        private Entity.Application ProcessApplication(
    OfflineApplicationQueueModel queueModel,
    Entity.Pet pet,
    Entity.Owner owner,
    Entity.User user,
    Entity.Address ownerAddress)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Application == null) throw new ArgumentNullException(nameof(queueModel.Application));
            if (pet == null) throw new ArgumentNullException(nameof(pet));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (ownerAddress == null) throw new ArgumentNullException(nameof(ownerAddress));

            var application = CreateApplication(queueModel, pet, owner, user, ownerAddress);
            _options.Repositories.ApplicationRepository.Add(application).GetAwaiter().GetResult();
            _options.Repositories.ApplicationRepository.SaveChanges().GetAwaiter().GetResult();
            return application;
        }

        private Entity.Application CreateApplication(
            OfflineApplicationQueueModel queueModel,
            Entity.Pet pet,
            Entity.Owner owner,
            Entity.User user,
            Entity.Address ownerAddress)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Application == null) throw new ArgumentNullException(nameof(queueModel.Application));
            if (pet == null) throw new ArgumentNullException(nameof(pet));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (ownerAddress == null) throw new ArgumentNullException(nameof(ownerAddress));

            return new Entity.Application
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
        }


        private void ProcessTravelDocument(
     OfflineApplicationQueueModel queueModel,
     Entity.Application application,
     Entity.Owner owner,
     Entity.Pet pet)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (application == null) throw new ArgumentNullException(nameof(application));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (pet == null) throw new ArgumentNullException(nameof(pet));

            var travelDocument = CreateTravelDocument(queueModel, application, owner, pet);
            _options.Repositories.TravelDocumentRepository.Add(travelDocument).GetAwaiter().GetResult();
            _options.Repositories.TravelDocumentRepository.SaveChanges().GetAwaiter().GetResult();
        }

        private Entity.TravelDocument CreateTravelDocument(
            OfflineApplicationQueueModel queueModel,
            Entity.Application application,
            Entity.Owner owner,
            Entity.Pet pet)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (application == null) throw new ArgumentNullException(nameof(application));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (pet == null) throw new ArgumentNullException(nameof(pet));

            return new Entity.TravelDocument
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
        }


        private Entity.Pet ProcessPet(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Pet == null) throw new ArgumentNullException(nameof(queueModel.Pet));

            (int? finalBreedId, string additionalBreedInfo) = DeterminePetBreed(queueModel);
            string otherColour = DeterminePetColor(queueModel) ?? string.Empty;

            var pet = CreatePet(queueModel, finalBreedId, additionalBreedInfo, otherColour);
            _options.Repositories.PetRepository.Add(pet).GetAwaiter().GetResult();
            _options.Repositories.PetRepository.SaveChanges().GetAwaiter().GetResult();
            return pet;
        }

        private (int? breedId, string additionalInfo) DeterminePetBreed(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Pet == null) throw new ArgumentNullException(nameof(queueModel.Pet));

            if (queueModel.Pet.BreedId == MIXED_BREED_ID || queueModel.Pet.BreedId == UNKNOWN_BREED_ID)
            {
                return (queueModel.Pet.BreedId, queueModel.Pet.AdditionalInfoMixedBreedOrUnknown ?? string.Empty);
            }

            var breed = _options.Repositories.BreedRepository.FindById(queueModel.Pet.BreedId).GetAwaiter().GetResult();
            if (breed == null)
            {
                throw new OfflineApplicationProcessingException($"Invalid breed id: {queueModel.Pet.BreedId}");
            }
            return (breed.Id, string.Empty);
        }

        private static string DeterminePetColor(OfflineApplicationQueueModel queueModel)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Pet == null) throw new ArgumentNullException(nameof(queueModel.Pet));

            return (queueModel.Pet.ColourId == OTHER_COLOR_ID_1 ||
                    queueModel.Pet.ColourId == OTHER_COLOR_ID_2 ||
                    queueModel.Pet.ColourId == OTHER_COLOR_ID_3)
                ? TruncateString(queueModel.Pet.OtherColour ?? string.Empty, MAX_NAME_LENGTH)
                : string.Empty;
        }


        private Entity.Pet CreatePet(
      OfflineApplicationQueueModel queueModel,
      int? finalBreedId,
      string additionalBreedInfo,
      string otherColour)
        {
            if (queueModel == null) throw new ArgumentNullException(nameof(queueModel));
            if (queueModel.Pet == null) throw new ArgumentNullException(nameof(queueModel.Pet));

            return new Entity.Pet
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
                    ? TruncateString(additionalBreedInfo, MAX_NAME_LENGTH)
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
        }

        private static string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.Length > maxLength ? input.Substring(0, maxLength) : input;
        }

    }
}