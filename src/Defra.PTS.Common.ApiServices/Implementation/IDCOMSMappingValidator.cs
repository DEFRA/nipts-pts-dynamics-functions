﻿using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public partial class IdcomsMappingValidator(
        ILogger<IdcomsMappingValidator> logger,
        IBreedRepository breedRepository,
        IColourRepository colourRepository) : IIdcomsMappingValidator
    {
        private const string ReferenceNumberField = "ReferenceNumber";
        private const int TimeoutDuration = 30;
        private readonly ILogger<IdcomsMappingValidator> _logger = logger;
        private readonly IBreedRepository _breedRepository = breedRepository;
        private readonly IColourRepository _colourRepository = colourRepository;

        [GeneratedRegex(@"^\+?[\d\s-]{10,}$")]
        private static partial Regex PhoneNumberRegex();

        [GeneratedRegex(@"^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$")]
        private static partial Regex PostcodeRegex();

        [GeneratedRegex(@"^\d{15}$")]
        private static partial Regex MicrochipFormatRegex();

        [GeneratedRegex(@"^(GB826AD[0-9A-F]{4}|GB\d{8})$", RegexOptions.IgnoreCase)]
        private static partial Regex ReferenceNumberRegex();

        private static bool IsValidPhoneNumber(string phone)
        {
            return PhoneNumberRegex().IsMatch(phone);
        }

        private static bool IsValidPostcode(string postcode)
        {
            return PostcodeRegex().IsMatch(postcode.ToUpper());
        }

        private static bool IsValidMicrochipFormat(string microchip)
        {
            return MicrochipFormatRegex().IsMatch(microchip);
        }

        private static bool IsValidReferenceNumber(string reference)
        {
            return ReferenceNumberRegex().IsMatch(reference);
        }
        public async Task<ValidationResult> ValidateMapping(OfflineApplicationQueueModel? queueModel)
        {
            var result = new ValidationResult();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutDuration));

            if (queueModel == null)
            {
                result.AddError("Model", "Request model cannot be null");
                return result;
            }

            try
            {
                await ValidateAllFields(queueModel, result, cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Validation timed out after {Seconds} seconds", TimeoutDuration);
                result.AddError("Timeout", "Validation operation timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error occurred");
                result.AddError("Unexpected validation error", ex.Message);
            }

            return result;
        }

        private async Task ValidateAllFields(OfflineApplicationQueueModel model, ValidationResult result, CancellationToken cancellationToken)
        {
            ValidateOwnerFields(model, result);
            ValidateApplicantFields(model, result);
            await ValidatePetBreedInfo(model, result, cancellationToken);
            await ValidatePetColourInfo(model, result, cancellationToken);
            ValidatePetBasicInfo(model, result);
            ValidateAddressFields(model.OwnerAddress, "Owner", result);

            if (model.ApplicantAddress != null)
            {
                ValidateAddressFields(model.ApplicantAddress, "Applicant", result);
            }

            ValidateBasicApplicationFields(model, result);
            ValidateApplicationDates(model, result);
            ValidateIDCOMSFormat(model, result);
            await ValidateTravelDocumentFields(model, result, cancellationToken);
        }

        private async Task ValidatePetBreedInfo(OfflineApplicationQueueModel model, ValidationResult result, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(typeof(PetSpeciesType), model.Pet.SpeciesId))
            {
                result.AddError("PetSpeciesId", $"Invalid species value: {model.Pet.SpeciesId}");
                return;
            }

            if (model.Pet.SpeciesId == 3 && !model.Pet.BreedId.HasValue)
            {               
                return;
            }

            if (model.Pet.BreedId.HasValue && model.Pet.BreedId != 99 && model.Pet.BreedId != 100)
            {
                var breedTask = _breedRepository.FindById(model.Pet.BreedId.Value);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutDuration));

                try
                {
                    var breed = await breedTask.WaitAsync(cts.Token);
                    if (breed == null)
                    {
                        result.AddError("PetBreedId", $"Invalid breed id: {model.Pet.BreedId}");
                    }
                    else if (breed.SpeciesId != model.Pet.SpeciesId)
                    {
                        result.AddError("PetBreedId", $"Breed {model.Pet.BreedId} is not valid for species {model.Pet.SpeciesId}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw new OperationCanceledException("Breed validation timed out");
                }
            }
        }

        private async Task ValidatePetColourInfo(OfflineApplicationQueueModel model, ValidationResult result, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutDuration));

            try
            {
                var colour = await _colourRepository.FindById(model.Pet.ColourId).WaitAsync(cts.Token);
                if (colour == null)
                {
                    result.AddError("PetColourId", $"Invalid colour id: {model.Pet.ColourId}");
                }
                else if ((model.Pet.ColourId == 11 || model.Pet.ColourId == 20 || model.Pet.ColourId == 29)
                         && string.IsNullOrEmpty(model.Pet.OtherColour))
                {
                    result.AddError("OtherColour", "Other colour description is required for this colour selection");
                }
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Colour validation timed out");
            }
        }

        private static void ValidatePetBasicInfo(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!Enum.IsDefined(typeof(PetGenderType), model.Pet.SexId))
            {
                result.AddError("PetSexId", $"Invalid sex value: {model.Pet.SexId}");
            }

            ValidatePetName(model, result);
            ValidatePetMicrochip(model, result);
            ValidatePetDates(model, result);
            ValidatePetAdditionalInfo(model, result);
        }

        private static void ValidatePetName(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Pet.Name))
            {
                result.AddError("PetName", "Pet name is required");
            }
            else if (model.Pet.Name.Length > 300)
            {
                result.AddError("PetName", "Pet name cannot exceed 300 characters");
            }
        }

        private static void ValidatePetMicrochip(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(model.Pet.MicrochipNumber))
            {
                if (!IsValidMicrochipFormat(model.Pet.MicrochipNumber))
                {
                    result.AddError("MicrochipNumber", "Invalid microchip number format");
                }
                if (model.Pet.MicrochipNumber.Length > 15)
                {
                    result.AddError("MicrochipNumber", "Microchip number cannot exceed 15 characters");
                }
            }
        }

        private static void ValidatePetDates(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model.Pet.DOB.HasValue && model.Pet.DOB.Value > DateTime.UtcNow)
            {
                result.AddError("DOB", "Date of birth cannot be in the future");
            }

            if (model.Pet.MicrochippedDate.HasValue && model.Pet.MicrochippedDate.Value > DateTime.UtcNow)
            {
                result.AddError("MicrochippedDate", "Microchip date cannot be in the future");
            }
        }

        private static void ValidatePetAdditionalInfo(OfflineApplicationQueueModel model, ValidationResult result)
        {
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
        }

        private static void ValidateBasicApplicationFields(OfflineApplicationQueueModel model, ValidationResult result)
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

            ValidateReferenceNumber(model, result);
            ValidateDynamicId(model, result);
        }

        private static void ValidateReferenceNumber(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Application.ReferenceNumber))
            {
                result.AddError(ReferenceNumberField, "Reference number is required");
                return;
            }

            if (model.Application.ReferenceNumber.Length > 20)
            {
                result.AddError(ReferenceNumberField, "Reference number cannot exceed 20 characters");
            }

            if (!IsValidReferenceNumber(model.Application.ReferenceNumber))
            {
                result.AddError(ReferenceNumberField, "Invalid reference number format");
            }
        }

        private static void ValidateDynamicId(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(model.Application.DynamicId) &&
                !Guid.TryParse(model.Application.DynamicId, out _))
            {
                result.AddError("DynamicId", "Invalid Dynamic ID format");
            }
        }

        private static void ValidateApplicationDates(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model.Application.DateOfApplication.Date > DateTime.UtcNow.Date)
            {
                result.AddError("DateOfApplication", "Application date cannot be in the future");
            }

            if (model.Application.DateAuthorised.HasValue &&
                model.Application.DateAuthorised.Value.Date < model.Application.DateOfApplication.Date)
            {
                result.AddError("DateAuthorised", "Authorization date cannot be before application date");
            }
        }

        private static async Task ValidateTravelDocumentFields(OfflineApplicationQueueModel model, ValidationResult result, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => ValidateTravelDocumentBasicFields(model, result), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Travel document validation timed out");
            }
        }

        private static void ValidateTravelDocumentBasicFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (model.Ptd == null)
            {
                result.AddError(ReferenceNumberField, "Travel document information is required");
                return;
            }

            ValidateTravelDocumentReference(model, result);
        }

        private static void ValidateTravelDocumentReference(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (string.IsNullOrEmpty(model.Ptd.DocumentReferenceNumber))
            {
                result.AddError(ReferenceNumberField, "Document reference number is required");
                return;
            }

            if (model.Ptd.DocumentReferenceNumber.Length > 20)
            {
                result.AddError(ReferenceNumberField, "Document reference number cannot exceed 20 characters");
            }

            if (model.Ptd.DocumentReferenceNumber != model.Application.ReferenceNumber)
            {
                result.AddError(ReferenceNumberField, "Document reference number must match application reference number");
            }
        }

      
       
        private static void ValidateIDCOMSFormat(OfflineApplicationQueueModel model, ValidationResult result)
        {
            ValidateIDCOMSReferenceNumber(model, result);
            ValidateIDCOMSNames(model, result);
        }



        private static void ValidateIDCOMSReferenceNumber(OfflineApplicationQueueModel model, ValidationResult result)
        {
            if (!ReferenceNumberRegex().IsMatch(model.Application.ReferenceNumber))
            {
                result.AddError(ReferenceNumberField, "Reference number must either be GB followed by 8 digits or GB826AD followed by 4 hexadecimal characters");
            }
        }


        private static void ValidateIDCOMSNames(OfflineApplicationQueueModel model, ValidationResult result)
        {
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

        private static void ValidateOwnerFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            ValidateOwnerName(model.Owner.FullName, result);
            ValidateOwnerEmail(model.Owner.Email, result);
            ValidateOwnerPhone(model.Owner.Telephone, result);
        }

        private static void ValidateOwnerName(string fullName, ValidationResult result)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                result.AddError("OwnerName", "Owner name is required");
            }
            else if (fullName.Length > 300)
            {
                result.AddError("OwnerName", "Owner name cannot exceed 300 characters");
            }
        }

        private static void ValidateOwnerEmail(string email, ValidationResult result)
        {
            if (string.IsNullOrEmpty(email))
            {
                result.AddError("OwnerEmail", "Owner email is required");
            }
            else
            {
                if (email.Length > 100)
                {
                    result.AddError("OwnerEmail", "Owner email cannot exceed 100 characters");
                }
                if (!IsValidEmail(email))
                {
                    result.AddError("OwnerEmail", "Invalid email format");
                }
            }
        }

        private static void ValidateOwnerPhone(string phone, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                if (phone.Length > 50)
                {
                    result.AddError("OwnerPhone", "Owner phone number cannot exceed 50 characters");
                }
                if (!IsValidPhoneNumber(phone))
                {
                    result.AddError("OwnerPhone", "Invalid phone number format");
                }
            }
        }


        private static void ValidateApplicantFields(OfflineApplicationQueueModel model, ValidationResult result)
        {
            ValidateApplicantName(model.Applicant.FullName, result);
            ValidateApplicantEmail(model.Applicant.Email, result);
            ValidateApplicantPhone(model.Applicant.Telephone, result);
            ValidateApplicantFirstName(model.Applicant.FirstName, result);
            ValidateApplicantLastName(model.Applicant.LastName, result);
        }

        private static void ValidateApplicantName(string fullName, ValidationResult result)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                result.AddError("ApplicantName", "Applicant name is required");
            }
            else if (fullName.Length > 300)
            {
                result.AddError("ApplicantName", "Applicant name cannot exceed 300 characters");
            }
        }

        private static void ValidateApplicantEmail(string email, ValidationResult result)
        {
            if (string.IsNullOrEmpty(email))
            {
                result.AddError("ApplicantEmail", "Applicant email is required");
            }
            else
            {
                if (email.Length > 100)
                {
                    result.AddError("ApplicantEmail", "Applicant email cannot exceed 100 characters");
                }
                if (!IsValidEmail(email))
                {
                    result.AddError("ApplicantEmail", "Invalid email format");
                }
            }
        }

        private static void ValidateApplicantPhone(string phone, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                if (phone.Length > 50)
                {
                    result.AddError("ApplicantPhone", "Applicant phone number cannot exceed 50 characters");
                }
                if (!IsValidPhoneNumber(phone))
                {
                    result.AddError("ApplicantPhone", "Invalid phone number format");
                }
            }
        }

        private static void ValidateApplicantFirstName(string firstName, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(firstName) && firstName.Length > 100)
            {
                result.AddError("ApplicantFirstName", "Applicant first name cannot exceed 100 characters");
            }
        }

        private static void ValidateApplicantLastName(string lastName, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(lastName) && lastName.Length > 100)
            {
                result.AddError("ApplicantLastName", "Applicant last name cannot exceed 100 characters");
            }
        }

        private static void ValidateAddressFields(AddressInfo address, string addressType, ValidationResult result)
        {
            if (address == null)
            {
                result.AddError($"{addressType}Address", $"{addressType} address information is required");
                return;
            }

            ValidateAddressLineOne(address.AddressLineOne, addressType, result);
            ValidateAddressLineTwo(address.AddressLineTwo, addressType, result);
            ValidateTownOrCity(address.TownOrCity, addressType, result);
            ValidateCounty(address.County, addressType, result);
            ValidatePostCode(address.PostCode, addressType, result);
        }

        private static void ValidateAddressLineOne(string addressLineOne, string addressType, ValidationResult result)
        {
            if (string.IsNullOrEmpty(addressLineOne))
            {
                result.AddError($"{addressType}AddressLineOne", $"{addressType} address line 1 is required");
            }
            else if (addressLineOne.Length > 250)
            {
                result.AddError($"{addressType}AddressLineOne", $"{addressType} address line 1 cannot exceed 250 characters");
            }
        }

        private static void ValidateAddressLineTwo(string addressLineTwo, string addressType, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(addressLineTwo) && addressLineTwo.Length > 250)
            {
                result.AddError($"{addressType}AddressLineTwo", $"{addressType} address line 2 cannot exceed 250 characters");
            }
        }

        private static void ValidateTownOrCity(string townOrCity, string addressType, ValidationResult result)
        {
            if (string.IsNullOrEmpty(townOrCity))
            {
                result.AddError($"{addressType}TownOrCity", $"{addressType} town/city is required");
            }
            else if (townOrCity.Length > 250)
            {
                result.AddError($"{addressType}TownOrCity", $"{addressType} town/city cannot exceed 250 characters");
            }
        }

        private static void ValidateCounty(string county, string addressType, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(county) && county.Length > 100)
            {
                result.AddError($"{addressType}County", $"{addressType} county cannot exceed 100 characters");
            }
        }

        private static void ValidatePostCode(string postCode, string addressType, ValidationResult result)
        {
            if (string.IsNullOrEmpty(postCode))
            {
                result.AddError($"{addressType}PostCode", $"{addressType} postcode is required");
            }
            else
            {
                if (postCode.Length > 20)
                {
                    result.AddError($"{addressType}PostCode", $"{addressType} postcode cannot exceed 20 characters");
                }
                if (!IsValidPostcode(postCode))
                {
                    result.AddError($"{addressType}PostCode", $"Invalid {addressType.ToLower()} postcode format");
                }
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

    }
}