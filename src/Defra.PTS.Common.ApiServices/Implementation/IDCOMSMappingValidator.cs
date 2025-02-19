using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class IdcomsMappingValidator : IIdcomsMappingValidator
    {
        // Field constants
        private const string REFERENCE_NUMBER_FIELD = "ReferenceNumber";
        private const string MODEL_FIELD = "Model";
        private const string OWNER_NAME_FIELD = "OwnerName";
        private const string OWNER_EMAIL_FIELD = "OwnerEmail";
        private const string OWNER_PHONE_FIELD = "OwnerPhone";
        private const string PET_NAME_FIELD = "PetName";
        private const string PET_BREED_ID_FIELD = "PetBreedId";
        private const string PET_SPECIES_ID_FIELD = "PetSpeciesId";
        private const string PET_SEX_ID_FIELD = "PetSexId";
        private const string PET_COLOR_ID_FIELD = "PetColorId";

        // Validation limits
        private const int MAX_NAME_LENGTH = 300;
        private const int MAX_EMAIL_LENGTH = 100;
        private const int MAX_PHONE_LENGTH = 50;
        private const int MAX_ADDRESS_LINE_LENGTH = 250;
        private const int MAX_COUNTY_LENGTH = 100;
        private const int MAX_POSTCODE_LENGTH = 20;
        private const int MAX_MICROCHIP_LENGTH = 15;
        private const int MIN_NAME_LENGTH = 2;

        // Special IDs
        private const int MIXED_BREED_ID = 99;
        private const int UNKNOWN_BREED_ID = 100;
        private const int OTHER_COLOR_ID_1 = 11;
        private const int OTHER_COLOR_ID_2 = 20;
        private const int OTHER_COLOR_ID_3 = 29;

        private readonly ILogger<IdcomsMappingValidator> _logger;
        private readonly IBreedRepository _breedRepository;
        private readonly IColourRepository _colourRepository;

        public IdcomsMappingValidator(
            ILogger<IdcomsMappingValidator> logger,
            IBreedRepository breedRepository,
            IColourRepository colourRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _breedRepository = breedRepository ?? throw new ArgumentNullException(nameof(breedRepository));
            _colourRepository = colourRepository ?? throw new ArgumentNullException(nameof(colourRepository));
        }


        public ValidationResult ValidateMapping(OfflineApplicationQueueModel queueModel)
        {
            var result = new ValidationResult();

            if (queueModel == null)
            {
                result.AddError(MODEL_FIELD, "Request model cannot be null");
                return result;
            }

            try
            {
                ValidateAllFields(queueModel, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error occurred");
                result.AddError("Unexpected validation error", "An unexpected error occurred during validation.");
            }

            return result;
        }


        private void ValidateAllFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            ValidateOwnerFields(model, result);
            ValidateApplicantFields(model, result);
            ValidatePetBreedInfo(model, result);
            ValidatePetColourInfo(model, result);
            ValidatePetBasicInfo(model, result);
            ValidateAddressFields(model.OwnerAddress, "Owner", result);

            if (model.ApplicantAddress != null)
            {
                ValidateAddressFields(model.ApplicantAddress, "Applicant", result);
            }

            ValidateBasicApplicationFields(model, result);
            ValidateApplicationDates(model, result);
            ValidateIDCOMSFormat(model, result);
            ValidateTravelDocumentFields(model, result);
        }

        private void ValidatePetBreedInfo(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Pet == null)
            {
                result.AddError("Pet", "Pet information is required");
                return;
            }

            if (!Enum.IsDefined(typeof(PetSpeciesType), model.Pet.SpeciesId))
            {
                result.AddError(PET_SPECIES_ID_FIELD, $"Invalid species value: {model.Pet.SpeciesId}");
                return;
            }

            if (model.Pet.BreedId != MIXED_BREED_ID && model.Pet.BreedId != UNKNOWN_BREED_ID)
            {
                var breed = _breedRepository.FindById(model.Pet.BreedId).Result;
                if (breed == null)
                {
                    result.AddError(PET_BREED_ID_FIELD, $"Invalid breed id: {model.Pet.BreedId}");
                }
                else if (breed.SpeciesId != model.Pet.SpeciesId)
                {
                    result.AddError(PET_BREED_ID_FIELD, $"Breed {model.Pet.BreedId} is not valid for species {model.Pet.SpeciesId}");
                }
            }
        }

        private void ValidatePetColourInfo(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Pet == null)
            {
                result.AddError("Pet", "Pet information is required");
                return;
            }

            var colour = _colourRepository.FindById(model.Pet.ColourId).Result;
            if (colour == null)
            {
                result.AddError(PET_COLOR_ID_FIELD, $"Invalid colour id: {model.Pet.ColourId}");
            }
            else if ((model.Pet.ColourId == OTHER_COLOR_ID_1 ||
                     model.Pet.ColourId == OTHER_COLOR_ID_2 ||
                     model.Pet.ColourId == OTHER_COLOR_ID_3) &&
                    string.IsNullOrEmpty(model.Pet.OtherColour))
            {
                result.AddError("OtherColour", "Other colour description is required for this colour selection");
            }
        }


        private static void ValidatePetBasicInfo(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Pet == null)
            {
                result.AddError("Pet", "Pet information is required");
                return;
            }

            if (!Enum.IsDefined(typeof(PetGenderType), model.Pet.SexId))
            {
                result.AddError(PET_SEX_ID_FIELD, $"Invalid sex value: {model.Pet.SexId}");
            }

            ValidatePetName(model, result);
            ValidatePetMicrochip(model, result);
            ValidatePetDates(model, result);
            ValidatePetAdditionalInfo(model, result);
        }

        private static void ValidatePetName(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model?.Pet?.Name))
            {
                result.AddError(PET_NAME_FIELD, "Pet name is required");
            }
            else if (model.Pet.Name.Length > MAX_NAME_LENGTH)
            {
                result.AddError(PET_NAME_FIELD, $"Pet name cannot exceed {MAX_NAME_LENGTH} characters");
            }
        }

        private static void ValidatePetMicrochip(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(model?.Pet?.MicrochipNumber))
            {
                if (!IsValidMicrochipFormat(model.Pet.MicrochipNumber))
                {
                    result.AddError("MicrochipNumber", "Invalid microchip number format");
                }
                if (model.Pet.MicrochipNumber.Length > MAX_MICROCHIP_LENGTH)
                {
                    result.AddError("MicrochipNumber", $"Microchip number cannot exceed {MAX_MICROCHIP_LENGTH} characters");
                }
            }
        }

        private static void ValidatePetDates(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Pet?.DOB.HasValue == true && model.Pet.DOB.Value > DateTime.UtcNow)
            {
                result.AddError("DOB", "Date of birth cannot be in the future");
            }

            if (model?.Pet?.MicrochippedDate.HasValue == true && model.Pet.MicrochippedDate.Value > DateTime.UtcNow)
            {
                result.AddError("MicrochippedDate", "Microchip date cannot be in the future");
            }
        }


        private static void ValidatePetAdditionalInfo(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(model?.Pet?.AdditionalInfoMixedBreedOrUnknown)
                && model.Pet.AdditionalInfoMixedBreedOrUnknown.Length > MAX_NAME_LENGTH)
            {
                result.AddError("AdditionalInfoMixedBreedOrUnknown", $"Additional breed info cannot exceed {MAX_NAME_LENGTH} characters");
            }

            if (!string.IsNullOrEmpty(model?.Pet?.UniqueFeatureDescription)
                && model.Pet.UniqueFeatureDescription.Length > MAX_NAME_LENGTH)
            {
                result.AddError("UniqueFeatureDescription", $"Unique feature description cannot exceed {MAX_NAME_LENGTH} characters");
            }
        }

        private static void ValidateBasicApplicationFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model?.Application?.Status) ||
                model.Application.Status != Status.Authorised.ToString())
            {
                result.AddError("Status", "Status must be 'Authorised' for offline applications");
            }

            if (!model?.Application?.DateAuthorised.HasValue == true)
            {
                result.AddError("DateAuthorised", "Authorisation date is required for offline applications");
            }

            if (model != null)
            {
                ValidateReferenceNumber(model, result);
                ValidateDynamicId(model, result);
            }
        }


        private static void ValidateReferenceNumber(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model?.Application?.ReferenceNumber))
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Reference number is required");
                return;
            }

            if (model.Application.ReferenceNumber.Length > MAX_MICROCHIP_LENGTH)
            {
                result.AddError(REFERENCE_NUMBER_FIELD, $"Reference number cannot exceed {MAX_MICROCHIP_LENGTH} characters");
            }

            if (!IsValidReferenceNumber(model.Application.ReferenceNumber))
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Invalid reference number format");
            }
        }


        private static void ValidateDynamicId(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(model?.Application?.DynamicId) &&
                !Guid.TryParse(model.Application.DynamicId, out _))
            {
                result.AddError("DynamicId", "Invalid Dynamic ID format");
            }
        }

        private static void ValidateApplicationDates(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Application?.DateOfApplication > DateTime.UtcNow)
            {
                result.AddError("DateOfApplication", "Application date cannot be in the future");
            }

            if (model?.Application?.DateAuthorised.HasValue == true &&
                model.Application.DateAuthorised.Value < model.Application.DateOfApplication)
            {
                result.AddError("DateAuthorised", "Authorization date cannot be before application date");
            }
        }

        private static void ValidateTravelDocumentFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Ptd == null)
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Travel document information is required");
                return;
            }

            ValidateTravelDocumentBasicFields(model, result);
        }

        private static void ValidateTravelDocumentBasicFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            ValidateTravelDocumentReference(model, result);
            ValidateTravelDocumentDates(model, result);
            ValidateTravelDocumentAuthority(model, result);
        }


        private static void ValidateTravelDocumentReference(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Ptd?.DocumentReferenceNumber == null)
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Document reference number is required");
                return;
            }

            if (model.Ptd.DocumentReferenceNumber.Length > MAX_MICROCHIP_LENGTH)
            {
                result.AddError(REFERENCE_NUMBER_FIELD, $"Document reference number cannot exceed {MAX_MICROCHIP_LENGTH} characters");
            }

            if (model.Ptd.DocumentReferenceNumber != model.Application.ReferenceNumber)
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Document reference number must match application reference number");
            }
        }

        private static void ValidateTravelDocumentDates(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Ptd?.ValidityEndDate.HasValue == true && model.Ptd.ValidityStartDate.HasValue &&
                model.Ptd.ValidityEndDate.Value < model.Ptd.ValidityStartDate.Value)
            {
                result.AddError("ValidityEndDate", "Validity end date cannot be before start date");
            }
        }

        private static void ValidateTravelDocumentAuthority(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Ptd?.IssuingAuthorityId.HasValue == true && model.Ptd.IssuingAuthorityId.Value <= 0)
            {
                result.AddError("IssuingAuthorityId", "Issuing authority ID must be a positive number");
            }
        }

        private static void ValidateIDCOMSFormat(OfflineApplicationQueueModel model, ValidationResult result)
        {
            ValidateIDCOMSReferenceNumber(model, result);
            ValidateIDCOMSNames(model, result);
        }


        private static void ValidateIDCOMSReferenceNumber(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Application?.ReferenceNumber == null)
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Reference number is required");
                return;
            }

            if (!(model.Application.ReferenceNumber.StartsWith("GB", StringComparison.OrdinalIgnoreCase) ||
                  model.Application.ReferenceNumber.StartsWith("AD", StringComparison.OrdinalIgnoreCase)) ||
                !Regex.IsMatch(model.Application.ReferenceNumber, @"^(GB|AD)\d{8}$"))
            {
                result.AddError(REFERENCE_NUMBER_FIELD, "Reference must start with 'GB' or 'AD' followed by 8 digits");
            }
        }

        private static void ValidateIDCOMSNames(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Owner?.FullName != null &&
                (model.Owner.FullName.Length < MIN_NAME_LENGTH || model.Owner.FullName.Length > MAX_NAME_LENGTH))
            {
                result.AddError(OWNER_NAME_FIELD, $"Owner name must be between {MIN_NAME_LENGTH} and {MAX_NAME_LENGTH} characters");
            }

            if (model?.Pet?.Name != null &&
                (model.Pet.Name.Length < MIN_NAME_LENGTH || model.Pet.Name.Length > MAX_NAME_LENGTH))
            {
                result.AddError(PET_NAME_FIELD, $"Pet name must be between {MIN_NAME_LENGTH} and {MAX_NAME_LENGTH} characters");
            }
        }

        private static void ValidateOwnerFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model?.Owner?.FullName == null)
            {
                result.AddError(OWNER_NAME_FIELD, "Owner name is required");
            }
            else if (model.Owner.FullName.Length > MAX_NAME_LENGTH)
            {
                result.AddError(OWNER_NAME_FIELD, $"Owner name cannot exceed {MAX_NAME_LENGTH} characters");
            }

            if (model?.Owner?.Email == null)
            {
                result.AddError(OWNER_EMAIL_FIELD, "Owner email is required");
            }
            else
            {
                if (model.Owner.Email.Length > MAX_EMAIL_LENGTH)
                {
                    result.AddError(OWNER_EMAIL_FIELD, $"Owner email cannot exceed {MAX_EMAIL_LENGTH} characters");
                }
                if (!IsValidEmail(model.Owner.Email))
                {
                    result.AddError(OWNER_EMAIL_FIELD, "Invalid email format");
                }
            }

            if (!string.IsNullOrEmpty(model?.Owner?.Telephone))
            {
                if (model.Owner.Telephone.Length > MAX_PHONE_LENGTH)
                {
                    result.AddError(OWNER_PHONE_FIELD, $"Owner phone number cannot exceed {MAX_PHONE_LENGTH} characters");
                }
                if (!IsValidPhoneNumber(model.Owner.Telephone))
                {
                    result.AddError(OWNER_PHONE_FIELD, "Invalid phone number format");
                }
            }
        }


        private static void ValidateApplicantFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Applicant.FullName))
            {
                result.AddError("ApplicantName", "Applicant name is required");
            }
            else if (model.Applicant.FullName.Length > MAX_NAME_LENGTH)
            {
                result.AddError("ApplicantName", $"Applicant name cannot exceed {MAX_NAME_LENGTH} characters");
            }

            if (string.IsNullOrEmpty(model.Applicant.Email))
            {
                result.AddError("ApplicantEmail", "Applicant email is required");
            }
            else
            {
                if (model.Applicant.Email.Length > MAX_EMAIL_LENGTH)
                {
                    result.AddError("ApplicantEmail", $"Applicant email cannot exceed {MAX_EMAIL_LENGTH} characters");
                }
                if (!IsValidEmail(model.Applicant.Email))
                {
                    result.AddError("ApplicantEmail", "Invalid email format");
                }
            }

            if (!string.IsNullOrEmpty(model.Applicant.Telephone))
            {
                if (model.Applicant.Telephone.Length > MAX_PHONE_LENGTH)
                {
                    result.AddError("ApplicantPhone", $"Applicant phone number cannot exceed {MAX_PHONE_LENGTH} characters");
                }
                if (!IsValidPhoneNumber(model.Applicant.Telephone))
                {
                    result.AddError("ApplicantPhone", "Invalid phone number format");
                }
            }

            if (!string.IsNullOrEmpty(model.Applicant.FirstName) && model.Applicant.FirstName.Length > MAX_EMAIL_LENGTH)
            {
                result.AddError("ApplicantFirstName", $"Applicant first name cannot exceed {MAX_EMAIL_LENGTH} characters");
            }

            if (!string.IsNullOrEmpty(model.Applicant.LastName) && model.Applicant.LastName.Length > MAX_EMAIL_LENGTH)
            {
                result.AddError("ApplicantLastName", $"Applicant last name cannot exceed {MAX_EMAIL_LENGTH} characters");
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
            else if (address.AddressLineOne.Length > MAX_ADDRESS_LINE_LENGTH)
            {
                result.AddError($"{addressType}AddressLineOne", $"{addressType} address line 1 cannot exceed {MAX_ADDRESS_LINE_LENGTH} characters");
            }

            if (!string.IsNullOrEmpty(address.AddressLineTwo) && address.AddressLineTwo.Length > MAX_ADDRESS_LINE_LENGTH)
            {
                result.AddError($"{addressType}AddressLineTwo", $"{addressType} address line 2 cannot exceed {MAX_ADDRESS_LINE_LENGTH} characters");
            }

            if (string.IsNullOrEmpty(address.TownOrCity))
            {
                result.AddError($"{addressType}TownOrCity", $"{addressType} town/city is required");
            }
            else if (address.TownOrCity.Length > MAX_ADDRESS_LINE_LENGTH)
            {
                result.AddError($"{addressType}TownOrCity", $"{addressType} town/city cannot exceed {MAX_ADDRESS_LINE_LENGTH} characters");
            }

            if (!string.IsNullOrEmpty(address.County) && address.County.Length > MAX_COUNTY_LENGTH)
            {
                result.AddError($"{addressType}County", $"{addressType} county cannot exceed {MAX_COUNTY_LENGTH} characters");
            }

            if (string.IsNullOrEmpty(address.PostCode))
            {
                result.AddError($"{addressType}PostCode", $"{addressType} postcode is required");
            }
            else
            {
                if (address.PostCode.Length > MAX_POSTCODE_LENGTH)
                {
                    result.AddError($"{addressType}PostCode", $"{addressType} postcode cannot exceed {MAX_POSTCODE_LENGTH} characters");
                }
                if (!IsValidPostcode(address.PostCode))
                {
                    result.AddError($"{addressType}PostCode", $"Invalid {addressType.ToLower()} postcode format");
                }
            }
        }


        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            return Regex.IsMatch(phone, @"^\+?[\d\s-]{10,}$");
        }

        private static bool IsValidPostcode(string postcode)
        {
            if (string.IsNullOrWhiteSpace(postcode))
            {
                return false;
            }

            return Regex.IsMatch(postcode.ToUpper(), @"^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$");
        }

        private static bool IsValidMicrochipFormat(string microchip)
        {
            if (string.IsNullOrWhiteSpace(microchip))
            {
                return false;
            }

            return Regex.IsMatch(microchip, @"^\d{15}$");
        }

        private static bool IsValidReferenceNumber(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return false;
            }

            return Regex.IsMatch(reference, @"^(GB|AD)\d{8}$", RegexOptions.IgnoreCase);
        }

    }
}