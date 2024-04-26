using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Entities
{
    [ExcludeFromCodeCoverageAttribute]
    public class PetDocumentEvidence
    {
        public Guid Id { get; set; }
        public Guid PetId { get; set; }
        public string EvidenceReference { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
