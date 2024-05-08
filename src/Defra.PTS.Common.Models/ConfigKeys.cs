using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public static class ConfigKeys
    {
        public const string ServiceBusConnection = nameof(ServiceBusConnection);
        public const string ServiceBusNamespace = $"{ServiceBusConnection}:fullyQualifiedNamespace";
    }
}
