using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Dynamics.Functions.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Dynamics.Functions.Tests.Functions
{
    [TestFixture]
    public class FetchUpdateAddressTest
    {
        private Mock<HttpRequest>? _mockRequest;
        private Mock<IDynamicsService> _dynamicsServiceMock;
        private Mock<IOptions<DynamicOptions>> _dynamicOptionsMock;
        private Mock<IUserService> _userServiceMock;
        private Mock<IKeyVaultAccess> _keyVaultAcessMock;
        private Mock<HttpClient> _httpClientMock;
        private Mock<ILogger<FetchUpdateAddress>> _loggerMock;
        private FetchUpdateAddress _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _mockRequest = new Mock<HttpRequest>();
            _dynamicsServiceMock = new Mock<IDynamicsService>();
            _dynamicOptionsMock = new Mock<IOptions<DynamicOptions>>();
            _userServiceMock = new Mock<IUserService>();
            _httpClientMock = new Mock<HttpClient>();
            _keyVaultAcessMock = new Mock<IKeyVaultAccess>();
            _loggerMock = new Mock<ILogger<FetchUpdateAddress>>();

            _systemUnderTest = new FetchUpdateAddress(_loggerMock.Object, _dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _userServiceMock.Object, _keyVaultAcessMock.Object, _httpClientMock.Object);
        }

        [Test]
        public void Run_WithInValidParameters_ThrowsInvalidUserInput_Exception()
        {
            var expectedResult = "Invalid user input, is NUll or Empty";
            MemoryStream stream = null;
            _mockRequest!.Setup(x => x.Body).Returns(stream);

            // Act
            var result = Assert.ThrowsAsync<UserFunctionException>(() => _systemUnderTest.FetchAndUpdateAddress(_mockRequest.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result?.Message);
        }


        [Test]
        public async Task Run_WithInValidParameters_ThrowsInvalidQueueException()
        {            
            var json = JsonConvert.SerializeObject(new { });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            
            _mockRequest!.Setup(x => x.Body).Returns(stream);

            var userRequest = new UserRequest()
            {
                ContactId = Guid.NewGuid(),
                Address = new Address()
                {
                    AddressLineOne = "1",
                    AddressLineTwo = "2",
                    AddressType = "test",
                    CountryName = "Country",
                    County = "County",
                    IsActive = true,
                    Id = Guid.NewGuid(),
                    PostCode = "TTN 1wx",
                    TownOrCity = "City",
                }
            };

            var expectedResult = "User does not exist for this contact: " + userRequest.ContactId.ToString();
            _userServiceMock.Setup(a => a.GetUserRequestModel(It.IsAny<Stream>())).ReturnsAsync(userRequest);
            _userServiceMock.Setup(a => a.DoesUserExists((Guid)userRequest.ContactId)).ReturnsAsync(false);

            // Act
            var result = await _systemUnderTest.FetchAndUpdateAddress(_mockRequest.Object);
            var objectResult = result as ObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(404, objectResult!.StatusCode);
            Assert.AreEqual(expectedResult, objectResult!.Value?.ToString());
        }

        [Test]
        public async Task Run_WithValidParameters_WithUserDetails_Successful_Update()
        {
           string secretClientUrl = "http://gle.com";
           var json = JsonConvert.SerializeObject(new { });
           var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

           _mockRequest!.Setup(x => x.Body).Returns(stream);

           var userRequest = new UserRequest()
           {
               ContactId = Guid.NewGuid(),
               Address = new Address()
               {
                   AddressLineOne = "1",
                   AddressLineTwo = "2",
                   AddressType = "test",
                   CountryName = "Country",
                   County = "County",
                   IsActive = true,
                   Id = Guid.NewGuid(),
                   PostCode = "TTN 1wx",
                   TownOrCity = "City",
               }
           };
           Guid addressId = Guid.Parse("6B29FC40-CA47-1067-B31D-00DD010662DA");

           var userDetails = (Guid.Parse("6B29FC40-CA47-1067-B31D-00DD010662DA"), Guid.Parse("6B29FC40-CA47-1067-B31D-00DD010662DA"), "test@email.com");

           _userServiceMock.Setup(a => a.GetUserRequestModel(It.IsAny<Stream>())).ReturnsAsync(userRequest);
           _userServiceMock.Setup(a => a.DoesUserExists((Guid)userRequest.ContactId)).ReturnsAsync(true);
           _userServiceMock.Setup(a => a.GetUserDetails((Guid)userRequest.ContactId)).Returns(userDetails);
           _userServiceMock.Setup(a => a.AddAddress(It.IsAny<UserRequest>())).ReturnsAsync(addressId);
           _userServiceMock.Setup(a => a.UpdateAddress(It.IsAny<UserRequest>(), It.IsAny<Guid>())).ReturnsAsync(addressId);
           _userServiceMock.Setup(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId)).ReturnsAsync(addressId);

           _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });
           _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);
           _dynamicsServiceMock.Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>())).ReturnsAsync("fakeAccessToken"); // Set up a fake access token



           // Mock the behavior of HttpClient
           var handlerMock = new Mock<HttpMessageHandler>();
           var response = new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("{\"@odata.context\":\"https://defra-trade-int-plants.crm4.dynamics.com/api/data/v9.1/$metadata#contacts(defra_addrcorbuildingnumber,defra_addrcorbuildingname,defra_addrcorstreet,defra_addrcortown,defra_addrcorcounty,defra_addrcorpostcode,defra_addrcorcountry)/$entity\",\"@odata.etag\":\"W/\\\"179124370\\\"\",\"defra_addrcorbuildingnumber\":\"2\",\"defra_addrcorbuildingname\":null,\"defra_addrcorstreet\":\"HEATHFIELD PARK DRIVE\",\"defra_addrcortown\":\"ROMFORD\",\"defra_addrcorcounty\":\"REDBRIDGE\",\"defra_addrcorpostcode\":\"RM6 4FB\",\"contactid\":\"2ba8c954-d5ba-ee11-a569-6045bd905113\"}"),
           };

           handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
             "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(response);

           var httpClientMock = new HttpClient(handlerMock.Object);
           _systemUnderTest = new FetchUpdateAddress(_loggerMock.Object, _dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _userServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);

           // Act
           var result = await _systemUnderTest.FetchAndUpdateAddress(_mockRequest.Object);
           var objectResult = result as ObjectResult;

           // Assert
           Assert.That(result, Is.Not.Null);
           Assert.AreEqual(200, objectResult?.StatusCode);
           Assert.AreEqual(addressId, objectResult?.Value);


           _userServiceMock.Verify(a => a.GetUserRequestModel(It.IsAny<Stream>()), Times.Once);
           _userServiceMock.Verify(a => a.DoesUserExists((Guid)userRequest.ContactId), Times.Once);
           _userServiceMock.Verify(a => a.GetUserDetails((Guid)userRequest.ContactId), Times.Once);
           _userServiceMock.Verify(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId), Times.Once);

           _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
           _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.AtLeastOnce);
           _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once); // Set up a fake access token
        }

        [Test]
        public async Task Run_WithInValidParameters_Successful_Update()
        {
            string secretClientUrl = "http://gle.com";
            var json = JsonConvert.SerializeObject(new { });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            _mockRequest!.Setup(x => x.Body).Returns(stream);

            var userRequest = new UserRequest()
            {
                ContactId = Guid.NewGuid(),
                Address = new Address()
                {
                    AddressLineOne = "1",
                    AddressLineTwo = "2",
                    AddressType = "test",
                    CountryName = "Country",
                    County = "County",
                    IsActive = true,
                    Id = Guid.NewGuid(),
                    PostCode = "TTN 1wx",
                    TownOrCity = "City",
                }
            };
            Guid addressId = Guid.NewGuid();

            _userServiceMock.Setup(a => a.GetUserRequestModel(It.IsAny<Stream>())).ReturnsAsync(userRequest);
            _userServiceMock.Setup(a => a.DoesUserExists((Guid)userRequest.ContactId)).ReturnsAsync(true);
            _userServiceMock.Setup(a => a.GetUserDetails((Guid)userRequest.ContactId)).Returns((null, null, null));
            _userServiceMock.Setup(a => a.AddAddress(It.IsAny<UserRequest>())).ReturnsAsync(addressId);
            _userServiceMock.Setup(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId)).ReturnsAsync(Guid.NewGuid());

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);
            _dynamicsServiceMock.Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>())).ReturnsAsync("fakeAccessToken"); // Set up a fake access token
            


            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"@odata.context\":\"https://defra-trade-int-plants.crm4.dynamics.com/api/data/v9.1/$metadata#contacts(defra_addrcorbuildingnumber,defra_addrcorbuildingname,defra_addrcorstreet,defra_addrcortown,defra_addrcorcounty,defra_addrcorpostcode,defra_addrcorcountry)/$entity\",\"@odata.etag\":\"W/\\\"179124370\\\"\",\"defra_addrcorbuildingnumber\":\"2\",\"defra_addrcorbuildingname\":null,\"defra_addrcorstreet\":\"HEATHFIELD PARK DRIVE\",\"defra_addrcortown\":\"ROMFORD\",\"defra_addrcorcounty\":\"REDBRIDGE\",\"defra_addrcorpostcode\":\"RM6 4FB\",\"contactid\":\"2ba8c954-d5ba-ee11-a569-6045bd905113\"}"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);
            _systemUnderTest = new FetchUpdateAddress(_loggerMock.Object, _dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _userServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);

            // Act
            var result = await _systemUnderTest.FetchAndUpdateAddress(_mockRequest.Object);
            var objectResult = result as ObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(200, objectResult?.StatusCode);            
            Assert.AreEqual(addressId, objectResult?.Value); 


            _userServiceMock.Verify(a => a.GetUserRequestModel(It.IsAny<Stream>()), Times.Once);
            _userServiceMock.Verify(a => a.DoesUserExists((Guid)userRequest.ContactId), Times.Once);
            _userServiceMock.Verify(a => a.GetUserDetails((Guid)userRequest.ContactId), Times.Once);
            _userServiceMock.Verify(a => a.AddAddress(It.IsAny<UserRequest>()), Times.Once);
            _userServiceMock.Verify(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId), Times.Once);

            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.AtLeastOnce);
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once); // Set up a fake access token
        }

        [Test]
        public void Run_WithInValidParameters_UnsuccessfulGet_ThrowsCustomException()
        {
            string expectedResult = "Bad Gateway - 0x80048d19 - Error identified in Payload provided by the user for Entity :'nipts_ptdapplications', For more information on this error please follow this help link https://go.microsoft.com/fwlink/?linkid=2195293  ---->  InnerException : Microsoft.OData.ODataException: Cannot convert the literal 'Charity' to the expected type 'Edm.Int32'. ---> System.FormatException: Input string was not in a correct format.\r\n   at System.Number.StringToNumber(String str, NumberStyles options, NumberBuffer& number, NumberFormatInfo info, Boolean parseDecimal)\r\n   at System.Number.ParseInt32(String s, NumberStyles style, NumberFormatInfo info)\r\n   at System.String.System.IConvertible.ToInt32(IFormatProvider provider)\r\n   at System.Convert.ChangeType(Object value, Type conversionType, IFormatProvider provider)\r\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertStringValue(String stringValue, Type targetType)\r\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\r\n   --- End of inner exception stack trace ---\r\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\r\n   at Microsoft.Crm.Extensibility.ODataV4.CrmPrimitivePayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightReaderUtils.ConvertValue(Object value, IEdmPrimitiveTypeReference primitiveTypeReference, ODataMessageReaderSettings messageReaderSettings, Boolean validateNullValue, String propertyName, ODataPayloadValueConverter converter)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightPropertyAndValueDeserializer.ReadPrimitiveValue(Boolean insideJsonObjectValue, IEdmPrimitiveTypeReference expectedValueTypeReference, Boolean validateNullValue, String propertyName)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightPropertyAndValueDeserializer.ReadNonEntityValueImplementation(String payloadTypeName, IEdmTypeReference expectedTypeReference, PropertyAndAnnotationCollector propertyAndAnnotationCollector, CollectionWithoutExpectedTypeValidator collectionValidator, Boolean validateNullValue, Boolean isTopLevelPropertyValue, Boolean insideResourceValue, String propertyName, Nullable`1 isDynamicProperty)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadEntryDataProperty(IODataJsonLightReaderResourceState resourceState, IEdmProperty edmProperty, String propertyTypeName)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadPropertyWithValue(IODataJsonLightReaderResourceState resourceState, String propertyName, Boolean isDeltaResourceSet)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.<>c__DisplayClass9_0.<ReadResourceContent>b__0(PropertyParsingResult propertyParsingResult, String propertyName)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightDeserializer.ProcessProperty(PropertyAndAnnotationCollector propertyAndAnnotationCollector, Func`2 readPropertyAnnotationValue, Action`2 handleProperty)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadResourceContent(IODataJsonLightReaderResourceState resourceState)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightReader.ReadNextNestedInfo()\r\n   at Microsoft.OData.JsonLight.ODataJsonLightReader.ReadAtNestedResourceInfoEndImplementation()\r\n   at Microsoft.OData.ODataReaderCore.ReadImplementation()\r\n   at Microsoft.OData.ODataReaderCore.InterceptException[T](Func`1 action)\r\n   at System.Web.OData.Formatter.Deserialization.ODataReaderExtensions.ReadResourceOrResourceSet(ODataReader reader)\r\n   at System.Web.OData.Formatter.Deserialization.ODataResourceDeserializer.Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)\r\n   at System.Web.OData.Formatter.ODataMediaTypeFormatter.ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger).";
            string secretClientUrl = "http://gle.com";
            var json = JsonConvert.SerializeObject(new { });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            _mockRequest!.Setup(x => x.Body).Returns(stream);

            var userRequest = new UserRequest()
            {
                ContactId = Guid.NewGuid(),
                Address = new Address()
                {
                    AddressLineOne = "1",
                    AddressLineTwo = "2",
                    AddressType = "test",
                    CountryName = "Country",
                    County = "County",
                    IsActive = true,
                    Id = Guid.NewGuid(),
                    PostCode = "TTN 1wx",
                    TownOrCity = "City",
                }
            };
            
            Guid addressId = Guid.NewGuid();

            _userServiceMock.Setup(a => a.GetUserRequestModel(It.IsAny<Stream>())).ReturnsAsync(userRequest);
            _userServiceMock.Setup(a => a.DoesUserExists((Guid)userRequest.ContactId)).ReturnsAsync(true);
            _userServiceMock.Setup(a => a.AddAddress(It.IsAny<UserRequest>())).ReturnsAsync(addressId);
            _userServiceMock.Setup(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId)).ReturnsAsync(Guid.NewGuid());

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);
            _dynamicsServiceMock.Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>())).ReturnsAsync("fakeAccessToken"); // Set up a fake access token



            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent("{\r\n    \"error\": {\r\n        \"code\": \"0x80048d19\",\r\n        \"message\": \"Error identified in Payload provided by the user for Entity :'nipts_ptdapplications', For more information on this error please follow this help link https://go.microsoft.com/fwlink/?linkid=2195293  ---->  InnerException : Microsoft.OData.ODataException: Cannot convert the literal 'Charity' to the expected type 'Edm.Int32'. ---> System.FormatException: Input string was not in a correct format.\\r\\n   at System.Number.StringToNumber(String str, NumberStyles options, NumberBuffer& number, NumberFormatInfo info, Boolean parseDecimal)\\r\\n   at System.Number.ParseInt32(String s, NumberStyles style, NumberFormatInfo info)\\r\\n   at System.String.System.IConvertible.ToInt32(IFormatProvider provider)\\r\\n   at System.Convert.ChangeType(Object value, Type conversionType, IFormatProvider provider)\\r\\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertStringValue(String stringValue, Type targetType)\\r\\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\\r\\n   --- End of inner exception stack trace ---\\r\\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\\r\\n   at Microsoft.Crm.Extensibility.ODataV4.CrmPrimitivePayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightReaderUtils.ConvertValue(Object value, IEdmPrimitiveTypeReference primitiveTypeReference, ODataMessageReaderSettings messageReaderSettings, Boolean validateNullValue, String propertyName, ODataPayloadValueConverter converter)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightPropertyAndValueDeserializer.ReadPrimitiveValue(Boolean insideJsonObjectValue, IEdmPrimitiveTypeReference expectedValueTypeReference, Boolean validateNullValue, String propertyName)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightPropertyAndValueDeserializer.ReadNonEntityValueImplementation(String payloadTypeName, IEdmTypeReference expectedTypeReference, PropertyAndAnnotationCollector propertyAndAnnotationCollector, CollectionWithoutExpectedTypeValidator collectionValidator, Boolean validateNullValue, Boolean isTopLevelPropertyValue, Boolean insideResourceValue, String propertyName, Nullable`1 isDynamicProperty)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadEntryDataProperty(IODataJsonLightReaderResourceState resourceState, IEdmProperty edmProperty, String propertyTypeName)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadPropertyWithValue(IODataJsonLightReaderResourceState resourceState, String propertyName, Boolean isDeltaResourceSet)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.<>c__DisplayClass9_0.<ReadResourceContent>b__0(PropertyParsingResult propertyParsingResult, String propertyName)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightDeserializer.ProcessProperty(PropertyAndAnnotationCollector propertyAndAnnotationCollector, Func`2 readPropertyAnnotationValue, Action`2 handleProperty)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadResourceContent(IODataJsonLightReaderResourceState resourceState)\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightReader.ReadNextNestedInfo()\\r\\n   at Microsoft.OData.JsonLight.ODataJsonLightReader.ReadAtNestedResourceInfoEndImplementation()\\r\\n   at Microsoft.OData.ODataReaderCore.ReadImplementation()\\r\\n   at Microsoft.OData.ODataReaderCore.InterceptException[T](Func`1 action)\\r\\n   at System.Web.OData.Formatter.Deserialization.ODataReaderExtensions.ReadResourceOrResourceSet(ODataReader reader)\\r\\n   at System.Web.OData.Formatter.Deserialization.ODataResourceDeserializer.Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)\\r\\n   at System.Web.OData.Formatter.ODataMediaTypeFormatter.ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger).\"\r\n    }\r\n}"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);
            _systemUnderTest = new FetchUpdateAddress(_loggerMock.Object, _dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _userServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);

            // Act
            var result = Assert.ThrowsAsync<UserFunctionException>(() => _systemUnderTest.FetchAndUpdateAddress(_mockRequest.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result?.Message);

            _userServiceMock.Verify(a => a.GetUserRequestModel(It.IsAny<Stream>()), Times.Once);
            _userServiceMock.Verify(a => a.DoesUserExists((Guid)userRequest.ContactId), Times.Once);
            _userServiceMock.Verify(a => a.GetUserDetails((Guid)userRequest.ContactId), Times.Once);
            _userServiceMock.Verify(a => a.AddAddress(It.IsAny<UserRequest>()), Times.Never);
            _userServiceMock.Verify(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId), Times.Never);

            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.AtLeastOnce);
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once); // Set up a fake access token
        }

        [Test]
        public void Run_WithInValidParameters_UnsuccessfulGet_JunkResponse_ThrowsCustomException()
        {
            string secretClientUrl = "http://gle.com";
            var json = JsonConvert.SerializeObject(new { });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            _mockRequest!.Setup(x => x.Body).Returns(stream);

            var userRequest = new UserRequest()
            {
                ContactId = Guid.NewGuid(),
                Address = new Address()
                {
                    AddressLineOne = "1",
                    AddressLineTwo = "2",
                    AddressType = "test",
                    CountryName = "Country",
                    County = "County",
                    IsActive = true,
                    Id = Guid.NewGuid(),
                    PostCode = "TTN 1wx",
                    TownOrCity = "City",
                }
            };

            Guid addressId = Guid.NewGuid();

            _userServiceMock.Setup(a => a.GetUserRequestModel(It.IsAny<Stream>())).ReturnsAsync(userRequest);
            _userServiceMock.Setup(a => a.DoesUserExists((Guid)userRequest.ContactId)).ReturnsAsync(true);
            _userServiceMock.Setup(a => a.AddAddress(It.IsAny<UserRequest>())).ReturnsAsync(addressId);
            _userServiceMock.Setup(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId)).ReturnsAsync(Guid.NewGuid());

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);
            _dynamicsServiceMock.Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>())).ReturnsAsync("fakeAccessToken"); // Set up a fake access token



            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent("Junk"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);
            _systemUnderTest = new FetchUpdateAddress(_loggerMock.Object, _dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _userServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);

            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.FetchAndUpdateAddress(_mockRequest.Object));

            // Assert
            Assert.That(result, Is.Not.Null);

            _userServiceMock.Verify(a => a.GetUserRequestModel(It.IsAny<Stream>()), Times.Once);
            _userServiceMock.Verify(a => a.DoesUserExists((Guid)userRequest.ContactId), Times.Once);
            _userServiceMock.Verify(a => a.GetUserDetails((Guid)userRequest.ContactId), Times.Once);
            _userServiceMock.Verify(a => a.AddAddress(It.IsAny<UserRequest>()), Times.Never);
            _userServiceMock.Verify(a => a.UpdateUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), addressId), Times.Never);

            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.AtLeastOnce);
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once); // Set up a fake access token
        }

    }
}
