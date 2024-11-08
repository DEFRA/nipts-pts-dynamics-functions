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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Model = Defra.PTS.Common.Models;

namespace Defra.PTS.Dynamics.Functions.Functions;


public class FetchUpdateAddress
{
    private readonly ILogger<FetchUpdateAddress> _logger;
    private readonly IDynamicsService _dynamicsService;
    private readonly IOptions<DynamicOptions> _dynamicOptions;
    private readonly IUserService _userService;
    private readonly IKeyVaultAccess _keyVaultAccess;
    private readonly HttpClient _httpClient;

    private const string TagName = "FetchAndUpdateAddress";


    public FetchUpdateAddress(
          ILogger<FetchUpdateAddress> log
        , IDynamicsService dynamicsService
        , IOptions<DynamicOptions> dynamicOptions
        , IUserService userService
        , IKeyVaultAccess keyVaultAccess
        , HttpClient httpClient)
    {
        _logger = log;
        _dynamicsService = dynamicsService;
        _dynamicOptions = dynamicOptions;
        _userService = userService;
        _keyVaultAccess = keyVaultAccess;
        _httpClient = httpClient;
    }

    [FunctionName("FetchAndUpdateAddress")]
    [OpenApiOperation(operationId: "FetchAndUpdateAddress", tags: new[] { TagName })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Model.UserRequest), Description = "Sync User Details from Dynamics")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Description = "The NotFound response")]
    public async Task<IActionResult> FetchAndUpdateAddress(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fetchupdateaddress")] HttpRequest req)
    {

        var inputData = req?.Body;
        if (inputData == null)
        {
            throw new UserFunctionException("Invalid user input, is NUll or Empty");
        }

        var userRequestModel = await _userService.GetUserRequestModel(inputData);

        var userExist = await _userService.DoesUserExists(userRequestModel.ContactId.GetValueOrDefault());
        if (!userExist)
        {
            return new NotFoundObjectResult($"User does not exist for this contact: {userRequestModel.ContactId}");
        }

        var addressId = await SyncDynamicsContactDetailsToUser(userRequestModel);

        return new OkObjectResult(addressId);
    }

    private async Task<Guid?> SyncDynamicsContactDetailsToUser
        (UserRequest userRequestModel)
    {
        Guid? addressId = Guid.Empty;
        HttpResponseMessage response;
        string responseContent;
        var count = 10;

        (Guid? userId, Guid? existingAddressId, string email) = _userService.GetUserDetails(userRequestModel.ContactId.GetValueOrDefault());

        await AddHttpHeaders();

        var apiUrl = await GetApiUrl(userRequestModel.ContactId.GetValueOrDefault());

        do
        {
            response = await _httpClient.GetAsync(apiUrl);
            responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && addressId == Guid.Empty)
            {
                var json = JObject.Parse(responseContent);

                userRequestModel.Address.AddressLineOne = json["defra_addrcorsubbuildingname"]?.Value<string>();
                userRequestModel.Address.AddressLineOne = userRequestModel.Address?.AddressLineOne + " " + json["defra_addrcorbuildingnumber"]?.Value<string>();
                userRequestModel.Address.AddressLineTwo = json["defra_addrcorbuildingname"]?.Value<string>();
                userRequestModel.Address.AddressLineTwo = userRequestModel.Address?.AddressLineTwo + " " + json["defra_addrcorstreet"]?.Value<string>();
                userRequestModel.Address.TownOrCity = json["defra_addrcortown"]?.Value<string>();
                userRequestModel.Address.PostCode = json["defra_addrcorpostcode"]?.Value<string>();
                userRequestModel.Address.County = json["defra_addrcorcounty"]?.Value<string>();

                var telephone = json["telephone1"]?.Value<string>();

                if (existingAddressId.HasValue && existingAddressId.Value != Guid.Empty)
                {
                    userRequestModel.Address.UpdatedBy = userId;

                    addressId = await _userService.UpdateAddress(userRequestModel, existingAddressId.Value);
                }
                else
                {
                    userRequestModel.Address.CreatedBy = userId;

                    addressId = await _userService.AddAddress(userRequestModel);
                }
                string firstName = json["firstname"]?.Value<string>();
                string lastName = json["lastname"]?.Value<string>();
                await _userService.UpdateUser(firstName, lastName, email, telephone, addressId);
            }

            if (ResponseNotValid(response.IsSuccessStatusCode, addressId, count))
            {
                Thread.Sleep(3000);
            }

            count--;
        } while (ResponseNotValid(response.IsSuccessStatusCode, addressId, count));

        if (addressId == Guid.Empty)
        {
            bool isValidJson = JsonHelper.IsValidJson(responseContent);
            if (isValidJson)
            {        
                DynamicsResponseDto dynamicsEntryCreationResponse = JsonConvert.DeserializeObject<DynamicsResponseDto>(responseContent);
                _logger.LogError("Error: {StatusCode} - {ReasonPhrase} Dynamics Response Error : {Code} - {Message}", response.StatusCode, response.ReasonPhrase, dynamicsEntryCreationResponse.Error.Code, dynamicsEntryCreationResponse.Error.Message);
                throw new UserFunctionException($"{response.ReasonPhrase} - {dynamicsEntryCreationResponse.Error.Code} - {dynamicsEntryCreationResponse.Error.Message}");
            }
            else
            {
                throw new QueueReaderException($"{userRequestModel.ContactId} - {response.ReasonPhrase} - {responseContent}");
            }
        }

        return addressId;
    }

    private static bool ResponseNotValid(bool successStatusCode, Guid? addressId, int count) =>
        !successStatusCode && addressId == Guid.Empty && count > 0;

    private async Task AddHttpHeaders()
    {
        var serviceUrl = await _keyVaultAccess.GetSecretAsync("Pts-Dynamics-Tenant-ServiceUrl");
        var accessToken = await _dynamicsService.GetTokenForClient(new[] { $"{serviceUrl}/.default" });

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<string> GetApiUrl(Guid contactId)
    {
        var apiVersion = _dynamicOptions.Value.ApiVersion;
        var serviceUrl = await _keyVaultAccess.GetSecretAsync("Pts-Dynamics-Tenant-ServiceUrl");
        var apiUrl = $"{serviceUrl}/api/data/v{apiVersion}/contacts({contactId})?$select=\r\ndefra_addrcorbuildingnumber,defra_addrcorbuildingname,defra_addrcorsubbuildingname,defra_addrcorstreet,defra_addrcortown,defra_addrcorcounty,defra_addrcorpostcode,defra_addrcorcountry,telephone1,firstname,lastname";

        return apiUrl;
    }
}

