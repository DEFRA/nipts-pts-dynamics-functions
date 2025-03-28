using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Defra.PTS.Dynamics.Functions.Functions
{
    public partial class OfflineApplicationQueueReader(IOfflineApplicationService offlineApplicationService, ILogger<OfflineApplicationQueueReader> logger)
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

            bool isIdcomsMessage = IsIdcomsMessage(offlineApplication);

            if (isIdcomsMessage)
            {
                if (string.IsNullOrEmpty(offlineApplication.Owner?.Email))
                {
                    offlineApplication.Owner.Email = "ad.dummy.user@example.com";
                    _logger.LogInformation("Set default email for IDCOMS owner with reference: {Reference}",
                        offlineApplication.Application?.ReferenceNumber);
                }

                if (string.IsNullOrEmpty(offlineApplication.Applicant?.Email))
                {
                    offlineApplication.Applicant.Email = "ad.dummy.user@example.com";
                    _logger.LogInformation("Set default email for IDCOMS applicant with reference: {Reference}",
                        offlineApplication.Application?.ReferenceNumber);
                }
            }

            return offlineApplication;
        }

        [GeneratedRegex(@"^(GB826AD[0-9A-F]{4}|GB\d{8})$", RegexOptions.IgnoreCase)]
        private static partial Regex IdcomsRegex();

        private static bool IsIdcomsMessage(OfflineApplicationQueueModel model)
        {
            if (model?.Application?.ReferenceNumber == null)
                return false;

            return IdcomsRegex().IsMatch(model.Application.ReferenceNumber);
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
