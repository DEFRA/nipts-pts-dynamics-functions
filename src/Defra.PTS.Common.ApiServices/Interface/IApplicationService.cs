using Defra.PTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.ApiServices.Interface
{
    public interface IApplicationService
    {
        Task<Application> GetApplication(Guid applicationId);
        Task<Guid?> UpdateApplicationStatus(ApplicationUpdateQueueModel applicationUpdateQueueModel);

        Task<bool> PerformHealthCheckLogic();

        Task<ApplicationSubmittedMessageQueueModel> GetApplicationQueueModel(Stream applicationStream);
    }
}
