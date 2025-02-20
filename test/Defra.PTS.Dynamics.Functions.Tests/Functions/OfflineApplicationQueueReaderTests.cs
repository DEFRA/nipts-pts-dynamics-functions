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
    }
}
