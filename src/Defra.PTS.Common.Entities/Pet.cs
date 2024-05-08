using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Entities
{
    [ExcludeFromCodeCoverageAttribute]
    public class Pet
    {
        public Guid Id { get; set; }
        public int IdentificationType { get; set; }
        public string? MicrochipNumber { get; set; }
        public DateTime? MicrochippedDate { get; set; }
        public int SpeciesId { get; set; }
        public int? BreedId { get; set; }
        public int? BreedTypeId { get; set; }
        public string? AdditionalInfoMixedBreedOrUnknown { get; set; }
        public string Name { get; set; }
        public int SexId { get; set; }
        public int IsDateOfBirthKnown { get; set; }
        public DateTime? DOB { get; set; }
        public int? ApproximateAge { get; set; }
        public int ColourId { get; set; }
        public string? OtherColour { get; set; }
        public int HasUniqueFeature { get; set; }
        public string? UniqueFeatureDescription { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
