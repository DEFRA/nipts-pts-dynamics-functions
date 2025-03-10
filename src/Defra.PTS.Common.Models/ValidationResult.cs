﻿using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class ValidationResult
    {
        public List<ValidationError> Errors { get; set; } = [];

        public bool IsValid => Errors.Count == 0;

        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError { Field = field, Message = message });
        }
    }

    [ExcludeFromCodeCoverage]
    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
