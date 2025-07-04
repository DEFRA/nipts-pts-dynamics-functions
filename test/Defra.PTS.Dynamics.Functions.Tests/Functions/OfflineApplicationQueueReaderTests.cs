﻿using Defra.PTS.Common.ApiServices.Interface;
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
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

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
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new OfflineApplicationProcessingException("Processing error"));

            Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () => await _queueReader.ProcessOfflineApplication(validMessage));
        }

        [Test]
        public void ProcessOfflineApplication_ValidationError_ThrowsException()
        {
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

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
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

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
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

            _offlineApplicationServiceMock.Setup(service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(async () =>
                await _queueReader.ProcessOfflineApplication(validMessage));
            Assert.That(ex!.Message, Does.Contain("Unhandled error"));
        }

        [Test]
        public async Task ProcessOfflineApplication_SuccessfulProcessing_LogsSuccess()
        {
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

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
            var validMessage = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" }
            });

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

            Assert.DoesNotThrowAsync(async () => await _queueReader.ProcessOfflineApplication(validMessage));
            Assert.That(logCalls, Is.EqualTo(2));
        }

        [Test]
        public async Task ProcessOfflineApplication_WithEmptyObjects_HandlesGracefully()
        {
            var messageWithEmptyObjects = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB826AD004A" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" },
                Owner = new OwnerInfo(),
                Applicant = new ApplicantInfo()
            });

            await _queueReader.ProcessOfflineApplication(messageWithEmptyObjects);

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

            await _queueReader.ProcessOfflineApplication(message);

            var expectedEmail = $"ad.blank.email.{ptdReferenceNumber}@example.com";

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == expectedEmail &&
                    model.Applicant.Email == expectedEmail)),
                Times.Once);

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
            var message = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = string.Empty },
                Owner = new OwnerInfo { Email = "owner@example.com" },
                Applicant = new ApplicantInfo { Email = "applicant@example.com" }
            });

            await _queueReader.ProcessOfflineApplication(message);

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
            var message = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "" },
                Owner = new OwnerInfo { Email = "owner@example.com" },
                Applicant = new ApplicantInfo { Email = "applicant@example.com" }
            });

            await _queueReader.ProcessOfflineApplication(message);

            var expectedEmail = "ad.blank.email.@example.com";

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.Email == expectedEmail &&
                    model.Applicant.Email == expectedEmail)),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_WithEscapedQuotes_HandlesCorrectly()
        {
            var jsonWithEscapedQuotes = @"{
                ""Application"": { ""ReferenceNumber"": ""GB12345678"" },
                ""Ptd"": { ""DocumentReferenceNumber"": ""GB826AD004A"" },
                ""Pet"": { ""Name"": ""My pet \""Buddy\"""" },
                ""Owner"": { ""FullName"": ""John \""Johnny\"" Smith"", ""Email"": ""owner@example.com"" },
                ""Applicant"": { ""FullName"": ""Same Owner"", ""Email"": ""applicant@example.com"" }
            }";

            await _queueReader.ProcessOfflineApplication(jsonWithEscapedQuotes);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Owner.FullName == @"John ""Johnny"" Smith" &&
                    model.Pet.Name == @"My pet ""Buddy""")),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_WithEscapedSlashes_HandlesCorrectly()
        {
            var jsonWithEscapedSlashes = @"{
                ""Application"": { ""ReferenceNumber"": ""GB12345678"" },
                ""Ptd"": { ""DocumentReferenceNumber"": ""GB826AD004A"" },
                ""OwnerAddress"": { 
                    ""AddressLineOne"": ""123 Main St"", 
                    ""AddressLineTwo"": ""Apt 4\\/5"", 
                    ""TownOrCity"": ""London"", 
                    ""County"": ""Greater London"", 
                    ""PostCode"": ""EC1N 2PB"" 
                },
                ""Owner"": { ""FullName"": ""John Smith"", ""Email"": ""owner@example.com"" },
                ""Applicant"": { ""FullName"": ""Same Owner"", ""Email"": ""applicant@example.com"" }
            }";

            await _queueReader.ProcessOfflineApplication(jsonWithEscapedSlashes);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.OwnerAddress.AddressLineTwo == "Apt 4/5")),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_WithMultipleEscapedCharacters_HandlesCorrectly()
        {
            var jsonWithMultipleEscapedChars = @"{
                ""Application"": { ""ReferenceNumber"": ""GB12345678"" },
                ""Ptd"": { ""DocumentReferenceNumber"": ""GB826AD004A"" },
                ""Pet"": { 
                    ""Name"": ""Fluffy"", 
                    ""UniqueFeatureDescription"": ""Has marks like this: \""$%^$%&^%*()""
                },
                ""Owner"": { ""FullName"": ""John Smith"", ""Email"": ""owner@example.com"" },
                ""Applicant"": { ""FullName"": ""Same Owner"", ""Email"": ""applicant@example.com"" }
            }";

            await _queueReader.ProcessOfflineApplication(jsonWithMultipleEscapedChars);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Pet.UniqueFeatureDescription == @"Has marks like this: ""$%^$%&^%*()")),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_WithBackslashEscapes_HandlesCorrectly()
        {
            var jsonWithBackslashes = @"{
                ""Application"": { ""ReferenceNumber"": ""GB12345678"" },
                ""Ptd"": { ""DocumentReferenceNumber"": ""GB826AD004A"" },
                ""Pet"": { 
                    ""Name"": ""Rex"", 
                    ""UniqueFeatureDescription"": ""Path: C:\\\\Pets\\\\Files\\\\Rex""
                },
                ""Owner"": { ""FullName"": ""John Smith"", ""Email"": ""owner@example.com"" },
                ""Applicant"": { ""FullName"": ""Same Owner"", ""Email"": ""applicant@example.com"" }
            }";

            await _queueReader.ProcessOfflineApplication(jsonWithBackslashes);

            _offlineApplicationServiceMock.Verify(service =>
                service.ProcessOfflineApplication(It.Is<OfflineApplicationQueueModel>(model =>
                    model.Pet.UniqueFeatureDescription == @"Path: C:\Pets\Files\Rex")),
                Times.Once);
        }

        [Test]
        public async Task CleanupString_NullOrEmptyInput_ReturnsOriginalValue()
        {
            var message = JsonConvert.SerializeObject(new OfflineApplicationQueueModel
            {
                Application = new ApplicationInfo { ReferenceNumber = "GB12345678" },
                Ptd = new PtdInfo { DocumentReferenceNumber = "GB826AD004A" },
                Owner = new OwnerInfo { Email = "", Telephone = null! },
                ApplicantAddress = null!
            });

            await _queueReader.ProcessOfflineApplication(message);

            _offlineApplicationServiceMock.Verify(
                service => service.ProcessOfflineApplication(It.IsAny<OfflineApplicationQueueModel>()),
                Times.Once);
        }
    }
}