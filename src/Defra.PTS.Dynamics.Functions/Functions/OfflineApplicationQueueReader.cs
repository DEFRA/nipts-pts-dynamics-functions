using System;
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
        public void ProcessOfflineApplication(
            [ServiceBusTrigger("%AzureServiceBusOptions:OfflineApplicationQueueName%", Connection = "ServiceBusConnection")] string queueMessage)
        {
            try
            {
                _logger.LogInformation("Starting to process offline application message: {Message}", queueMessage);

                if (string.IsNullOrEmpty(queueMessage))
                {
                    _logger.LogWarning("Empty message received in offline application queue");
                    return;
                }

                var offlineApplication = JsonConvert.DeserializeObject<OfflineApplicationQueueModel>(queueMessage);
                if (offlineApplication == null)
                {
                    _logger.LogWarning("Failed to deserialize message to OfflineApplicationQueueModel: {Message}", queueMessage);
                    return;
                }

                try
                {
                    _offlineApplicationService.ProcessOfflineApplication(offlineApplication);
                    _logger.LogInformation("Successfully processed offline application for reference: {Reference}",
                        offlineApplication.Application.ReferenceNumber);
                }
                catch (OfflineApplicationProcessingException ex) when (ex.Message.Contains("Validation failed"))
                {
                    _logger.LogWarning("Validation failure for application {Reference}: {Message}",
                        offlineApplication.Application.ReferenceNumber, ex.Message);
                    throw new OfflineApplicationProcessingException($"Validation failed for application {offlineApplication.Application.ReferenceNumber}", ex);
                }
                catch (OfflineApplicationProcessingException ex)
                {
                    _logger.LogError(ex, "Processing error for application {Reference}",
                        offlineApplication.Application.ReferenceNumber);
                    throw new OfflineApplicationProcessingException($"Failed to process application {offlineApplication.Application.ReferenceNumber}", ex);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error for message: {Message}", queueMessage);
                throw new OfflineApplicationProcessingException("Invalid message format", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing offline application message: {Message}", queueMessage);
                throw new OfflineApplicationProcessingException("Unhandled error processing offline application", ex);
            }
        }
    }
}