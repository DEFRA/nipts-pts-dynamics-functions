using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Repositories;
using Defra.PTS.Common.Repositories.Implementation;
using Defra.PTS.Dynamics.Functions.Functions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using Entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Dynamics.Functions.Tests.Functions
{
    // Disabled on normal runs. Run on demand with:
    //   dotnet test --filter "Category=Integration"
    // Before running, manually place a suspension message on the queue, e.g.:
    //   { "contactId": "<guid>", "status": "Suspended" }   (or "Active")
    // Requires these environment variables (connection string must NOT include EntityPath):
    //   PTS_INTEGRATION_SQL_CONNECTION        - SQL connection string to the pet-travel database
    //   PTS_PETOWNER_SERVICEBUS_CONNECTION    - Service Bus connection string for the petowner namespace (Listen rights are enough)
    //   PTS_PETOWNER_SUSPENSION_QUEUE         - optional, defaults to ptd.petowner.suspension
    [TestFixture]
    [Explicit("Integration test: reads a manually-placed Service Bus message and writes to a real database. Run manually.")]
    [Category("Integration")]
    public class UserSuspensionIntegrationTests
    {
        private const string SqlConnEnvVar = "PTS_INTEGRATION_SQL_CONNECTION";
        private const string ServiceBusConnEnvVar = "PTS_PETOWNER_SERVICEBUS_CONNECTION";
        private const string QueueNameEnvVar = "PTS_PETOWNER_SUSPENSION_QUEUE";
        private const string DefaultQueueName = "defra.trade.pts.petowner.suspension";

        private string _sqlConnection = null!;
        private string _serviceBusConnection = null!;
        private string _queueName = null!;
        private Guid _contactId;
        private Guid _userId;

        [SetUp]
        public void Setup()
        {
            _sqlConnection = Environment.GetEnvironmentVariable(SqlConnEnvVar)!;
            _serviceBusConnection = Environment.GetEnvironmentVariable(ServiceBusConnEnvVar)!;
            _queueName = Environment.GetEnvironmentVariable(QueueNameEnvVar) ?? DefaultQueueName;

            if (string.IsNullOrWhiteSpace(_sqlConnection) || string.IsNullOrWhiteSpace(_serviceBusConnection))
            {
                Assert.Ignore($"Set {SqlConnEnvVar} and {ServiceBusConnEnvVar} (and optionally {QueueNameEnvVar}) to run this integration test.");
            }

            _contactId = Guid.Empty;
            _userId = Guid.Empty;
        }

        [Test]
        public async Task EndToEnd_SuspensionMessage_SuspendsUserInDatabase()
        {
            // Arrange: read the suspension message that was placed on the queue manually
            var received = await ReceiveMessageAsync();
            Assert.That(received, Is.Not.Null,
                $"No message found on '{_queueName}'. Place one on the queue before running, e.g. {{ \"contactId\": \"<guid>\", \"status\": \"Suspended\" }}.");

            var messageBody = received!.Body.ToString();
            var suspension = JsonConvert.DeserializeObject<UserSuspendQueueModel>(messageBody);
            Assert.That(suspension, Is.Not.Null, "Message on the queue was not a valid suspension message.");
            Assert.That(suspension!.ContactId, Is.Not.EqualTo(Guid.Empty), "Message is missing a contactId.");

            bool expectedSuspended = string.Equals(suspension.Status, "Suspended", StringComparison.OrdinalIgnoreCase);
            _contactId = suspension.ContactId;

            // Add a matching user to the db in the opposite state so processing actually changes it
            _userId = await AddUserAsync(_contactId, isUserSuspended: !expectedSuspended);

            // Act: run the message through the function
            await using (var context = CreateContext())
            {
                var reader = new UserSuspensionQueueReader(
                    BuildUserService(context),
                    NullLogger<UserSuspensionQueueReader>.Instance);

                await reader.ProcessUserSuspension(messageBody);
            }

            // Assert: the user's suspension state now matches the message
            Assert.That(await IsUserSuspendedAsync(_contactId), Is.EqualTo(expectedSuspended),
                $"Expected IsUserSuspended to be {expectedSuspended} for status '{suspension.Status}'.");
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_userId != Guid.Empty)
            {
                await RemoveUserAsync(_userId);
            }
        }

        private CommonDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommonDbContext>()
                .UseSqlServer(_sqlConnection)
                .Options;
            return new CommonDbContext(options);
        }

        private static UserService BuildUserService(CommonDbContext context)
        {
            var userRepository = new UserRepository(context);
            var addressRepository = new Repository<Entity.Address>(context);
            return new UserService(userRepository, addressRepository);
        }

        private async Task<Guid> AddUserAsync(Guid contactId, bool isUserSuspended)
        {
            await using var context = CreateContext();
            var user = new Entity.User
            {
                Id = Guid.NewGuid(),
                Email = $"integration.suspension.{contactId:N}@example.com",
                FullName = "Integration Suspension Test",
                FirstName = "Integration",
                LastName = "Test",
                Role = "Owner",
                ContactId = contactId,
                IsUserSuspended = isUserSuspended,
                CreatedOn = DateTime.UtcNow
            };

            context.User!.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        }

        private async Task<bool> IsUserSuspendedAsync(Guid contactId)
        {
            await using var context = CreateContext();
            var user = await context.User!
                .Where(u => u.ContactId == contactId)
                .OrderByDescending(u => u.CreatedOn)
                .FirstOrDefaultAsync();
            return user?.IsUserSuspended ?? false;
        }

        private async Task RemoveUserAsync(Guid userId)
        {
            await using var context = CreateContext();
            var user = await context.User!.FindAsync(userId);
            if (user != null)
            {
                context.User!.Remove(user);
                await context.SaveChangesAsync();
            }
        }

        private async Task<ServiceBusReceivedMessage?> ReceiveMessageAsync()
        {
            await using var client = new ServiceBusClient(_serviceBusConnection);
            var receiver = client.CreateReceiver(_queueName);
            var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
            if (message != null)
            {
                await receiver.CompleteMessageAsync(message);
            }
            return message;
        }
    }
}
