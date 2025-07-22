using Azure.Messaging.ServiceBus;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Common.Models.CustomException;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.ApiServices.Implementation
{
    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusService> _logger;
        private readonly IOptions<AzureServiceBusOptions> _serviceBusOptions;

        public ServiceBusService(
            ServiceBusClient client,
            IOptions<AzureServiceBusOptions> serviceBusOptions,
            ILogger<ServiceBusService> logger)
        {
            _client = client;
            _logger = logger;
            _serviceBusOptions = serviceBusOptions;
        }

        public async Task SendMessageAsync(ApplicationSubmittedMessageQueueModel message)
        {
            if (message == null)
            {
                return;
            }

            _logger.LogInformation("Sending application submitted message for ApplicationId : {0}", message.ApplicationId);

            var messages = new List<ApplicationSubmittedMessageQueueModel> { message };
            await SendBatchAsync(messages);

            _logger.LogInformation("Successfully sent application submitted message for ApplicationId : {0}", message.ApplicationId);
        }

        private async Task AttemptSendBatch(ServiceBusSender sender, int batchIndex, ServiceBusMessageBatch messageBatch)
        {
            bool success = false;
            int attempts = 0;
            while (!success && attempts < 10)
            {
                try
                {
                    await SendBatch(sender, batchIndex, messageBatch);
                    success = true;
                }
                catch (ServiceBusException ex)
                {
                    attempts++;

                    _logger.LogError(ex, "An error occurred sending the service bus batch. It will be retried in 5 seconds.");

                    // The most likely cause of this error is the service bus namespace being throttled. Wait 5 seconds and try again.
                    await Task.Delay(5000);
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private async Task SendBatchAsync<T>(List<T> messages)
        {
            var sender = _client.CreateSender(_serviceBusOptions.Value.SubmitQueueName);

            var batchIndex = 0;

            ServiceBusMessageBatch? messageBatch = null;
            try
            {
                messageBatch = await sender.CreateMessageBatchAsync();
                if (messageBatch == null)
                {
                    throw new ServiceBusServiceException("Cannot create message batch");
                }

                foreach (var message in messages)
                {
                    var serviceBusMessage = BuildMessage(message);
                    serviceBusMessage.ApplicationProperties.Add("Application", "PTS");
                    if (!messageBatch.TryAddMessage(serviceBusMessage))
                    {
                        await AttemptSendBatch(sender, batchIndex, messageBatch);

                        batchIndex++;

                        messageBatch.Dispose();
                        messageBatch = await sender.CreateMessageBatchAsync();

                        if (!messageBatch.TryAddMessage(serviceBusMessage))
                            throw new InvalidOperationException($"Could not add service bus message to empty batch. {message}");
                    }
                }

                if (messageBatch.Count > 0)
                {
                    await SendBatch(sender, batchIndex, messageBatch);
                }
            }
            finally
            {
                await sender.DisposeAsync();
            }
        }

        [ExcludeFromCodeCoverage]
        private async Task SendBatch(ServiceBusSender sender, int batchIndex, ServiceBusMessageBatch messageBatch)
        {
            _logger.LogInformation("Sending {0} in a message batch for batchIndex {1}", messageBatch.Count, batchIndex);

            await sender.SendMessagesAsync(messageBatch);
        }

        [ExcludeFromCodeCoverage]
        private static ServiceBusMessage BuildMessage<T>(T message)
        {
            var messageJson = JsonSerializer.Serialize(message);
            return new ServiceBusMessage(messageJson);
        }
    }
}
