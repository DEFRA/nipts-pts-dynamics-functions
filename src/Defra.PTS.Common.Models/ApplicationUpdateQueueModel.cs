using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class ApplicationUpdateQueueModel
    {
        [JsonProperty("Application.Id ")]
        public Guid Id { get; set; }
        [JsonProperty("Application.DynamicId")]
        public Guid DynamicId { get; set; }
        [JsonProperty("Application.StatusId")]
        public string? StatusId { get; set; }
        [JsonProperty("Application.DateAuthorised")]
        public DateTime? DateAuthorised { get; set; }
        [JsonProperty("Application.DateRejected")]
        public DateTime? DateRejected { get; set; }
        [JsonProperty("Application.DateRevoked")]
        public DateTime? DateRevoked { get; set; }
    }
}
