using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Dynamics.Functions.Functions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Defra.PTS.Dynamics.Functions.Tests.Functions
{
    [TestFixture]
    public class OfflineApplicationQueueReaderTests
    {
        private Mock<IOfflineApplicationService> _offlineApplicationServiceMock = null!;
        private Mock<ILogger<OfflineApplicationQueueReader>> _loggerMock = null!;
        private OfflineApplicationQueueReader _queueReader = null!;


        [SetUp]
        public void Setup()
        {
            _offlineApplicationServiceMock = new Mock<IOfflineApplicationService>();
            _loggerMock = new Mock<ILogger<OfflineApplicationQueueReader>>();
            _queueReader = new OfflineApplicationQueueReader(_offlineApplicationServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task ProcessOfflineApplication_ValidMessage_ProcessesSuccessfully()
        {
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";

            await _queueReader.ProcessOfflineApplication(validMessage);

            _offlineApplicationServiceMock.Verify(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()), Times.Once);
        }

        [Test]
        public void ProcessOfflineApplication_InvalidJson_ThrowsException()
        {
            var invalidMessage = "invalid_json";

            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () => await _queueReader.ProcessOfflineApplication(invalidMessage));
        }

        [Test]
        public async Task ProcessOfflineApplication_EmptyMessage_LogsWarning()
        {
            await _queueReader.ProcessOfflineApplication(string.Empty);
            _loggerMock.Verify(log => log.Log(
                LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
        }


        [Test]
        public void ProcessOfflineApplication_ProcessingError_ThrowsException()
        {
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";
            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Processing error"));

            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () => await _queueReader.ProcessOfflineApplication(validMessage));
        }

        [Test]
        public void ProcessOfflineApplication_ValidationError_ThrowsException()
        {
            
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";
            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Validation failure for application"));

            
            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));

            
            _loggerMock.Verify(logger => logger.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting to process")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

            
            _loggerMock.Verify(logger => logger.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing error for application")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

            
            _loggerMock.Verify(logger => logger.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled error processing offline application")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        }

        [Test]
        public void ProcessOfflineApplication_NullMessage_LogsWarning()
        {
           
            Assert.DoesNotThrowAsync(async () => await _queueReader.ProcessOfflineApplication(null));

            
            _loggerMock.Verify(logger => logger.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        }





        [Test]
        public void ProcessOfflineApplication_InvalidJsonStructure_ThrowsException()
        {
            var invalidMessage = "{malformed json}";

            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(invalidMessage));
            Assert.That(ex!.Message, Does.Contain("Invalid message format"));
        }

        [Test]
        public async Task ProcessOfflineApplication_SuccessfulProcessing_LogsInformation()
        {
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";

            await _queueReader.ProcessOfflineApplication(validMessage);

            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Starting to process")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        }


        [Test]
        public void ProcessOfflineApplication_UnexpectedError_ThrowsWithCorrectMessage()
        {
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";
            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));
            Assert.That(ex!.Message, Does.Contain("Unhandled error"));
        }
    }
}
