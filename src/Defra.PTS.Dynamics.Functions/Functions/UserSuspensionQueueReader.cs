using System;
using System.Threading.Tasks;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Defra.PTS.Dynamics.Functions.Functions
{
    public class UserSuspensionQueueReader(IUserService userService, ILogger<UserSuspensionQueueReader> logger)
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<UserSuspensionQueueReader> _logger = logger;

        [FunctionName("ProcessUserSuspension")]
        public async Task ProcessUserSuspension(
            [ServiceBusTrigger("%AzureServiceBusOptions:UserSuspendQueueName%",
            Connection = "ServiceBusConnection")] string myQueueItem)
        {
            if (string.IsNullOrEmpty(myQueueItem))
            {
                throw new UserFunctionException("Invalid Queue Message :" + myQueueItem);
            }

            UserSuspendQueueModel? suspension;
            try
            {
                suspension = JsonConvert.DeserializeObject<UserSuspendQueueModel>(myQueueItem);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid user suspension message format: {Message}", myQueueItem);
                throw new UserFunctionException("Invalid user suspension message format", ex);
            }

            if (suspension == null || suspension.ContactId == Guid.Empty)
            {
                throw new UserFunctionException("Invalid Object from message :" + myQueueItem);
            }

            _logger.LogInformation(
                "Processing user suspension change for ContactId: {ContactId}, IsUserSuspended: {IsUserSuspended}",
                suspension.ContactId, suspension.IsUserSuspended);

            var userId = await _userService.UpdateUserSuspensionStatus(suspension.ContactId, suspension.IsUserSuspended);

            _logger.LogInformation(
                "Updated suspension status for user: {UserId} (ContactId: {ContactId}) to IsUserSuspended: {IsUserSuspended}",
                userId, suspension.ContactId, suspension.IsUserSuspended);
        }
    }
}
