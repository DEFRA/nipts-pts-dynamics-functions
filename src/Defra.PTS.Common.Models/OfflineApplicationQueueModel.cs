using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class OfflineApplicationQueueModel
    {
        public AddressInfo OwnerAddress { get; set; } = new();
        public AddressInfo ApplicantAddress { get; set; } = new();
        public OwnerInfo Owner { get; set; } = new();
        public ApplicantInfo Applicant { get; set; } = new();
        public PetInfo Pet { get; set; } = new();
        public ApplicationInfo Application { get; set; } = new();
        public PtdInfo Ptd { get; set; } = new();
        public Guid CreatedBy { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class AddressInfo
    {
        public string AddressLineOne { get; set; } = string.Empty;
        public string AddressLineTwo { get; set; } = string.Empty;
        public string TownOrCity { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string PostCode { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class OwnerInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class ApplicantInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ContactId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class PetInfo
    {
        public string Name { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public DateTime? MicrochippedDate { get; set; }
        public int SpeciesId { get; set; }
        public int BreedId { get; set; }
        public int SexId { get; set; }
        public DateTime? DOB { get; set; }
        public int ColourId { get; set; }
        public string AdditionalInfoMixedBreedOrUnknown { get; set; } = string.Empty;
        public string UniqueFeatureDescription { get; set; } = string.Empty;
        public string OtherColour { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class ApplicationInfo
    {
        public string Status { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime DateOfApplication { get; set; }
        public string DynamicId { get; set; } = string.Empty;
        public DateTime? DateAuthorised { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PtdInfo
    {
        public string DocumentReferenceNumber { get; set; } = string.Empty;
    }
}