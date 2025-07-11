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

    public class OfflineApplicationService(OfflineApplicationServiceOptions options) : IOfflineApplicationService
    {
        private readonly OfflineApplicationServiceOptions _options = options;
        private const string ProcessingStartMessage = "Processing offline application {ReferenceNumber}";
        private const string ProcessingCompleteMessage = "Completed processing application {ReferenceNumber}";

        public async Task ProcessOfflineApplication(OfflineApplicationQueueModel queueModel)
        {
            var validationResult = await _options.MappingValidator.ValidateMapping(queueModel);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => $"{e.Field}: {e.Message}"));
                _options.Logger.LogError("Validation failed for application {ReferenceNumber}. Errors: {Errors}",
                    queueModel.Application.ReferenceNumber, errors);
                throw new OfflineApplicationProcessingException($"Validation failed: {errors}");
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                _options.Logger.LogInformation(ProcessingStartMessage, queueModel.Application.ReferenceNumber);

                var ownerAddress = await ProcessAddress(queueModel.OwnerAddress, "Owner");
                Entity.Address? applicantAddress = null;
                if (queueModel.ApplicantAddress != null)
                {
                    applicantAddress = await ProcessAddress(queueModel.ApplicantAddress, "User");
                }

                var user = await ProcessUser(queueModel);
                if (applicantAddress != null)
                {
                    user.AddressId = applicantAddress.Id;
                    await _options.UserRepository.SaveChanges();
                }

                var owner = await ProcessOwner(queueModel, ownerAddress);
                var pet = await ProcessPet(queueModel);
                var application = await ProcessApplication(queueModel, pet, owner, user, ownerAddress);
                await ProcessTravelDocument(queueModel, application, owner, pet);

                scope.Complete();
                _options.Logger.LogInformation(ProcessingCompleteMessage, queueModel.Application.ReferenceNumber);
            }
            catch (Exception ex)
            {
                _options.Logger.LogError(ex, "Error processing application {ReferenceNumber}",
                    queueModel.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Failed to process application {queueModel.Application.ReferenceNumber}", ex);
            }
        }

        private async Task<Entity.User> ProcessUser(OfflineApplicationQueueModel queueModel)
        {
            var user = await _options.UserRepository.GetUser(queueModel.Applicant.Email);
            if (user == null)
            {
                user = new Entity.User
                {
                    Id = Guid.NewGuid(),
                    Email = TruncateString(queueModel.Applicant.Email, 100),
                    FullName = TruncateString(queueModel.Applicant.FullName, 300),
                    FirstName = TruncateString(queueModel.Applicant.FirstName, 100),
                    LastName = TruncateString(queueModel.Applicant.LastName, 100),
                    Telephone = TruncateString(queueModel.Applicant.Telephone ?? string.Empty, 50),
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
                await _options.UserRepository.Add(user);
                await _options.UserRepository.SaveChanges();
            }
            return user;
        }

        private async Task<Entity.Address> ProcessAddress(AddressInfo address, string addressType)
        {
            var newAddress = new Entity.Address
            {
                Id = Guid.NewGuid(),
                AddressLineOne = TruncateString(address.AddressLineOne, 250),
                AddressLineTwo = address.AddressLineTwo == "NULL" ? null : TruncateString(address.AddressLineTwo, 250),
                TownOrCity = TruncateString(address.TownOrCity, 250),
                County = TruncateString(address.County, 100),
                PostCode = TruncateString(address.PostCode, 20),
                CountryName = "United Kingdom", // Set programmatically
                AddressType = addressType,
                IsActive = true,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            await _options.AddressRepository.Add(newAddress);
            await _options.AddressRepository.SaveChanges();
            return newAddress;
        }

        private async Task<Entity.Owner> ProcessOwner(OfflineApplicationQueueModel queueModel, Entity.Address ownerAddress)
        {
            var owner = new Entity.Owner
            {
                Id = Guid.NewGuid(),
                FullName = TruncateString(queueModel.Owner.FullName, 300),
                Email = TruncateString(queueModel.Owner.Email, 100),
                Telephone = TruncateString(queueModel.Owner.Telephone ?? string.Empty, 50),
                OwnerType = null,
                CharityName = null,
                AddressId = ownerAddress.Id,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            await _options.OwnerRepository.Add(owner);
            await _options.OwnerRepository.SaveChanges();
            return owner;
        }

        private async Task<Entity.Application> ProcessApplication(
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
                OwnerNewName = TruncateString(queueModel.Owner.FullName, 300),
                OwnerNewTelephone = TruncateString(queueModel.Owner.Telephone, 50),
                Status = Status.Authorised.ToString(),
                ReferenceNumber = TruncateString(queueModel.Application.ReferenceNumber, 20),
                DateOfApplication = queueModel.Application.DateOfApplication,
                DateAuthorised = queueModel.Application.DateAuthorised ?? DateTime.UtcNow,
                DateRejected = null,
                DateRevoked = null,
                DateSuspended = null,
                DateUnsuspended = null,
                DynamicId = Guid.TryParse(queueModel.Application.DynamicId, out var dynamicId) ? dynamicId : null,
                IsDeclarationSigned = false,
                IsConsentAgreed = false,
                IsPrivacyPolicyAgreed = false,
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            await _options.ApplicationRepository.Add(application);
            await _options.ApplicationRepository.SaveChanges();
            return application;
        }

        private async Task ProcessTravelDocument(
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
                DocumentReferenceNumber = TruncateString(queueModel.Ptd.DocumentReferenceNumber, 20),
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

            await _options.TravelDocumentRepository.Add(travelDocument);
            await _options.TravelDocumentRepository.SaveChanges();
        }

        private async Task<Entity.Pet> ProcessPet(OfflineApplicationQueueModel queueModel)
        {
            int? finalBreedId = null;
            string? additionalBreedInfo = null;
           
            if (queueModel.Pet.SpeciesId == 3 && !queueModel.Pet.BreedId.HasValue)
            {
                finalBreedId = null;
            }
            else if (queueModel.Pet.BreedId.HasValue && (queueModel.Pet.BreedId == 99 || queueModel.Pet.BreedId == 100))
            {
                finalBreedId = queueModel.Pet.BreedId;
                additionalBreedInfo = queueModel.Pet.AdditionalInfoMixedBreedOrUnknown;
            }
            else if (queueModel.Pet.BreedId.HasValue)
            {
                var breed = await _options.BreedRepository.FindById(queueModel.Pet.BreedId.Value);
                finalBreedId = breed?.Id ?? throw new OfflineApplicationProcessingException($"Invalid breed id: {queueModel.Pet.BreedId}");
            }

            string? otherColour = null;
            if (queueModel.Pet.ColourId == 11 || queueModel.Pet.ColourId == 20 || queueModel.Pet.ColourId == 29)
            {
                otherColour = TruncateString(queueModel.Pet.OtherColour, 300);
            }

            var pet = new Entity.Pet
            {
                Id = Guid.NewGuid(),
                IdentificationType = 1,
                Name = TruncateString(queueModel.Pet.Name, 300),
                SpeciesId = queueModel.Pet.SpeciesId,
                BreedId = finalBreedId,
                BreedTypeId = 0,
                SexId = queueModel.Pet.SexId,
                IsDateOfBirthKnown = 0,
                DOB = queueModel.Pet.DOB,
                ApproximateAge = null,
                ColourId = queueModel.Pet.ColourId,
                OtherColour = otherColour,
                MicrochipNumber = TruncateString(queueModel.Pet.MicrochipNumber, 15),
                MicrochippedDate = queueModel.Pet.MicrochippedDate,
                AdditionalInfoMixedBreedOrUnknown = (finalBreedId == 99 || finalBreedId == 100)
                    ? TruncateString(additionalBreedInfo ?? string.Empty, 300)
                    : null,
                HasUniqueFeature = string.IsNullOrEmpty(queueModel.Pet.UniqueFeatureDescription) ? 2 : 1,
                UniqueFeatureDescription = string.IsNullOrEmpty(queueModel.Pet.UniqueFeatureDescription)
                    ? null
                    : TruncateString(queueModel.Pet.UniqueFeatureDescription, 300),
                CreatedBy = null,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = null,
                UpdatedOn = null
            };

            await _options.PetRepository.Add(pet);
            await _options.PetRepository.SaveChanges();
            return pet;
        }

        private static string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Length > maxLength ? input[..maxLength] : input;
        }
    }
}