using System.IO;
using System.Net;
using System.Threading.Tasks;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models.CustomException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Model = Defra.PTS.Common.Models;
using Azure.Messaging.ServiceBus;
using static Defra.PTS.Common.Models.ConfigKeys;
using System.Threading;
using System;

namespace Defra.PTS.Dynamics.Functions.Functions
{
    public class QueueWriter
    {
        private readonly IApplicationService _applicationService;
        private readonly IServiceBusService _azureServiceBusService;

        public QueueWriter(
            IApplicationService applicationService
            , IServiceBusService azureServiceBusService)
        {
            _applicationService = applicationService;
            _azureServiceBusService = azureServiceBusService;
        }

        [Function("WriteApplicationToQueue")]
        [OpenApiOperation(operationId: "WriteApplicationToQueue", tags: ["Queue"], Summary = "Submit an application message to the Service Bus queue")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Message added to queue successfully")]
        public async Task<IActionResult> WriteApplicationToQueue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "writetoqueue")] HttpRequest req
            )
        {
            string responseMessage = $"Added Message to Queue Successfully";       
            var inputData = req?.Body;
            if (inputData == null)
            {
                throw new QueueWriterException("Invalid Queue message input, is NUll or Empty");
            }

            var applicationMessagetoSubmit = await _applicationService.GetApplicationQueueModel(inputData);
            if(applicationMessagetoSubmit == null)
            {
                throw new QueueWriterException("Invalid QueueMessage Model, is Null or Empty");
            }

            await _azureServiceBusService.SendMessageAsync(applicationMessagetoSubmit);

            return new OkObjectResult(responseMessage);
        }
    }
}

