using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class ApplicationSubmittedMessageQueueModel
    {
        public Guid ApplicationId { get; set; }
        public int ApplicationLanguage { get; set; }
    }
}
