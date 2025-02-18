using Defra.PTS.Common.Models;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IIDCOMSMappingValidator
    {
        Task<ValidationResult> ValidateMapping(OfflineApplicationQueueModel queueModel);
    }
}