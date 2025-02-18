using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class IdcomsMappingValidator : IIdcomsMappingValidator
    {
        private readonly ILogger<IdcomsMappingValidator> _logger;
        private readonly IBreedRepository _breedRepository;
        private readonly IColourRepository _colourRepository;

        public IdcomsMappingValidator(
            ILogger<IdcomsMappingValidator> logger,
            IBreedRepository breedRepository,
            IColourRepository colourRepository)
        {
            _logger = logger;
            _breedRepository = breedRepository;
            _colourRepository = colourRepository;
        }

        public async Task<ValidationResult> ValidateMapping(OfflineApplicationQueueModel queueModel)
        {
            var result = new ValidationResult();

            if (queueModel == null)
            {
                result.AddError("Model", "Request model cannot be null");
                return result;
            }

            try
            {
                ValidateOwnerFields(queueModel, result);
                ValidateApplicantFields(queueModel, result);
                await ValidatePetFields(queueModel, result);
                ValidateAddressFields(queueModel.OwnerAddress, "Owner", result);
                if (queueModel.ApplicantAddress != null)
                {
                    ValidateAddressFields(queueModel.ApplicantAddress, "Applicant", result);
                }
                ValidateApplicationFields(queueModel, result);
                ValidateIDCOMSFormat(queueModel, result);
                ValidateTravelDocumentFields(queueModel, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error occurred");
                result.AddError("Unexpected validation error", ex.Message);
            }

            return result;
        }

        private static void ValidateApplicantFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Applicant.FullName))
            {
                result.AddError("ApplicantName", "Applicant name is required");
            }
            else if (model.Applicant.FullName.Length > 300)
            {
                result.AddError("ApplicantName", "Applicant name cannot exceed 300 characters");
            }

            if (string.IsNullOrEmpty(model.Applicant.Email))
            {
                result.AddError("ApplicantEmail", "Applicant email is required");
            }
            else
            {
                if (model.Applicant.Email.Length > 100)
                {
                    result.AddError("ApplicantEmail", "Applicant email cannot exceed 100 characters");
                }
                if (!IsValidEmail(model.Applicant.Email))
                {
                    result.AddError("ApplicantEmail", "Invalid email format");
                }
            }

            if (!string.IsNullOrEmpty(model.Applicant.Telephone))
            {
                if (model.Applicant.Telephone.Length > 50)
                {
                    result.AddError("ApplicantPhone", "Applicant phone number cannot exceed 50 characters");
                }
                if (!IsValidPhoneNumber(model.Applicant.Telephone))
                {
                    result.AddError("ApplicantPhone", "Invalid phone number format");
                }
            }

            if (!string.IsNullOrEmpty(model.Applicant.FirstName) && model.Applicant.FirstName.Length > 100)
            {
                result.AddError("ApplicantFirstName", "Applicant first name cannot exceed 100 characters");
            }

            if (!string.IsNullOrEmpty(model.Applicant.LastName) && model.Applicant.LastName.Length > 100)
            {
                result.AddError("ApplicantLastName", "Applicant last name cannot exceed 100 characters");
            }
        }

        private static void ValidateOwnerFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Owner.FullName))
            {
                result.AddError("OwnerName", "Owner name is required");
            }
            else if (model.Owner.FullName.Length > 300)
            {
                result.AddError("OwnerName", "Owner name cannot exceed 300 characters");
            }

            if (string.IsNullOrEmpty(model.Owner.Email))
            {
                result.AddError("OwnerEmail", "Owner email is required");
            }
            else
            {
                if (model.Owner.Email.Length > 100)
                {
                    result.AddError("OwnerEmail", "Owner email cannot exceed 100 characters");
                }
                if (!IsValidEmail(model.Owner.Email))
                {
                    result.AddError("OwnerEmail", "Invalid email format");
                }
            }

            if (!string.IsNullOrEmpty(model.Owner.Telephone))
            {
                if (model.Owner.Telephone.Length > 50)
                {
                    result.AddError("OwnerPhone", "Owner phone number cannot exceed 50 characters");
                }
                if (!IsValidPhoneNumber(model.Owner.Telephone))
                {
                    result.AddError("OwnerPhone", "Invalid phone number format");
                }
            }
        }

        private static void ValidateAddressFields(AddressInfo address, string addressType, ValidationResult result)
        {
            if (address == null)
            {
                result.AddError($"{addressType}Address", $"{addressType} address information is required");
                return;
            }

            if (string.IsNullOrEmpty(address.AddressLineOne))
            {
                result.AddError($"{addressType}AddressLineOne", $"{addressType} address line 1 is required");
            }
            else if (address.AddressLineOne.Length > 250)
            {
                result.AddError($"{addressType}AddressLineOne", $"{addressType} address line 1 cannot exceed 250 characters");
            }

            if (!string.IsNullOrEmpty(address.AddressLineTwo) && address.AddressLineTwo.Length > 250)
            {
                result.AddError($"{addressType}AddressLineTwo", $"{addressType} address line 2 cannot exceed 250 characters");
            }

            if (string.IsNullOrEmpty(address.TownOrCity))
            {
                result.AddError($"{addressType}TownOrCity", $"{addressType} town/city is required");
            }
            else if (address.TownOrCity.Length > 250)
            {
                result.AddError($"{addressType}TownOrCity", $"{addressType} town/city cannot exceed 250 characters");
            }

            if (!string.IsNullOrEmpty(address.County) && address.County.Length > 100)
            {
                result.AddError($"{addressType}County", $"{addressType} county cannot exceed 100 characters");
            }

            if (string.IsNullOrEmpty(address.PostCode))
            {
                result.AddError($"{addressType}PostCode", $"{addressType} postcode is required");
            }
            else
            {
                if (address.PostCode.Length > 20)
                {
                    result.AddError($"{addressType}PostCode", $"{addressType} postcode cannot exceed 20 characters");
                }
                if (!IsValidPostcode(address.PostCode))
                {
                    result.AddError($"{addressType}PostCode", $"Invalid {addressType.ToLower()} postcode format");
                }
            }
        }

        private async Task ValidatePetFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!Enum.IsDefined(typeof(PetSpeciesType), model.Pet.SpeciesId))
            {
                result.AddError("PetSpeciesId", $"Invalid species value: {model.Pet.SpeciesId}");
            }

            if (!Enum.IsDefined(typeof(PetGenderType), model.Pet.SexId))
            {
                result.AddError("PetSexId", $"Invalid sex value: {model.Pet.SexId}");
            }

            if (model.Pet.BreedId != 99 && model.Pet.BreedId != 100)
            {
                var breed = await _breedRepository.FindById(model.Pet.BreedId);
                if (breed == null)
                {
                    result.AddError("PetBreedId", $"Invalid breed id: {model.Pet.BreedId}");
                }
                else if (breed.SpeciesId != model.Pet.SpeciesId)
                {
                    result.AddError("PetBreedId", $"Breed {model.Pet.BreedId} is not valid for species {model.Pet.SpeciesId}");
                }
            }

            var colour = await _colourRepository.FindById(model.Pet.ColourId);
            if (colour == null)
            {
                result.AddError("PetColourId", $"Invalid colour id: {model.Pet.ColourId}");
            }
            else if ((model.Pet.ColourId == 11 || model.Pet.ColourId == 20 || model.Pet.ColourId == 29)
                     && string.IsNullOrEmpty(model.Pet.OtherColour))
            {
                result.AddError("OtherColour", "Other colour description is required for this colour selection");
            }

            if (string.IsNullOrEmpty(model.Pet.Name))
            {
                result.AddError("PetName", "Pet name is required");
            }
            else if (model.Pet.Name.Length > 300)
            {
                result.AddError("PetName", "Pet name cannot exceed 300 characters");
            }

            if (!string.IsNullOrEmpty(model.Pet.MicrochipNumber) &&
                !IsValidMicrochipFormat(model.Pet.MicrochipNumber))
            {
                result.AddError("MicrochipNumber", "Invalid microchip number format");
            }

            if (model.Pet.MicrochipNumber?.Length > 15)
            {
                result.AddError("MicrochipNumber", "Microchip number cannot exceed 15 characters");
            }

            if (!string.IsNullOrEmpty(model.Pet.AdditionalInfoMixedBreedOrUnknown)
                && model.Pet.AdditionalInfoMixedBreedOrUnknown.Length > 300)
            {
                result.AddError("AdditionalInfoMixedBreedOrUnknown", "Additional breed info cannot exceed 300 characters");
            }

            if (!string.IsNullOrEmpty(model.Pet.UniqueFeatureDescription)
                && model.Pet.UniqueFeatureDescription.Length > 300)
            {
                result.AddError("UniqueFeatureDescription", "Unique feature description cannot exceed 300 characters");
            }

            if (model.Pet.DOB.HasValue && model.Pet.DOB.Value > DateTime.UtcNow)
            {
                result.AddError("DOB", "Date of birth cannot be in the future");
            }

            if (model.Pet.MicrochippedDate.HasValue && model.Pet.MicrochippedDate.Value > DateTime.UtcNow)
            {
                result.AddError("MicrochippedDate", "Microchip date cannot be in the future");
            }
        }

        private static void ValidateApplicationFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Application.Status) ||
                model.Application.Status != Status.Authorised.ToString())
            {
                result.AddError("Status", "Status must be 'Authorised' for offline applications");
            }

            if (!model.Application.DateAuthorised.HasValue)
            {
                result.AddError("DateAuthorised", "Authorisation date is required for offline applications");
            }

            if (string.IsNullOrEmpty(model.Application.ReferenceNumber))
            {
                result.AddError("ReferenceNumber", "Reference number is required");
            }
            else
            {
                if (model.Application.ReferenceNumber.Length > 20)
                {
                    result.AddError("ReferenceNumber", "Reference number cannot exceed 20 characters");
                }
                if (!IsValidReferenceNumber(model.Application.ReferenceNumber))
                {
                    result.AddError("ReferenceNumber", "Invalid reference number format");
                }
            }

            if (model.Application.DateOfApplication > DateTime.UtcNow)
            {
                result.AddError("DateOfApplication", "Application date cannot be in the future");
            }

            if (model.Application.DateAuthorised.HasValue &&
                model.Application.DateAuthorised.Value < model.Application.DateOfApplication)
            {
                result.AddError("DateAuthorised", "Authorization date cannot be before application date");
            }

            if (!string.IsNullOrEmpty(model.Application.DynamicId) &&
                !Guid.TryParse(model.Application.DynamicId, out _))
            {
                result.AddError("DynamicId", "Invalid Dynamic ID format");
            }
        }

        private static void ValidateTravelDocumentFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model.Ptd == null)
            {
                result.AddError("PTD", "Travel document information is required");
                return;
            }

            if (string.IsNullOrEmpty(model.Ptd.DocumentReferenceNumber))
            {
                result.AddError("DocumentReferenceNumber", "Document reference number is required");
            }
            else
            {
                if (model.Ptd.DocumentReferenceNumber.Length > 20)
                {
                    result.AddError("DocumentReferenceNumber", "Document reference number cannot exceed 20 characters");
                }
                if (model.Ptd.DocumentReferenceNumber != model.Application.ReferenceNumber)
                {
                    result.AddError("DocumentReferenceNumber", "Document reference number must match application reference number");
                }
            }

            if (model.Ptd.ValidityEndDate.HasValue && model.Ptd.ValidityStartDate.HasValue &&
                model.Ptd.ValidityEndDate.Value < model.Ptd.ValidityStartDate.Value)
            {
                result.AddError("ValidityEndDate", "Validity end date cannot be before start date");
            }

            if (model.Ptd.IssuingAuthorityId.HasValue && model.Ptd.IssuingAuthorityId.Value <= 0)
            {
                result.AddError("IssuingAuthorityId", "Issuing authority ID must be a positive number");
            }
        }

        private static void ValidateIDCOMSFormat(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!(model.Application.ReferenceNumber.StartsWith("GB", StringComparison.OrdinalIgnoreCase) ||
                  model.Application.ReferenceNumber.StartsWith("AD", StringComparison.OrdinalIgnoreCase)) ||
                !Regex.IsMatch(model.Application.ReferenceNumber, @"^(GB|AD)\d{8}$"))
            {
                result.AddError("ReferenceNumber", "Reference must start with 'GB' or 'AD' followed by 8 digits");
            }

            if (!string.IsNullOrWhiteSpace(model.Owner.FullName) &&
                (model.Owner.FullName.Length < 2 || model.Owner.FullName.Length > 300))
            {
                result.AddError("OwnerName", "Owner name must be between 2 and 300 characters");
            }

            if (!string.IsNullOrWhiteSpace(model.Pet.Name) &&
                (model.Pet.Name.Length < 2 || model.Pet.Name.Length > 300))
            {
                result.AddError("PetName", "Pet name must be between 2 and 300 characters");
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPhoneNumber(string phone)
        {
            return Regex.IsMatch(phone, @"^\+?[\d\s-]{10,}$");
        }

        private static bool IsValidPostcode(string postcode)
        {
            return Regex.IsMatch(postcode.ToUpper(), @"^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$");
        }

        private static bool IsValidMicrochipFormat(string microchip)
        {
            return Regex.IsMatch(microchip, @"^\d{15}$");
        }

        private static bool IsValidReferenceNumber(string reference)
        {
            return Regex.IsMatch(reference, @"^(GB|AD)\d{8}$", RegexOptions.IgnoreCase);
        }
    }
}