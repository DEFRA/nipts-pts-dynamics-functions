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
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.OpenApi.Models;
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

        public QueueReader(
              IDynamicsService dynamicsService
            , IOptions<DynamicOptions> dynamicOptions
            , IApplicationService applicationService
            , IKeyVaultAccess keyVaultAccess
            , HttpClient httpClient)
        {
            _dynamicsService = dynamicsService;
            _dynamicOptions = dynamicOptions;
            _applicationService = applicationService;
            _keyVaultAccess = keyVaultAccess;
            _httpClient = httpClient;
        }


        [FunctionName("ReadApplicationFromQueue")]
        public async Task ReadApplicationFromQueue(
              [ServiceBusTrigger("%AzureServiceBusOptions:SubmitQueueName%", Connection = ServiceBusConnection)] string myQueueItem
            , ILogger log)
        {
            try
            {
                if (string.IsNullOrEmpty(myQueueItem))
                {
                    throw new QueueReaderException("Invalid Queue Message :" + myQueueItem);
                }

                ApplicationSubmittedMessageQueueModel currentApplication = JsonConvert.DeserializeObject<ApplicationSubmittedMessageQueueModel>(myQueueItem);
                if (currentApplication == null ||
                    currentApplication.ApplicationId == Guid.Empty)
                {
                    throw new QueueReaderException("Invalid Object from message :" + myQueueItem);
                }

                Guid applicationId = currentApplication.ApplicationId;
                log.LogInformation("Processing ApplicationId: ", applicationId.ToString());
                string apiVersion = _dynamicOptions.Value.ApiVersion;
                string serviceUrl = await _keyVaultAccess.GetSecretAsync("Pts-Dynamics-Tenant-ServiceUrl");
                string apiUrl = $"{serviceUrl}/api/data/v{apiVersion}/nipts_ptdapplications";

                string accessToken = await _dynamicsService.GetTokenForClient(new[] { $"{serviceUrl}/.default" });

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                log.LogInformation("Request Headers for application: ", applicationId.ToString(), _httpClient.DefaultRequestHeaders.ToString()); 

               var applicationObject = await _applicationService.GetApplication(applicationId);
                if(applicationObject == null)
                {
                    throw new QueueReaderException("Invalid Data Object so cannot Post to Dynamics");
                }

                string jsonData = JsonConvert.SerializeObject(applicationObject);
                log.LogInformation("Posting Json Payload: ", jsonData);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                log.LogInformation("POST Response for application: ",applicationId.ToString(), response);
                if (response.IsSuccessStatusCode)
                {
                    // Read and print the response content
                    log.LogInformation("POST request successful for ", applicationId.ToString());
                }
                else
                {
                    log.LogError("Error: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    if (response.Content == null || response.Content.Headers.ContentLength == 0)
                    {
                        throw new QueueReaderException($"Error Response Content is Null for {applicationId}");
                    }


                    // Log Error message from dynamics and Throw exception with details returned from Dynamics
                    var responseContent = await response.Content.ReadAsStringAsync();
                    log.LogInformation("Error Response content for application: ", applicationId.ToString(), responseContent);
                    bool isValidJson = JsonHelper.IsValidJson(responseContent);
                    if (isValidJson)
                    {
                        DynamicsResponseDto dynamicsEntryCreationResponse = JsonConvert.DeserializeObject<DynamicsResponseDto>(responseContent);
                        log.LogError("Dynamics Response Error : {0} - {1} - {2}"
                            ,applicationId, response.ReasonPhrase, dynamicsEntryCreationResponse.Error.Code, dynamicsEntryCreationResponse.Error.Message);
                        throw new QueueReaderException($"{applicationId} - {response.ReasonPhrase} - {dynamicsEntryCreationResponse.Error.Code} - {dynamicsEntryCreationResponse.Error.Message}");
                    }
                    else
                    {
                        throw new QueueReaderException($"{applicationId} - {response.ReasonPhrase} - {responseContent}");
                    }                   
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred.");
                throw;
            }
        }

        [FunctionName("UpdateApplicationFromQueue")]
        public async Task UpdateApplicationFromQueue(
              [ServiceBusTrigger("%AzureServiceBusOptions:UpdateQueueName%", Connection = ServiceBusConnection)] string myQueueItem
            , ILogger log)
        {
            try
            {
                if (string.IsNullOrEmpty(myQueueItem))
                {
                    log.LogError("Invalid Queue Message :", myQueueItem);
                    throw new QueueReaderException("Invalid Queue Message :" + myQueueItem);
                }
                ApplicationUpdateQueueModel currentApplication = JsonConvert.DeserializeObject<ApplicationUpdateQueueModel>(myQueueItem);
                if (currentApplication == null ||
                   currentApplication.Id == Guid.Empty)
                {
                    log.LogError("Invalid Object from message :" + myQueueItem);
                    throw new QueueReaderException("Invalid Object from message :" + myQueueItem);                    
                }                
                await _applicationService.UpdateApplicationStatus(currentApplication);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred.");
                throw;
            }
        }
    }
}

