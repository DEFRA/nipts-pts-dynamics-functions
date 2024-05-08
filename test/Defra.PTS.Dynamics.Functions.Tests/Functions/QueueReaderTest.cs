using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Dynamics.Functions.Functions;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Dynamics.Functions.Tests.Functions
{
    [TestFixture]
    public class QueueReaderTest
    {
        private Mock<IDynamicsService> _dynamicsServiceMock = new();
        private Mock<IOptions<DynamicOptions>> _dynamicOptionsMock = new();
        private Mock<IApplicationService> _applicationServiceMock = new();
        private Mock<IKeyVaultAccess> _keyVaultAcessMock = new();
        private Mock<HttpClient> _httpClientMock = new();
        private Mock<ILogger> _loggerMock = new();
        private QueueReader? _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, _httpClientMock.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _dynamicsServiceMock = new();
            _dynamicOptionsMock = new();
            _applicationServiceMock = new();
            _keyVaultAcessMock = new();
            _httpClientMock = new();
            _loggerMock = new();
        }

        [Test]
        public void ReadApplicationFromQueue_WithInValidParameters_ThrowsInvalidQueueException()
        {            
            string myQueueItem = string.Empty;
            var expectedResult = "Invalid Queue Message :" + myQueueItem;

            // Act
            var result =  Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest!.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result.Message);
        }
        
        [Test]
        public void ReadApplicationFromQueue_WithValidParameters_ThrowsInvalidObjectMessageException()
        {            
            string myQueueItem = "{}";
            var expectedResult = "Invalid Object from message :" + myQueueItem;

            // Act
            var result =  Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result.Message);
        }

        [Test]
        public void ReadApplicationFromQueue_ThrowsInavlidDataMessageException()
        {
            // Arrange            
            string myQueueItem = "{\"applicationId\":\"154098EF-EA92-4FB3-CA01-08DC1C336DA1\"}";
            var expectedResult = "Invalid Data Object so cannot Post to Dynamics";

            _dynamicsServiceMock
                .Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>()))
                .ReturnsAsync("fakeAccessToken"); // Set up a fake access token

            Application application = null;
            _applicationServiceMock
                .Setup(a => a.GetApplication(It.IsAny<Guid>()))
                .ReturnsAsync(application); // Set up a fake application object

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });

            string secretClientUrl = "http://google.com";
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);


            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("yourSuccessResponse"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);

            _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);


            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            // Assert
            // Add your assertions here based on the expected behavior
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once);
            _applicationServiceMock.Verify(a => a.GetApplication(It.IsAny<Guid>()), Times.Once);
            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.Once);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result.Message);


            handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Never(),
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                   ItExpr.IsAny<CancellationToken>());


        }

        [Test]
        public void ReadApplicationFromQueue_ThrowsInavlidSerilazationDataMessageException()
        {
            // Arrange            
            string myQueueItem = "{\"applicationId\":\"154098EF-EA92-4FB3-CA01-08DC1C336DA1\"}";
            var expectedResult = "Invalid Data Object so cannot Post to Dynamics";

            _dynamicsServiceMock
                .Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>()))
                .ReturnsAsync("fakeAccessToken"); // Set up a fake access token

            Application application = null;
            _applicationServiceMock
                .Setup(a => a.GetApplication(It.IsAny<Guid>()))
                .ReturnsAsync(application); // Set up a fake application object

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });

            string secretClientUrl = "http://google.com";
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);


            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("yourSuccessResponse"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);

            _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);


            // Act
            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            // Assert
            // Add your assertions here based on the expected behavior
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once);
            _applicationServiceMock.Verify(a => a.GetApplication(It.IsAny<Guid>()), Times.Once);
            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.Once);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result.Message);


            handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Never(),
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                   ItExpr.IsAny<CancellationToken>());


        }

        [Test]
        public async Task ReadApplicationFromQueue_Success()
        {
            // Arrange            
            string myQueueItem = "{\"applicationId\":\"154098EF-EA92-4FB3-CA01-08DC1C336DA1\"}";

            _dynamicsServiceMock
                .Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>()))
                .ReturnsAsync("fakeAccessToken"); // Set up a fake access token


            _applicationServiceMock
                .Setup(a => a.GetApplication(It.IsAny<Guid>()))
                .ReturnsAsync(new Application()); // Set up a fake application object

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope"});

            string secretClientUrl = "http://google.com";
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);

            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("yourSuccessResponse"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);

             _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);


            // Act
            await _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object);

            // Assert
            // Add your assertions here based on the expected behavior
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once);
            _applicationServiceMock.Verify(a => a.GetApplication(It.IsAny<Guid>()), Times.Once);
            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.Once);


            handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Exactly(1),
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                   ItExpr.IsAny<CancellationToken>());

        }

        [Test]
        public void ReadApplicationFromQueue_FailsOnPostToDynamics_ThrowsCustomException()
        {
            // Arrange
            string expectedResult = "154098ef-ea92-4fb3-ca01-08dc1c336da1 - Bad Gateway - 0x80048d19 - Error identified in Payload provided by the user for Entity :'nipts_ptdapplications', For more information on this error please follow this help link https://go.microsoft.com/fwlink/?linkid=2195293  ---->  InnerException : Microsoft.OData.ODataException: Cannot convert the literal 'Charity' to the expected type 'Edm.Int32'. ---> System.FormatException: Input string was not in a correct format.\r\n   at System.Number.StringToNumber(String str, NumberStyles options, NumberBuffer& number, NumberFormatInfo info, Boolean parseDecimal)\r\n   at System.Number.ParseInt32(String s, NumberStyles style, NumberFormatInfo info)\r\n   at System.String.System.IConvertible.ToInt32(IFormatProvider provider)\r\n   at System.Convert.ChangeType(Object value, Type conversionType, IFormatProvider provider)\r\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertStringValue(String stringValue, Type targetType)\r\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\r\n   --- End of inner exception stack trace ---\r\n   at Microsoft.OData.ODataPayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\r\n   at Microsoft.Crm.Extensibility.ODataV4.CrmPrimitivePayloadValueConverter.ConvertFromPayloadValue(Object value, IEdmTypeReference edmTypeReference)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightReaderUtils.ConvertValue(Object value, IEdmPrimitiveTypeReference primitiveTypeReference, ODataMessageReaderSettings messageReaderSettings, Boolean validateNullValue, String propertyName, ODataPayloadValueConverter converter)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightPropertyAndValueDeserializer.ReadPrimitiveValue(Boolean insideJsonObjectValue, IEdmPrimitiveTypeReference expectedValueTypeReference, Boolean validateNullValue, String propertyName)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightPropertyAndValueDeserializer.ReadNonEntityValueImplementation(String payloadTypeName, IEdmTypeReference expectedTypeReference, PropertyAndAnnotationCollector propertyAndAnnotationCollector, CollectionWithoutExpectedTypeValidator collectionValidator, Boolean validateNullValue, Boolean isTopLevelPropertyValue, Boolean insideResourceValue, String propertyName, Nullable`1 isDynamicProperty)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadEntryDataProperty(IODataJsonLightReaderResourceState resourceState, IEdmProperty edmProperty, String propertyTypeName)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadPropertyWithValue(IODataJsonLightReaderResourceState resourceState, String propertyName, Boolean isDeltaResourceSet)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.<>c__DisplayClass9_0.<ReadResourceContent>b__0(PropertyParsingResult propertyParsingResult, String propertyName)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightDeserializer.ProcessProperty(PropertyAndAnnotationCollector propertyAndAnnotationCollector, Func`2 readPropertyAnnotationValue, Action`2 handleProperty)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightResourceDeserializer.ReadResourceContent(IODataJsonLightReaderResourceState resourceState)\r\n   at Microsoft.OData.JsonLight.ODataJsonLightReader.ReadNextNestedInfo()\r\n   at Microsoft.OData.JsonLight.ODataJsonLightReader.ReadAtNestedResourceInfoEndImplementation()\r\n   at Microsoft.OData.ODataReaderCore.ReadImplementation()\r\n   at Microsoft.OData.ODataReaderCore.InterceptException[T](Func`1 action)\r\n   at System.Web.OData.Formatter.Deserialization.ODataReaderExtensions.ReadResourceOrResourceSet(ODataReader reader)\r\n   at System.Web.OData.Formatter.Deserialization.ODataResourceDeserializer.Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)\r\n   at System.Web.OData.Formatter.ODataMediaTypeFormatter.ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger).";            
            string myQueueItem = "{\"applicationId\":\"154098EF-EA92-4FB3-CA01-08DC1C336DA1\"}";

            _dynamicsServiceMock
                .Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>()))
                .ReturnsAsync("fakeAccessToken"); // Set up a fake access token


            _applicationServiceMock
                .Setup(a => a.GetApplication(It.IsAny<Guid>()))
                .ReturnsAsync(new Application()); // Set up a fake application object

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });

            string secretClientUrl = "http://google.com";
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);

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

            _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);


            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result!.Message);

            // Assert
            // Add your assertions here based on the expected behavior
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once);
            _applicationServiceMock.Verify(a => a.GetApplication(It.IsAny<Guid>()), Times.Once);
            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.Once);


            handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Exactly(1),
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                   ItExpr.IsAny<CancellationToken>());

        }

        [Test]
        public void ReadApplicationFromQueue_FailsOnPostToDynamics_ThrowsCustomException_LogsNullContent()
        {
            // Arrange
            string myQueueItem = "{\"applicationId\":\"154098EF-EA92-4FB3-CA01-08DC1C336DA1\"}";

            _dynamicsServiceMock
                .Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>()))
                .ReturnsAsync("fakeAccessToken"); // Set up a fake access token


            _applicationServiceMock
                .Setup(a => a.GetApplication(It.IsAny<Guid>()))
                .ReturnsAsync(new Application()); // Set up a fake application object

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });

            string secretClientUrl = "http://google.com";
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);

            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = null
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);

            _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);


            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            Assert.That(result, Is.Not.Null);

            // Assert
            // Add your assertions here based on the expected behavior
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once);
            _applicationServiceMock.Verify(a => a.GetApplication(It.IsAny<Guid>()), Times.Once);
            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.Once);


            handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Exactly(1),
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                   ItExpr.IsAny<CancellationToken>());

        }

        [Test]
        public void ReadApplicationFromQueue_FailsOnPostToDynamicsFor_BadRequest_ThrowsCustomException()
        {
            // Arrange
            string expectedResult = "154098ef-ea92-4fb3-ca01-08dc1c336da1 - Bad Request - <!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\"\"http://www.w3.org/TR/html4/strict.dtd\">\r\n<HTML><HEAD><TITLE>Bad Request</TITLE>\r\n<META HTTP-EQUIV=\"Content-Type\" Content=\"text/html; charset=us-ascii\"></HEAD>\r\n<BODY><h2>Bad Request - Request Too Long</h2>\r\n<hr><p>HTTP Error 400. The size of the request headers is too long.</p>\r\n</BODY></HTML>";            
            string myQueueItem = "{\"applicationId\":\"154098EF-EA92-4FB3-CA01-08DC1C336DA1\"}";

            _dynamicsServiceMock
                .Setup(ds => ds.GetTokenForClient(It.IsAny<string[]>()))
                .ReturnsAsync("fakeAccessToken"); // Set up a fake access token


            _applicationServiceMock
                .Setup(a => a.GetApplication(It.IsAny<Guid>()))
                .ReturnsAsync(new Application()); // Set up a fake application object

            _dynamicOptionsMock.Setup(a => a.Value).Returns(new DynamicOptions() { ApiVersion = "1.0", Authority = "authority", Scopes = "scope" });

            string secretClientUrl = "http://google.com";
            _keyVaultAcessMock.Setup(a => a.GetSecretAsync(It.IsAny<string>())).ReturnsAsync(secretClientUrl);

            // Mock the behavior of HttpClient
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\"\"http://www.w3.org/TR/html4/strict.dtd\">\r\n<HTML><HEAD><TITLE>Bad Request</TITLE>\r\n<META HTTP-EQUIV=\"Content-Type\" Content=\"text/html; charset=us-ascii\"></HEAD>\r\n<BODY><h2>Bad Request - Request Too Long</h2>\r\n<hr><p>HTTP Error 400. The size of the request headers is too long.</p>\r\n</BODY></HTML>"),
            };

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            var httpClientMock = new HttpClient(handlerMock.Object);

            _systemUnderTest = new QueueReader(_dynamicsServiceMock.Object, _dynamicOptionsMock.Object, _applicationServiceMock.Object, _keyVaultAcessMock.Object, httpClientMock);


            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.ReadApplicationFromQueue(myQueueItem, _loggerMock.Object));

            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result!.Message);

            // Assert
            // Add your assertions here based on the expected behavior
            _dynamicsServiceMock.Verify(ds => ds.GetTokenForClient(It.IsAny<string[]>()), Times.Once);
            _applicationServiceMock.Verify(a => a.GetApplication(It.IsAny<Guid>()), Times.Once);
            _dynamicOptionsMock.Verify(a => a.Value, Times.Once);
            _keyVaultAcessMock.Verify(a => a.GetSecretAsync(It.IsAny<string>()), Times.Once);


            handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Exactly(1),
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                   ItExpr.IsAny<CancellationToken>());

        }

        [Test]
        public void UpdateApplicationFromQueue_WithValidParameters_ThrowsInvalidQueueException()
        {            
            string myQueueItem = string.Empty;
            var expectedResult = "Invalid Queue Message :" + myQueueItem;

            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest!.UpdateApplicationFromQueue(myQueueItem, _loggerMock.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result!.Message);
        }

        [Test]
        public void UpdateApplicationFromQueue_WithValidParameters_ThrowsInvalidObjectMessageException()
        {            
            string myQueueItem = "{}";
            var expectedResult = "Invalid Object from message :" + myQueueItem;

            // Act
            var result = Assert.ThrowsAsync<QueueReaderException>(() => _systemUnderTest.UpdateApplicationFromQueue(myQueueItem, _loggerMock.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result.Message);
        }


        [Test]
        public void UpdateApplicationFromQueue_WithValidParameters_Success()
        {            
            string myQueueItem = "{ \"Application.Id \": \"4c75144b-af99-4a50-3011-08dc224aa9d8\",\r\n  \"Application.DynamicId\": \"64215130-55c0-ee11-9078-6045bdf4d4f8\",\r\n  \"Application.StatusId\": \"Authorised\", \"Application.DateAuthorised\": \"2024-02-01\"}";            
            Guid appId = Guid.NewGuid();

            _applicationServiceMock
                .Setup(a => a.UpdateApplicationStatus(It.IsAny<ApplicationUpdateQueueModel>()))
                .ReturnsAsync(appId);

            // Act
            var result = _systemUnderTest!.UpdateApplicationFromQueue(myQueueItem, _loggerMock.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            _applicationServiceMock
               .Verify(a => a.UpdateApplicationStatus(It.IsAny<ApplicationUpdateQueueModel>()), Times.Once());
        }
    }
}
