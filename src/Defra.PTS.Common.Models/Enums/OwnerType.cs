using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.Enums
{
    public enum OwnerType
    {
        [Description("Yes, I am the registered keeper")]
        RegisteredOwner = 1,

        [Description("No, I am applying for someone else's pet")]
        ApplyingForSomeoneElse = 2,

        [Description("No, I have an assistance dog registered in a charity's name")]
        RegisteredCharity = 3
    }

    public enum DynamicsOwnerType
    {
        Self = 489480000,
        ThirdParty = 489480001,
        Charity = 489480002
    }
}
