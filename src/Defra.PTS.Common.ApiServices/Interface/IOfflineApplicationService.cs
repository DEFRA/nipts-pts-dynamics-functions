using Defra.PTS.Common.Models;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IOfflineApplicationService
    {
        void ProcessOfflineApplication(OfflineApplicationQueueModel queueModel);
    }
}