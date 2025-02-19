using Defra.PTS.Common.Models;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IIdcomsMappingValidator
    {
        ValidationResult ValidateMapping(OfflineApplicationQueueModel queueModel);
    }
}