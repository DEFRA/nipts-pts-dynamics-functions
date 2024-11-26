using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Entities
{
    [ExcludeFromCodeCoverageAttribute]
    public class Breed
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int SpeciesId { get; set; }
    }
}
