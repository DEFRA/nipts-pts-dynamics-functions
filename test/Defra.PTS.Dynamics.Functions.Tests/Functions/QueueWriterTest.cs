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
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Dynamics.Functions.Tests.Functions
{
    public class QueueWriterTest
    {
        private Mock<HttpRequest>? _mockRequest;
        private Mock<IApplicationService>? _applicationServiceMock;
        private Mock<IServiceBusService>? _serviceBusServiceMock;
        private QueueWriter? _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _mockRequest = new Mock<HttpRequest>();
            _applicationServiceMock = new Mock<IApplicationService>();
            _serviceBusServiceMock = new Mock<IServiceBusService>();

            _systemUnderTest = new QueueWriter(_applicationServiceMock.Object, _serviceBusServiceMock.Object);
        }

        [Test]
        public void WriteApplicationToQueue_WithInValidParameters_ThrowsInvalidQueueMessageInputException()
        {
            var expectedResult = "Invalid Queue message input, is NUll or Empty";
            MemoryStream? stream = null;
            _mockRequest!.Setup(x => x.Body).Returns(stream!);

            // Act
            var result = Assert.ThrowsAsync<QueueWriterException>(() => _systemUnderTest!.WriteApplicationToQueue(_mockRequest.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result?.Message);
        }

        [Test]
        public void WriteApplicationToQueue_WithInValidParameters_ThrowsInvalidQueueMessageModelException()
        {
            var expectedResult = "Invalid QueueMessage Model, is Null or Empty";
            var json = JsonConvert.SerializeObject(new { });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            _mockRequest!.Setup(x => x.Body).Returns(stream);

            ApplicationSubmittedMessageQueueModel? applicationSubmittedMessageQueueModel = null;
            _applicationServiceMock!.Setup(a => a.GetApplicationQueueModel(It.IsAny<Stream>())!)!.ReturnsAsync(applicationSubmittedMessageQueueModel);

            // Act
            var result = Assert.ThrowsAsync<QueueWriterException>(() => _systemUnderTest!.WriteApplicationToQueue(_mockRequest.Object));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result?.Message);

            _applicationServiceMock.Verify(a => a.GetApplicationQueueModel(It.IsAny<Stream>()), Times.Once);
        }


        [Test]
        public async Task WriteApplicationToQueue_WithInValidParameters_Success()
        {
            string expectedResult = $"Added Message to Queue Successfully";

            var json = JsonConvert.SerializeObject(new { });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            _mockRequest!.Setup(x => x.Body).Returns(stream);

            ApplicationSubmittedMessageQueueModel applicationSubmittedMessageQueueModel = new ApplicationSubmittedMessageQueueModel();
            _applicationServiceMock!.Setup(a => a.GetApplicationQueueModel(It.IsAny<Stream>())).ReturnsAsync(applicationSubmittedMessageQueueModel);

            _serviceBusServiceMock!.Setup(a => a.SendMessageAsync(It.IsAny<ApplicationSubmittedMessageQueueModel>()));

            // Act
            var result = await _systemUnderTest!.WriteApplicationToQueue(_mockRequest.Object);
            var objectResult = result as ObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(200, objectResult?.StatusCode);
            Assert.AreEqual(expectedResult, objectResult?.Value?.ToString());

            _applicationServiceMock.Verify(a => a.GetApplicationQueueModel(It.IsAny<Stream>()), Times.Once);
            _serviceBusServiceMock.Verify(a => a.SendMessageAsync(It.IsAny<ApplicationSubmittedMessageQueueModel>()), Times.Once);
        }
    }
}
