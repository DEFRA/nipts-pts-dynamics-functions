using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class UserSuspendQueueModel
    {
        public Guid ContactId { get; set; }
        public string? Status { get; set; }
    }
}
