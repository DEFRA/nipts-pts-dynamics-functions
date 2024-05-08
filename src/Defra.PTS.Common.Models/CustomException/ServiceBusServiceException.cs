using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.CustomException
{
    [ExcludeFromCodeCoverageAttribute]
    public class ServiceBusServiceException :  Exception
    {
        public ServiceBusServiceException() { }

        public ServiceBusServiceException(string message) : base(message) { }

        public ServiceBusServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
