using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class UserRequest
    {
        public Guid? ContactId { get; set; }
        public Address? Address { get; set; }
    }
}