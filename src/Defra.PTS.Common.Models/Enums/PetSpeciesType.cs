using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.Enums
{
    public enum PetSpeciesType
    {
        [Description("")]
        None = 0,

        [Description("Dog")]
        Dog = 1,

        [Description("Cat")]
        Cat = 2,

        [Description("Ferret")]
        Ferret = 3,

        [Description("Other")]
        Other = 4
    }
}
