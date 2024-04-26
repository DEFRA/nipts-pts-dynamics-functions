using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.Models.CustomException
{
    [ExcludeFromCodeCoverageAttribute]
    public class UserFunctionException : Exception
    {
        public UserFunctionException() { }

        public UserFunctionException(string message) : base(message) { }

        public UserFunctionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
