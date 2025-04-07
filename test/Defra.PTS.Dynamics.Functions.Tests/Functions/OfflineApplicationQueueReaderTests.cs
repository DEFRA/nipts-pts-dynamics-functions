using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Dynamics.Functions.Functions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
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
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            // Act
            await _queueReader.ProcessOfflineApplication(validMessage);

            // Assert
            _offlineApplicationServiceMock.Verify(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()), Times.Once);
        }

        [Test]
        public void ProcessOfflineApplication_InvalidJson_ThrowsException()
        {
            // Arrange
            var invalidMessage = "invalid_json";

            // Act & Assert
            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () => await _queueReader.ProcessOfflineApplication(invalidMessage));
        }

        [Test]
        public async Task ProcessOfflineApplication_EmptyMessage_LogsWarning()
        {
            // Act
            await _queueReader.ProcessOfflineApplication(string.Empty);

            // Assert
            _loggerMock.Verify(log => log.Log(
                LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void ProcessOfflineApplication_ProcessingError_ThrowsException()
        {
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Processing error"));

            // Act & Assert
            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () => await _queueReader.ProcessOfflineApplication(validMessage));
        }

        [Test]
        public void ProcessOfflineApplication_ValidationError_ThrowsException()
        {
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Validation failure for application"));

            // Act & Assert
            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));

            // Verify logs
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
            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _queueReader.ProcessOfflineApplication(null));

            // Verify logs
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
            // Arrange
            var invalidMessage = "{malformed json}";

            // Act & Assert
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(invalidMessage));
            Assert.That(ex!.Message, Does.Contain("Invalid message format"));
        }

        [Test]
        public async Task ProcessOfflineApplication_SuccessfulProcessing_LogsInformation()
        {
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            // Act
            await _queueReader.ProcessOfflineApplication(validMessage);

            // Assert
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
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));
            Assert.That(ex!.Message, Does.Contain("Unhandled error"));
        }

        [Test]
        public async Task ProcessOfflineApplication_SuccessfulProcessing_LogsSuccess()
        {
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            // Act
            await _queueReader.ProcessOfflineApplication(validMessage);

            // Assert
            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        }

        [Test]
        public void ProcessOfflineApplication_ValidationFailureException_LogsWarning()
        {
            // Arrange
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Validation failed for field"));

            // Act & Assert
            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));

            // Verify logs
            _loggerMock.Verify(log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.AtLeastOnce);
        }

        [Test]
        public void ProcessOfflineApplication_JsonExceptionWithNullMessage_HandlesGracefully()
        {
            // Arrange
            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new JsonException());

            // Act & Assert
            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication("invalid"));

            // Verify logs
            _loggerMock.Verify(log => log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        }

        [Test]
        public void ProcessOfflineApplication_CompleteProcessingWithAllLogs_VerifyLogSequence()
        {
            // Arrange
            var sequence = new MockSequence();
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });
            var logCalls = 0;

            _loggerMock.InSequence(sequence).Setup(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting to process")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                .Callback(() => logCalls++);

            _offlineApplicationServiceMock.InSequence(sequence)
                .Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .Returns(Task.CompletedTask);

            _loggerMock.InSequence(sequence).Setup(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                .Callback(() => logCalls++);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _queueReader.ProcessOfflineApplication(validMessage));
            Assert.That(logCalls, Is.EqualTo(2));
        }

        [Test]
        public async Task ProcessOfflineApplication_WithEmptyObjects_HandlesGracefully()
        {
            // Arrange
            var messageWithEmptyObjects = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB826AD004A" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" },
                Owner = new OwnerInfo(),
                Applicant = new ApplicantInfo()
            });

            // Act
            await _queueReader.ProcessOfflineApplication(messageWithEmptyObjects);

            // Assert
            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Application.ReferenceNumber == "GB826AD004A" &&
                    model.Owner.Email == "ad.blank.email.GB826AD004A@example.com" &&
                    model.Applicant.Email == "ad.blank.email.GB826AD004A@example.com")),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_StandardizesAllEmailsBasedOnPtdNumber()
        {
            // Arrange
            var originalOwnerEmail = "owner@example.com";
            var originalApplicantEmail = "applicant@example.com";
            var ptdReferenceNumber = "GB826AD004A";

            var message = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = ptdReferenceNumber },
                Owner = new OwnerInfo { Email = originalOwnerEmail },
                Applicant = new ApplicantInfo { Email = originalApplicantEmail }
            });

            // Act
            await _queueReader.ProcessOfflineApplication(message);

            // Assert
            var expectedEmail = $"ad.blank.email.{ptdReferenceNumber}@example.com";

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == expectedEmail &&
                    model.Applicant.Email == expectedEmail)),
                Times.Once);

            // Verify logs for both email changes
            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set standardized email for owner with document reference")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set standardized email for applicant with document reference")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_NullPtdDocumentReference_HandlesGracefully()
        {
            // Arrange - Message with null PTD document reference
            var message = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = string.Empty },
                Owner = new OwnerInfo { Email = "owner@example.com" },
                Applicant = new ApplicantInfo { Email = "applicant@example.com" }
            });

            // Act
            await _queueReader.ProcessOfflineApplication(message);

            // Assert - Should use empty string for email
            var expectedEmail = "ad.blank.email.@example.com";

            VerifyProcessedApplication(expectedEmail);
        }

       

        private void VerifyProcessedApplication(string expectedEmail)
        {
            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == expectedEmail &&
                    model.Applicant.Email == expectedEmail)),
                Times.Once);
        }


        [Test]
        public async Task ProcessOfflineApplication_EmptyStringPtdDocumentReference_HandlesGracefully()
        {
            // Arrange - Message with empty string PTD document reference
            var message = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "" },
                Owner = new OwnerInfo { Email = "owner@example.com" },
                Applicant = new ApplicantInfo { Email = "applicant@example.com" }
            });

            // Act
            await _queueReader.ProcessOfflineApplication(message);

            // Assert - Should use empty string for email
            var expectedEmail = "ad.blank.email.@example.com";

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == expectedEmail &&
                    model.Applicant.Email == expectedEmail)),
                Times.Once);
        }
    }
}