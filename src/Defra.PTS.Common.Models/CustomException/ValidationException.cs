﻿

using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Models.CustomException
{
    [ExcludeFromCodeCoverageAttribute]
    public class ValidationException : Exception
    {
        public ValidationException() { }

        public ValidationException(string message) : base(message) { }

        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
