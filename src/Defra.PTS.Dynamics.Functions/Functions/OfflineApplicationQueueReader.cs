using System;
using System.Threading.Tasks;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Defra.PTS.Dynamics.Functions.Functions
{
    public class OfflineApplicationQueueReader(IOfflineApplicationService offlineApplicationService, ILogger<OfflineApplicationQueueReader> logger)
    {
        private readonly IOfflineApplicationService _offlineApplicationService = offlineApplicationService;
        private readonly ILogger<OfflineApplicationQueueReader> _logger = logger;

        [FunctionName("ProcessOfflineApplication")]
        public async Task ProcessOfflineApplication(
            [ServiceBusTrigger("%AzureServiceBusOptions:OfflineApplicationQueueName%",
            Connection = "ServiceBusConnection")] string queueMessage)
        {
            try
            {
                await ProcessQueueMessage(queueMessage);
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

        private async Task ProcessQueueMessage(string queueMessage)
        {
            _logger.LogInformation("Starting to process offline application message: {Message}", queueMessage);

            if (string.IsNullOrEmpty(queueMessage))
            {
                _logger.LogWarning("Empty message received in offline application queue");
                return;
            }

            var offlineApplication = DeserializeMessage(queueMessage);
            if (offlineApplication == null)
            {
                return;
            }

            await ProcessApplication(offlineApplication);
        }

        private OfflineApplicationQueueModel DeserializeMessage(string queueMessage)
        {
            var offlineApplication = JsonConvert.DeserializeObject<OfflineApplicationQueueModel>(queueMessage);
            if (offlineApplication == null)
            {
                _logger.LogWarning("Failed to deserialize message to OfflineApplicationQueueModel: {Message}", queueMessage);
                return null;
            }
                       
            string documentReferenceNumber = offlineApplication.Ptd?.DocumentReferenceNumber ?? "";
                        
            string standardizedEmail = $"ad.blank.email.{documentReferenceNumber}@example.com";

            if (offlineApplication.Owner != null)
            {
                offlineApplication.Owner.Email = standardizedEmail;
                _logger.LogInformation("Set standardized email for owner with document reference: {Reference}",
                    offlineApplication.Ptd?.DocumentReferenceNumber);
            }

            if (offlineApplication.Applicant != null)
            {
                offlineApplication.Applicant.Email = standardizedEmail;
                _logger.LogInformation("Set standardized email for applicant with document reference: {Reference}",
                    offlineApplication.Ptd?.DocumentReferenceNumber);
            }

            return offlineApplication;
        }

        private async Task ProcessApplication(OfflineApplicationQueueModel offlineApplication)
        {
            try
            {
                await _offlineApplicationService.ProcessOfflineApplication(offlineApplication);
                _logger.LogInformation("Successfully processed offline application for reference: {Reference}",
                    offlineApplication.Application.ReferenceNumber);
            }
            catch (OfflineApplicationProcessingException ex) when (ex.Message.Contains("Validation failed"))
            {
                _logger.LogWarning(ex, "Validation failure for application {Reference}",
                    offlineApplication.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Validation failure for application {offlineApplication.Application.ReferenceNumber}", ex);
            }
            catch (OfflineApplicationProcessingException ex)
            {
                _logger.LogError(ex, "Processing error for application {Reference}",
                    offlineApplication.Application.ReferenceNumber);
                throw new OfflineApplicationProcessingException(
                    $"Processing error for application {offlineApplication.Application.ReferenceNumber}", ex);
            }
        }
    }
}