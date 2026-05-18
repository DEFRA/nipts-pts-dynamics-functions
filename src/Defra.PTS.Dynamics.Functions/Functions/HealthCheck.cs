using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Common.ApiServices.Interface;

namespace Defra.PTS.Dynamics.Functions.Functions
{
    public class HealthCheck
    {
        private readonly IApplicationService _applicationService;
        private readonly IDynamicsService _dynamicsService;
        private readonly ILogger<HealthCheck> _logger;

        public HealthCheck(
            IApplicationService applicationService
            , IDynamicsService dynamicsService
            , ILogger<HealthCheck> logger)
        {
            _applicationService = applicationService;
            _dynamicsService = dynamicsService;
            _logger = logger;
        }

        [Function("HealthCheck")]
        [OpenApiOperation(operationId: "HealthCheck", tags: ["Health"], Summary = "Health check endpoint")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Services are healthy")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.ServiceUnavailable, Description = "One or more services are unavailable")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

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

