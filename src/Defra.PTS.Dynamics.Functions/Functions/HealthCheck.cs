using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Common.ApiServices.Interface;

namespace Defra.PTS.Dynamics.Functions.Functions
{
    public class HealthCheck
    {
        private readonly IApplicationService _applicationService;
        private readonly IDynamicsService _dynamicsService;

        private const string TagName = "name";

        public HealthCheck(
            IApplicationService applicationService
            , IDynamicsService dynamicsService)
        {
            _applicationService = applicationService;
            _dynamicsService = dynamicsService;
        }

        [Function("HealthCheck")]
        [OpenApiOperation(operationId: "Run", tags: TagName )]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req
            , ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Perform health check logic here
            bool isSqlConnectionHealthy = await _applicationService.PerformHealthCheckLogic();
            bool dynamicsConnectionHealthy = await _dynamicsService.PerformHealthCheckLogic();

            if (isSqlConnectionHealthy && dynamicsConnectionHealthy)
            {
                return new OkResult();
            }
            else
            {
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}

