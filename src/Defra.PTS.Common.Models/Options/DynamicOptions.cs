using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.Options
{
    public class DynamicOptions
    {
        public string? Authority { get; set; }
        public string? ApiVersion { get; set; }
        public string? Scopes { get; set; }
    }
}
