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

        [Test]
        public async Task ProcessOfflineApplication_SuccessfulProcessing_LogsSuccess()
        {
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";

            await _queueReader.ProcessOfflineApplication(validMessage);

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
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";
            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Validation failed for field"));

            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));

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
            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new JsonException());

            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication("invalid"));

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
            var sequence = new MockSequence();
            var validMessage = "{\"Application\": {\"ReferenceNumber\": \"GB12345678\"}}";
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

            Assert.DoesNotThrowAsync(async () => await _queueReader.ProcessOfflineApplication(validMessage));
            Assert.That(logCalls, Is.EqualTo(2));
        }

        [Test]
        public async Task ProcessOfflineApplication_IdcomsMessageWithEmptyEmails_SetsDefaultEmail()
        {
            var idcomsMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB826AD004A" },
                Owner = new OwnerInfo { Email = "" },
                Applicant = new ApplicantInfo { Email = "" }
            });

            await _queueReader.ProcessOfflineApplication(idcomsMessage);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == "ad.dummy.user@example.com" &&
                    model.Applicant.Email == "ad.dummy.user@example.com")),
                Times.Once);

            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set default email for IDCOMS owner")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set default email for IDCOMS applicant")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_IdcomsMessageWithValidEmails_DoesNotChangeEmails()
        {
            var originalOwnerEmail = "owner@example.com";
            var originalApplicantEmail = "applicant@example.com";

            var idcomsMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB826AD004A" },
                Owner = new OwnerInfo { Email = originalOwnerEmail },
                Applicant = new ApplicantInfo { Email = originalApplicantEmail }
            });

            await _queueReader.ProcessOfflineApplication(idcomsMessage);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == originalOwnerEmail &&
                    model.Applicant.Email == originalApplicantEmail)),
                Times.Once);

            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set default email")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
        }

        [Test]
        public async Task ProcessOfflineApplication_NonIdcomsMessageWithEmptyEmails_DoesNotChangeEmails()
        {
            var nonIdcomsMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "INVALID123" },
                Owner = new OwnerInfo { Email = "" },
                Applicant = new ApplicantInfo { Email = "" }
            });

            await _queueReader.ProcessOfflineApplication(nonIdcomsMessage);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == "" &&
                    model.Applicant.Email == "")),
                Times.Once);

            _loggerMock.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set default email")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
        }

        [Test]
        public void IsIdcomsMessage_WithValidIdcomsReferences_ReturnsTrue()
        {
            var refGB826AD = new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB826AD004A" }
            };

            var refGB8Digits = new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" }
            };

            var refLowercase = new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "gb826ad004a" }
            };

            var method = typeof(OfflineApplicationQueueReader).GetMethod("IsIdcomsMessage",
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.NotNull(method, "Method 'IsIdcomsMessage' should not be null");

            if (method != null)
            {
                Assert.That(method.Invoke(null, [refGB826AD]), Is.EqualTo(true));
                Assert.That(method.Invoke(null, [refGB8Digits]), Is.EqualTo(true));
                Assert.That(method.Invoke(null, [refLowercase]), Is.EqualTo(true));
            }


        }

        [Test]
        public void IsIdcomsMessage_WithInvalidReferences_ReturnsFalse()
        {
            var refInvalid1 = new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "ABC12345678" }
            };

            var refInvalid2 = new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB123456" }
            };

            var refInvalid3 = new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "" }
            };

            var method = typeof(OfflineApplicationQueueReader).GetMethod("IsIdcomsMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.That(method!.Invoke(null, [refInvalid1]), Is.EqualTo(false));
            Assert.That(method!.Invoke(null, [refInvalid2]), Is.EqualTo(false));
            Assert.That(method!.Invoke(null, [refInvalid3]), Is.EqualTo(false));

        }
        [Test]
        public async Task ProcessOfflineApplication_WithEmptyObjects_HandlesGracefully()
        {
            var messageWithEmptyObjects = "{\"Application\": {\"ReferenceNumber\": \"GB826AD004A\"}, \"Owner\": {}, \"Applicant\": {}}";

            await _queueReader.ProcessOfflineApplication(messageWithEmptyObjects);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Application.ReferenceNumber == "GB826AD004A" &&
                    model.Owner.Email == "ad.dummy.user@example.com" &&
                    model.Applicant.Email == "ad.dummy.user@example.com")),
                Times.Once);
        }
    }
}
