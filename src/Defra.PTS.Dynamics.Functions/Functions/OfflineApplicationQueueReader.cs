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
    public class OfflineApplicationQueueReader
    {
        private readonly IOfflineApplicationService _offlineApplicationService;
        private readonly ILogger<OfflineApplicationQueueReader> _logger;

        public OfflineApplicationQueueReader(
            IOfflineApplicationService offlineApplicationService,
            ILogger<OfflineApplicationQueueReader> logger)
        {
            _offlineApplicationService = offlineApplicationService;
            _logger = logger;
        }

        [FunctionName("ProcessOfflineApplication")]
        public async Task ProcessOfflineApplication(
        [ServiceBusTrigger("%AzureServiceBusOptions:OfflineApplicationQueueName%", Connection = "ServiceBusConnection")] string queueMessage)
        {
            try
            {
                _logger.LogInformation("Starting to process offline application message: {Message}",
              queueMessage);
                _logger.LogInformation($"Starting to process offline application message: {queueMessage}");

                if (string.IsNullOrEmpty(queueMessage))
                {
                    _logger.LogWarning("Message was empty");
                    return;
                }

                var offlineApplication = JsonConvert.DeserializeObject<OfflineApplicationQueueModel>(queueMessage);
                if (offlineApplication == null)
                {
                    _logger.LogWarning("Could not deserialize message to OfflineApplicationQueueModel");
                    return;
                }

                try
                {
                    await _offlineApplicationService.ProcessOfflineApplication(offlineApplication);
                    _logger.LogInformation("Successfully processed offline application for reference: {Reference}",
                offlineApplication.Application.ReferenceNumber);
                }
                catch (OfflineApplicationProcessingException ex) when (ex.Message.Contains("Validation failed"))
                {
                    _logger.LogWarning(ex, "Validation failure for message {Message}", ex.Message);
                    throw; // This will cause the message to be moved to dead letter queue
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offline application", ex);
                throw; // Retries will be handled by the Service Bus retry policy
            }
        }

    }
}

