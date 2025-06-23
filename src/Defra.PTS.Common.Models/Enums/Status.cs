using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.Enums
{
    public enum Status
    {
        [Description("Authorised")]
        Authorised = 1,
        [Description("Rejected")]
        Rejected = 2,
        [Description("Revoked")]
        Revoked = 3,
        [Description("Awaiting Verifictaion")]
        AwaitingVerifictaion = 4,
        [Description("Suspended")]
        Suspended = 5,
    }
}
