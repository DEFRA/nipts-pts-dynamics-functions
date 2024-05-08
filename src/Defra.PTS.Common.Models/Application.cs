using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class Application
    {
        [JsonProperty("nipts_applicantid@odata.bind")]
        public string? NiptsApplicantId { get; set; }

        [JsonProperty("nipts_portalapplicationid")]
        public string? NiptsPortalApplicationId { get; set; }

        [JsonProperty("nipts_applicationreference")]
        public string? NiptsApplicationReference { get; set; }

        [JsonProperty("nipts_documentreference")]
        public string? NiptsDocumentReference { get; set; }

        [JsonProperty("nipts_submissiondate")]
        public string? NiptsSubmissionDate { get; set; }

        [JsonProperty("nipts_ownertype")]
        public string? NiptsOwnerType { get; set; }

        [JsonProperty("nipts_ownername")]
        public string? NiptsOwnerName { get; set; }

        [JsonProperty("nipts_owneremail")]
        public string? NiptsOwnerEmail { get; set; }

        [JsonProperty("nipts_owneraddressline1")]
        public string? NiptsOwnerAddressLine1 { get; set; }

        [JsonProperty("nipts_owneraddressline2")]
        public string? NiptsOwnerAddressLine2 { get; set; }

        [JsonProperty("nipts_owneraddressline3")]
        public string? NiptsOwnerAddressLine3 { get; set; }

        [JsonProperty("nipts_ownertown")]
        public string? NiptsOwnerTown { get; set; }

        [JsonProperty("nipts_ownerpostcode")]
        public string? NiptsOwnerPostcode { get; set; }

        [JsonProperty("nipts_ownercounty")]
        public string? NiptsOwnerCounty { get; set; }

        [JsonProperty("nipts_ownercountry")]
        public string NiptsOwnerCountry { get; set; } = string.Empty;

        [JsonProperty("nipts_ownerphone")]
        public string? NiptsOwnerphone { get; set; }

        [JsonProperty("nipts_charityname")]
        public string? NiptsCharityName { get; set; }

        [JsonProperty("nipts_petname")]
        public string? NiptsPetname { get; set; }

        [JsonProperty("nipts_petspecies")]
        public string? NiptsPetSpecies { get; set; }

        [JsonProperty("nipts_petbreed")]
        public string? NiptsPetBreed { get; set; }

        [JsonProperty("nipts_petsex")]
        public string? NiptsPetSex { get; set; }

        [JsonProperty("nipts_petdob")]
        public string? NiptsPetDob { get; set; }

        [JsonProperty("nipts_petcolour")]
        public string? NiptsPetColour { get; set; }

        [JsonProperty("nipts_petothercolour")]
        public string? NiptsPetOtherColour { get; set; }

        [JsonProperty("nipts_petuniquefeatures")]
        public string? NiptsPetUniqueFeatures { get; set; }

        [JsonProperty("nipts_microchipnum")]
        public string? NiptsMicrochipnum { get; set; }

        [JsonProperty("nipts_microchippeddate")]
        public string? NiptsMicrochippedDate { get; set; }
    }
}
