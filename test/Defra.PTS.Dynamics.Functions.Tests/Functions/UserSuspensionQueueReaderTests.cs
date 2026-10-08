using System;
using System.Threading.Tasks;
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
    public class UserSuspensionQueueReaderTests
    {
        private Mock<IUserService> _userServiceMock = null!;
        private Mock<ILogger<UserSuspensionQueueReader>> _loggerMock = null!;
        private UserSuspensionQueueReader _queueReader = null!;

        [SetUp]
        public void Setup()
        {
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<UserSuspensionQueueReader>>();
            _queueReader = new UserSuspensionQueueReader(_userServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task ProcessUserSuspension_WhenUserSuspended_UpdatesStatus()
        {
            Guid contactId = Guid.NewGuid();
            var message = JsonConvert.SerializeObject(new UserSuspendQueueModel
            {
                ContactId = contactId,
                IsUserSuspended = true
            });

            await _queueReader.ProcessUserSuspension(message);

            _userServiceMock.Verify(s => s.UpdateUserSuspensionStatus(contactId, true), Times.Once);
        }

        [Test]
        public async Task ProcessUserSuspension_WhenUserUnsuspended_UpdatesStatus()
        {
            Guid contactId = Guid.NewGuid();
            var message = JsonConvert.SerializeObject(new UserSuspendQueueModel
            {
                ContactId = contactId,
                IsUserSuspended = false
            });

            await _queueReader.ProcessUserSuspension(message);

            _userServiceMock.Verify(s => s.UpdateUserSuspensionStatus(contactId, false), Times.Once);
        }

        [Test]
        public void ProcessUserSuspension_WhenMessageEmpty_ThrowsException()
        {
            Assert.ThrowsAsync<UserFunctionException>(async () => await _queueReader.ProcessUserSuspension(string.Empty));
        }

        [Test]
        public void ProcessUserSuspension_WhenContactIdEmpty_ThrowsException()
        {
            var message = JsonConvert.SerializeObject(new UserSuspendQueueModel
            {
                ContactId = Guid.Empty,
                IsUserSuspended = true
            });

            Assert.ThrowsAsync<UserFunctionException>(async () => await _queueReader.ProcessUserSuspension(message));
        }

        [Test]
        public void ProcessUserSuspension_WhenInvalidJson_ThrowsException()
        {
            Assert.ThrowsAsync<UserFunctionException>(async () => await _queueReader.ProcessUserSuspension("invalid_json"));
        }

        [Test]
        public void ProcessUserSuspension_WhenServiceThrows_PropagatesException()
        {
            Guid contactId = Guid.NewGuid();
            var message = JsonConvert.SerializeObject(new UserSuspendQueueModel
            {
                ContactId = contactId,
                IsUserSuspended = true
            });

            _userServiceMock.Setup(s => s.UpdateUserSuspensionStatus(contactId, true))
                .ThrowsAsync(new UserFunctionException($"No user found for ContactId {contactId}"));

            Assert.ThrowsAsync<UserFunctionException>(async () => await _queueReader.ProcessUserSuspension(message));
        }
    }
}
