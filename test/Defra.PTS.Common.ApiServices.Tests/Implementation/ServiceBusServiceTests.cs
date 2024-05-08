using Azure.Messaging.ServiceBus;
using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.ApiServices.Interface;

namespace Defra.PTS.Common.ApiServices.Tests.Implementation
{
    [TestFixture]
    public class ServiceBusServiceTests
    {
        private Mock<ServiceBusClient> ?mockServiceBusClient;
        private Mock<ILogger<ServiceBusService>> ?mockLogger;
        private Mock<IOptions<AzureServiceBusOptions>> ?mockServiceBusOptions;
        ServiceBusService ?_systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            mockServiceBusClient = new Mock<ServiceBusClient>();
            mockLogger = new Mock<ILogger<ServiceBusService>>();
            mockServiceBusOptions = new Mock<IOptions<AzureServiceBusOptions>>();
            _systemUnderTest = new ServiceBusService(mockServiceBusClient.Object, mockServiceBusOptions.Object, mockLogger.Object);
        }

        [Test]
        public Task SendMessageAsync_WhenSenderIsNullThrowsException()
        {
            // Arrange
            var expectedResult = "Cannot create message batch";           
            var mockSender = new Mock<ServiceBusSender>();

            mockServiceBusClient!.Setup(a => a.CreateSender(It.IsAny<string>())).Returns(mockSender.Object);
            mockServiceBusOptions!.Setup(a => a.Value).Returns(new AzureServiceBusOptions() { SubmitQueueName = "submit", UpdateQueueName = "update" });
            var message = new ApplicationSubmittedMessageQueueModel { ApplicationId = Guid.NewGuid() };

            // Act
            var result = Assert.ThrowsAsync<ServiceBusServiceException>(() => _systemUnderTest!.SendMessageAsync(message));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.AreEqual(expectedResult, result?.Message);

            mockServiceBusClient.Verify(
                x => x.CreateSender(It.IsAny<string>()),
                Times.Once);

            return Task.CompletedTask;
        }

        [Test]
        public void SendEmptyMessageAsync_ReturnsNull()
        {
            // Arrange

            // Act
            var result = _systemUnderTest!.SendMessageAsync(null!);

            // Assert
            Assert.IsTrue(result.IsCompletedSuccessfully);
        }
    }
}
