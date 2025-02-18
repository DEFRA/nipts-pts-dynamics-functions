using Defra.PTS.Common.Models;

namespace Defra.PTS.Common.ApiServices.Interface
{
   
    public interface IOfflineApplicationService
    {      
        Task ProcessOfflineApplication(OfflineApplicationQueueModel queueModel);
    }
}