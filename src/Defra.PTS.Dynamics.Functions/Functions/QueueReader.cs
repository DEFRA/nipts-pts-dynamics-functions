using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Models.Helper;
using Defra.PTS.Common.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using static Defra.PTS.Common.Models.ConfigKeys;


namespace Defra.PTS.Dynamics.Functions.Functions
{
    public class QueueReader
    {
        private readonly IDynamicsService _dynamicsService;
        private readonly IOptions<DynamicOptions> _dynamicOptions;
        private readonly IApplicationService _applicationService;
        private readonly IKeyVaultAccess _keyVaultAccess;
        private readonly HttpClient _httpClient;
        private readonly ILogger<QueueReader> _logger;

        public QueueReader(
              IDynamicsService dynamicsService
            , IOptions<DynamicOptions> dynamicOptions
            , IApplicationService applicationService
            , IKeyVaultAccess keyVaultAccess
            , HttpClient httpClient
            , ILogger<QueueReader> logger)
        {
            _dynamicsService = dynamicsService;
            _dynamicOptions = dynamicOptions;
            _applicationService = applicationService;
            _keyVaultAccess = keyVaultAccess;
            _httpClient = httpClient;
            _logger = logger;
        }


        [Function("ReadApplicationFromQueue")]
        public async Task ReadApplicationFromQueue(
              [ServiceBusTrigger("%AzureServiceBusOptions:SubmitQueueName%", Connection = ServiceBusConnection)] string myQueueItem)
        {
            if (string.IsNullOrEmpty(myQueueItem))
            {
                throw new QueueReaderException("Invalid Queue Message :" + myQueueItem);
            }

            var currentApplication = JsonConvert.DeserializeObject<ApplicationSubmittedMessageQueueModel>(myQueueItem);
            if (currentApplication == null ||
                currentApplication.ApplicationId == Guid.Empty)
            {
                throw new QueueReaderException("Invalid Object from message :" + myQueueItem);
            }

            Guid applicationId = currentApplication.ApplicationId;
            
            var apiVersion = _dynamicOptions.Value.ApiVersion;
            string serviceUrl = await _keyVaultAccess.GetSecretAsync("Pts-Dynamics-Tenant-ServiceUrl");
            string apiUrl = $"{serviceUrl}/api/data/v{apiVersion}/nipts_ptdapplications";

            string accessToken = await _dynamicsService.GetTokenForClient(new[] { $"{serviceUrl}/.default" });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var applicationObject = await _applicationService.GetApplication(applicationId);
            

            if (applicationObject == null)
            {
                throw new QueueReaderException("Invalid Data Object so cannot Post to Dynamics");
            }

            // Default to English (489480000) if ApplicationLanguage is not set or invalid
            applicationObject.NiptsApplicationLanguage = currentApplication.ApplicationLanguage == 0 
                ? "489480000" 
                : currentApplication.ApplicationLanguage.ToString();
            string jsonData = JsonConvert.SerializeObject(applicationObject);
            _logger.LogInformation("Request Headers for application: {0} {1} Posting Json Payload: {2}", applicationId.ToString(), _httpClient.DefaultRequestHeaders.ToString(), jsonData);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            _logger.LogInformation("POST Response for application: ", applicationId.ToString(), response);
            if (response.IsSuccessStatusCode)
            {
                // Read and print the response content
                _logger.LogInformation("POST request successful for ", applicationId.ToString());
            }
            else
            {
                _logger.LogError("Error: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                if (response.Content == null || response.Content.Headers.ContentLength == 0)
                {
                    throw new QueueReaderException($"Error Response Content is Null for {applicationId}");
                }


                // Log Error message from dynamics and Throw exception with details returned from Dynamics
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Error Response content for application: ", applicationId.ToString(), responseContent);
                bool isValidJson = JsonHelper.IsValidJson(responseContent);
                if (isValidJson)
                {
                    var dynamicsEntryCreationResponse = JsonConvert.DeserializeObject<DynamicsResponseDto>(responseContent);
                    _logger.LogError("Dynamics Response Error : {0} - {1} - {2}"
                        , applicationId, response.ReasonPhrase, dynamicsEntryCreationResponse?.Error?.Code, dynamicsEntryCreationResponse?.Error?.Message);
                    throw new QueueReaderException($"{applicationId} - {response.ReasonPhrase} - {dynamicsEntryCreationResponse?.Error?.Code} - {dynamicsEntryCreationResponse?.Error?.Message}");
                }
                else
                {
                    throw new QueueReaderException($"{applicationId} - {response.ReasonPhrase} - {responseContent}");
                }
            }
        }

        [Function("UpdateApplicationFromQueue")]
        public async Task UpdateApplicationFromQueue(
              [ServiceBusTrigger("%AzureServiceBusOptions:UpdateQueueName%", Connection = ServiceBusConnection)] string myQueueItem)
        {
                if (string.IsNullOrEmpty(myQueueItem))
                {
                    _logger.LogError("Invalid Queue Message :", myQueueItem);
                    throw new QueueReaderException("Invalid Queue Message :" + myQueueItem);
                }

                ApplicationUpdateQueueModel currentApplication = JsonConvert.DeserializeObject<ApplicationUpdateQueueModel>(myQueueItem);

                if (currentApplication == null || ((!currentApplication.Id.HasValue || currentApplication.Id == Guid.Empty) && (!currentApplication.DynamicId.HasValue || currentApplication.DynamicId == Guid.Empty)))
                {
                    _logger.LogError("Invalid Object from message : {0}", myQueueItem);
                    throw new QueueReaderException("Invalid Object from message :" + myQueueItem);
                }
                await _applicationService.UpdateApplicationStatus(currentApplication);
        }
    }
}

