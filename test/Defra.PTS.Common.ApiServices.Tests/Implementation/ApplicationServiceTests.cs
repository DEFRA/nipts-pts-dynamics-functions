using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Models = Defra.PTS.Common.Models;
using Entities = Defra.PTS.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Models.CustomException;
using System.Text.Json;
using Defra.PTS.Common.Repositories.Implementation;

namespace Defra.PTS.Common.ApiServices.Tests.Implementation
{
    [TestFixture]
    public class ApplicationServiceTests
    {
        private Mock<IApplicationRepository>? _applicationRepositoryMock;
        private Mock<IOwnerRepository>? _ownerRepositoryMock;
        private Mock<IAddressRepository>? _addressRepositoryMock;
        private Mock<IPetRepository>? _petRepositoryMock;
        private Mock<IBreedRepository>? _breedRepositoryMock;
        private Mock<IColourRepository>? _colourRepositoryMock;
        private Mock<ITravelDocumentRepository>? _travelDocumentRepositoryMock;
        private Mock<IUserRepository>? _userRepositoryMock;

        ApplicationService? sut;

        [SetUp]
        public void SetUp()
        {
            _applicationRepositoryMock = new Mock<IApplicationRepository>();
            _ownerRepositoryMock = new Mock<IOwnerRepository>();
            _addressRepositoryMock = new Mock<IAddressRepository>();
            _petRepositoryMock = new Mock<IPetRepository>();
            _breedRepositoryMock = new Mock<IBreedRepository>();
            _colourRepositoryMock = new Mock<IColourRepository>();
            _travelDocumentRepositoryMock = new Mock<ITravelDocumentRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            sut = new ApplicationService(
                  _applicationRepositoryMock.Object
                , _ownerRepositoryMock.Object
                , _addressRepositoryMock.Object
                , _petRepositoryMock.Object
                , _breedRepositoryMock.Object
                , _colourRepositoryMock.Object
                , _travelDocumentRepositoryMock.Object
                , _userRepositoryMock.Object);
        }

        [Test]
        public void GetApplication_WhenApplicationisNull_ReturnsBlankApplicationObject()
        {
            Guid guid = Guid.Empty;
            Entities.Application? application = null;

            _applicationRepositoryMock?.Setup(a => a.Find(It.IsAny<Guid>()))!.ReturnsAsync(application);

            var result = sut?.GetApplication(guid);
            Assert.AreEqual(typeof(Models.Application), result?.Result.GetType());
        }

        [Test]
        public async Task GetApplication_WhenApplicationIsNotNull_ReturnsApplicationObject()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var addressId = Guid.NewGuid();
            var petId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var petDob = DateTime.Now.AddYears(-2);
            var microChippedadte = DateTime.Now.AddMonths(-6);
            var contactId = Guid.NewGuid();

            _applicationRepositoryMock?.Setup(repo => repo.Find(applicationId))
                .ReturnsAsync(new Entities.Application
                {
                    Id = applicationId,
                    OwnerId = ownerId,
                    OwnerNewName =  "John Doe",
                    OwnerNewTelephone = "123-456-7890",
                    OwnerAddressId = addressId,
                    PetId = petId,
                    ReferenceNumber = "App123",
                    DateOfApplication = DateTime.Now,
                    UserId = userId
                    // Add other properties as needed
                });

            _ownerRepositoryMock?.Setup(repo => repo.Find(ownerId))
                .ReturnsAsync(new Entities.Owner
                {
                    Id = ownerId,
                    AddressId = addressId,
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    Telephone = "123-456-7890",
                    // Add other properties as needed
                });

            _addressRepositoryMock?.Setup(repo => repo.GetAddress(addressId, AddressType.Owner))
                .ReturnsAsync(new Entities.Address
                {
                    AddressLineOne = "123 Main St",
                    AddressLineTwo = "Apt 456",
                    TownOrCity = "City",
                    PostCode = "12345",
                    County = "County",
                    CountryName = "Country"
                    // Add other properties as needed
                });

            _petRepositoryMock?.Setup(repo => repo.Find(petId))
                .ReturnsAsync(new Entities.Pet
                {
                    Id = petId,
                    Name = "Fluffy",
                    SpeciesId = 1, // Replace with actual species ID
                    SexId = 1, // Replace with actual sex ID
                    DOB = petDob,
                    UniqueFeatureDescription = "Unique features",
                    MicrochipNumber = "123456789",
                    MicrochippedDate = microChippedadte,
                    BreedId = 1,
                    ColourId = 1,
                    OtherColour = "Other color",
                    AdditionalInfoMixedBreedOrUnknown = "Unique Breed"
                    // Add other properties as needed
                });

            _breedRepositoryMock?.Setup(repo => repo.Find(It.IsAny<int?>()!))
                .ReturnsAsync(new Entities.Breed
                {
                    Id = 1,
                    Name = "Labrador Retriever" // Replace with actual breed name
                                                // Add other properties as needed
                });

            _colourRepositoryMock?.Setup(repo => repo.Find(It.IsAny<int?>()!))
                .ReturnsAsync(new Entities.Colour
                {
                    Id = 1,
                    Name = "Brown" // Replace with actual color name
                                   // Add other properties as needed
                });

            _travelDocumentRepositoryMock?.Setup(repo => repo.GetTravelDocument(applicationId, ownerId, petId))
                .ReturnsAsync(new Entities.TravelDocument
                {
                    DocumentReferenceNumber = "Doc123"
                    // Add other properties as needed
                });

            _userRepositoryMock?.Setup(repo => repo.Find(It.IsAny<Guid>()))
                .ReturnsAsync(new Entities.User
                {
                    Id = userId,
                    ContactId = contactId,
                });

            // Act
            var result = await sut!.GetApplication(applicationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("contacts(" + contactId.ToString() + ")", result.NiptsApplicantId);
            Assert.AreEqual(applicationId.ToString(), result.NiptsPortalApplicationId);
            Assert.AreEqual(petDob.ToString("yyyy-MM-dd"), result.NiptsPetDob);
            Assert.AreEqual(microChippedadte.ToString("yyyy-MM-dd"), result.NiptsMicrochippedDate);
            // Add more assertions for other properties in payloadObject

            // You can also assert specific interactions with mock repositories if needed
            _applicationRepositoryMock?.Verify(repo => repo.Find(applicationId), Times.Once);
            _ownerRepositoryMock?.Verify(repo => repo.Find(ownerId), Times.Once);
            _addressRepositoryMock?.Verify(repo => repo.GetAddress(addressId, AddressType.Owner), Times.Once);
            _petRepositoryMock?.Verify(repo => repo.Find(petId), Times.Once);
            _breedRepositoryMock?.Verify(repo => repo.Find(It.IsAny<int?>()!), Times.Once);
            _colourRepositoryMock?.Verify(repo => repo.Find(It.IsAny<int?>()!), Times.Once);
            _travelDocumentRepositoryMock?.Verify(repo => repo.GetTravelDocument(applicationId, ownerId, petId), Times.Once);
        }

        [Test]
        public async Task UpdateApplicationStatus_ReturnsNullApplicationId()
        {
            var dynamicId = Guid.NewGuid();
            // Arrange
            var applicationUpdateQueueModel = new Models.ApplicationUpdateQueueModel
            {
                Id = Guid.NewGuid(),
                StatusId = "Authorised", // Replace with actual status ID
                DynamicId = dynamicId,
                DateAuthorised = DateTime.Now,
                DateRejected = null,
                DateRevoked = null
                // Add other properties as needed
            };

            Entities.Application? application = null;
            _applicationRepositoryMock?.Setup(repo => repo.Find(applicationUpdateQueueModel.Id))!
                .ReturnsAsync(application);

            // Act
            var result = await sut!.UpdateApplicationStatus(applicationUpdateQueueModel);

            // Assert
            Assert.IsNull(result);

            // Verify that the Update and SaveChanges methods were called on the mock repository
            _applicationRepositoryMock?.Verify(repo => repo.Find(applicationUpdateQueueModel.Id), Times.Once);
            _applicationRepositoryMock?.Verify(repo => repo.Update(It.IsAny<Entities.Application>()), Times.Never);
            _applicationRepositoryMock?.Verify(repo => repo.SaveChanges(), Times.Never);
        }

        [Test]
        public async Task UpdateApplicationStatus_ReturnsUpdatedApplicationId()
        {
            var dynamicId = Guid.NewGuid();
            // Arrange
            var applicationUpdateQueueModel = new Models.ApplicationUpdateQueueModel
            {
                Id = Guid.NewGuid(),
                StatusId = "Authorised", // Replace with actual status ID
                DynamicId = dynamicId,
                DateAuthorised = DateTime.Now,
                DateRejected = null,
                DateRevoked = null
                // Add other properties as needed
            };

            _applicationRepositoryMock?.Setup(repo => repo.Find(applicationUpdateQueueModel.Id))
                .ReturnsAsync(new Entities.Application
                {
                    Id = (Guid)applicationUpdateQueueModel.Id,
                    // Set other properties as needed
                });

            // Act
            var result = await sut!.UpdateApplicationStatus(applicationUpdateQueueModel);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(applicationUpdateQueueModel.Id, result);

            // Verify that the Update and SaveChanges methods were called on the mock repository
            _applicationRepositoryMock?.Verify(repo => repo.Find(applicationUpdateQueueModel.Id), Times.Once);
            _applicationRepositoryMock?.Verify(repo => repo.Update(It.IsAny<Entities.Application>()), Times.Once);
            _applicationRepositoryMock?.Verify(repo => repo.SaveChanges(), Times.Once);
        }

        [Test]
        public async Task PerformHealthCheckLogic_ReturnsTrue()
        {
            // Arrange
            _applicationRepositoryMock?.Setup(repo => repo.PerformHealthCheckLogic())
                .ReturnsAsync(true);

            // Act
            var result = await sut!.PerformHealthCheckLogic();

            // Assert
            Assert.IsTrue(result);

            // Verify that the PerformHealthCheckLogic method was called on the mock repository
            _applicationRepositoryMock?.Verify(repo => repo.PerformHealthCheckLogic(), Times.Once);
        }

        [Test]
        public async Task PerformHealthCheckLogic_ReturnsFalse()
        {
            // Arrange
            _applicationRepositoryMock?.Setup(repo => repo.PerformHealthCheckLogic())
                .ReturnsAsync(false);


            // Act
            var result = await sut!.PerformHealthCheckLogic();

            // Assert
            Assert.IsFalse(result);

            // Verify that the PerformHealthCheckLogic method was called on the mock repository
            _applicationRepositoryMock?.Verify(repo => repo.PerformHealthCheckLogic(), Times.Once);
        }

        [Test]
        public async Task GetApplicationQueueModel_ValidStream_ReturnsModel()
        {
            // Arrange
            var applicationData = new Models.ApplicationSubmittedMessageQueueModel
            {

            };
            string applicationJson = JsonSerializer.Serialize(applicationData);
            byte[] applicationBytes = Encoding.UTF8.GetBytes(applicationJson);
            using (var applicationStream = new MemoryStream(applicationBytes))
            {
                // Act
                var result = await sut!.GetApplicationQueueModel(applicationStream);

                // Assert
                Assert.NotNull(result);
                // Add more assertions as needed
            }
        }

        [Test]
        public void GetApplicationQueueModel_InvalidStream_ReturnsCustomException()
        {
            var json = "{" +
                "junk" +
                "}";

            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            Assert.ThrowsAsync<UserFunctionException>(() => sut!.GetApplicationQueueModel(memoryStream));
        }
    }
}
