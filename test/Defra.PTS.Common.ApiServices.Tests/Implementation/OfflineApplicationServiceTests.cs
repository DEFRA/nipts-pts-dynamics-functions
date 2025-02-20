using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Defra.PTS.Common.Tests.ApiServices
{
    [TestFixture]
    public class OfflineApplicationServiceTests
    {
        private Mock<IApplicationRepository> _applicationRepositoryMock = null!;
        private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
        private Mock<IAddressRepository> _addressRepositoryMock = null!;
        private Mock<IPetRepository> _petRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<ITravelDocumentRepository> _travelDocumentRepositoryMock = null!;
        private Mock<IBreedRepository> _breedRepositoryMock = null!;
        private Mock<IColourRepository> _colourRepositoryMock = null!;
        private Mock<ILogger<IdcomsMappingValidator>> _mappingLoggerMock = null!;
        private Mock<ILogger<OfflineApplicationService>> _loggerMock = null!;
        private IdcomsMappingValidator _mappingValidator = null!;
        private OfflineApplicationService _service = null!;


        [SetUp]
        public void Setup()
        {
            _applicationRepositoryMock = new Mock<IApplicationRepository>();
            _ownerRepositoryMock = new Mock<IOwnerRepository>();
            _addressRepositoryMock = new Mock<IAddressRepository>();
            _petRepositoryMock = new Mock<IPetRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _travelDocumentRepositoryMock = new Mock<ITravelDocumentRepository>();
            _breedRepositoryMock = new Mock<IBreedRepository>();
            _colourRepositoryMock = new Mock<IColourRepository>();
            _mappingLoggerMock = new Mock<ILogger<IdcomsMappingValidator>>();
            _loggerMock = new Mock<ILogger<OfflineApplicationService>>();

            _mappingValidator = new IdcomsMappingValidator(
                _mappingLoggerMock.Object,
                _breedRepositoryMock.Object,
                _colourRepositoryMock.Object);

            var options = new OfflineApplicationServiceOptions
            {
                ApplicationRepository = _applicationRepositoryMock.Object,
                OwnerRepository = _ownerRepositoryMock.Object,
                AddressRepository = _addressRepositoryMock.Object,
                PetRepository = _petRepositoryMock.Object,
                UserRepository = _userRepositoryMock.Object,
                TravelDocumentRepository = _travelDocumentRepositoryMock.Object,
                MappingValidator = _mappingValidator,
                BreedRepository = _breedRepositoryMock.Object,
                Logger = _loggerMock.Object
            };

            _service = new OfflineApplicationService(options);
            SetupRepositoryMocks();
        }

        [Test]
        public async Task ProcessOfflineApplication_ValidModel_SuccessfullyProcesses()
        {
            
            var model = CreateValidQueueModel();
            _breedRepositoryMock.Setup(x => x.FindById(1))
                .ReturnsAsync(new Entities.Breed { Id = 1, Name = "Test Breed", SpeciesId = 1 });
            _colourRepositoryMock.Setup(x => x.FindById(1))
                .ReturnsAsync(new Entities.Colour { Id = 1, Name = "Test Colour", SpeciesId = 1 });

            
            await _service.ProcessOfflineApplication(model);

            
            VerifyRepositoryCalls();
        }

        [Test]
        public void ProcessOfflineApplication_ValidationFails_ThrowsException()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.BreedId = 0; 

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.StartWith("Validation failed"));
        }

        [Test]
        public void ProcessOfflineApplication_RepositoryError_ThrowsException()
        {
            
            var model = CreateValidQueueModel();
            SetupForValidValidation();
            _applicationRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Application>()))
                .ThrowsAsync(new Exception("Database error"));

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.Contain("Failed to process application"));
        }

        [Test]
        public void ProcessOfflineApplication_EmptyReferenceNumber_ThrowsException()
        {
            
            var model = CreateValidQueueModel();
            model.Application.ReferenceNumber = "";
            SetupForValidValidation();

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.Contain("Reference number is required"));
        }

        [Test]
        public async Task ProcessOfflineApplication_UnknownBreed_SetsAdditionalInfo()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.BreedId = 100; 
            model.Pet.AdditionalInfoMixedBreedOrUnknown = "Unknown small breed";
            SetupForValidValidation();

            
            await _service.ProcessOfflineApplication(model);

            
            _petRepositoryMock.Verify(x => x.Add(It.Is<Entities.Pet>(p =>
                p.BreedId == 100 &&
                p.AdditionalInfoMixedBreedOrUnknown == "Unknown small breed")),
                Times.Once);
        }

       
        [Test]
        public async Task ProcessOfflineApplication_PetWithUniqueFeature_SetsFeatureDescription()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.UniqueFeatureDescription = "White spot on chest";
            SetupForValidValidation();

            
            await _service.ProcessOfflineApplication(model);

            
            _petRepositoryMock.Verify(x => x.Add(It.Is<Entities.Pet>(p =>
                p.HasUniqueFeature == 1 &&
                p.UniqueFeatureDescription == "White spot on chest")),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_AddressWithNullValue_HandlesNullCorrectly()
        {
            
            var model = CreateValidQueueModel();
            model.OwnerAddress.AddressLineTwo = "NULL";
            SetupForValidValidation();

            
            await _service.ProcessOfflineApplication(model);

            
            _addressRepositoryMock.Verify(x => x.Add(It.Is<Entities.Address>(a =>
                a.AddressLineTwo == null)),
                Times.Once);
        }

       

        [Test]
        public async Task ProcessOfflineApplication_ValidDynamicId_SetsDynamicId()
        {
            
            var model = CreateValidQueueModel();
            var dynamicId = Guid.NewGuid();
            model.Application.DynamicId = dynamicId.ToString();
            SetupForValidValidation();

            
            await _service.ProcessOfflineApplication(model);

            
            _applicationRepositoryMock.Verify(x => x.Add(It.Is<Entities.Application>(a =>
                a.DynamicId == dynamicId)),
                Times.Once);
        }

      

        [Test]
        public void ProcessOfflineApplication_SaveChangesError_ThrowsException()
        {
            
            var model = CreateValidQueueModel();
            SetupForValidValidation();
            _addressRepositoryMock.Setup(x => x.SaveChanges())
                .ThrowsAsync(new Exception("Database error"));

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.Contain("Failed to process application"));
        }

       

        [Test]
        public async Task ProcessOfflineApplication_ContactIdWithSpaces_ParsesCorrectly()
        {
            
            var model = CreateValidQueueModel();
            var contactId = Guid.NewGuid();
            model.Applicant.ContactId = $" {contactId} "; // Add spaces
            SetupForValidValidation();

            
            await _service.ProcessOfflineApplication(model);

            
            _userRepositoryMock.Verify(x => x.Add(It.Is<Entities.User>(u =>
                u.ContactId == contactId)),
                Times.Once);
        }

        private void SetupRepositoryMocks()
        {
            _userRepositoryMock.Setup(x => x.GetUser(It.IsAny<string>()))
                .ReturnsAsync((Entities.User?)null);

            // Setup SaveChanges for all repositories
            _addressRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _ownerRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _petRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _applicationRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _travelDocumentRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _userRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            // Setup Add methods
            _addressRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Address>())).Returns(Task.CompletedTask);
            _ownerRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Owner>())).Returns(Task.CompletedTask);
            _petRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Pet>())).Returns(Task.CompletedTask);
            _applicationRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Application>())).Returns(Task.CompletedTask);
            _travelDocumentRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.TravelDocument>())).Returns(Task.CompletedTask);
        }


        [Test]
        public void ProcessOfflineApplication_InvalidDynamicId_ThrowsValidationException()
        {
            
            var model = CreateValidQueueModel();
            model.Application.DynamicId = "invalid-guid";
            SetupForValidValidation();

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.Contain("Invalid Dynamic ID format"));
        }

        [Test]
        public void ProcessOfflineApplication_LongStrings_ThrowsValidationException()
        {
            
            var model = CreateValidQueueModel();
            model.Owner.FullName = new string('A', 301); 
            model.Pet.Name = new string('B', 301); 
            model.OwnerAddress.AddressLineOne = new string('C', 251); 
            SetupForValidValidation();

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.Contain("cannot exceed"));
        }

        [Test]
        public void ProcessOfflineApplication_NullOwnerAddress_ThrowsValidationException()
        {
            
            var model = CreateValidQueueModel();
            model.OwnerAddress = null!;
            SetupForValidValidation();

            
            var ex = Assert.ThrowsAsync<OfflineApplicationProcessingException>(
                async () => await _service.ProcessOfflineApplication(model)
            );
            Assert.That(ex!.Message, Does.Contain("Owner address information is required"));
        }


        [Test]
        public async Task ProcessOfflineApplication_OtherColour_SetsOtherColourDescription()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.ColourId = 11;
            model.Pet.OtherColour = "Tri-colored";

            
            _colourRepositoryMock.Setup(x => x.FindById(11))
                .ReturnsAsync(new Entities.Colour { Id = 11, Name = "Other", SpeciesId = 1 });

            
            _breedRepositoryMock.Setup(x => x.FindById(1))
                .ReturnsAsync(new Entities.Breed { Id = 1, Name = "Test Breed", SpeciesId = 1 });

            
            await _service.ProcessOfflineApplication(model);

            
            _petRepositoryMock.Verify(x => x.Add(It.Is<Entities.Pet>(p =>
                p.ColourId == 11 &&
                p.OtherColour == "Tri-colored")),
                Times.Once);
        }

        [Test]
        public async Task ProcessOfflineApplication_StringTruncation_ProcessesSuccessfully()
        {
            
            var model = CreateValidQueueModel();
           
            model.Owner.FullName = new string('A', 250); 
            model.Pet.Name = new string('B', 250); 
            model.OwnerAddress.AddressLineOne = new string('C', 200); 

            SetupForValidValidation();

            
            await _service.ProcessOfflineApplication(model);

            
            _ownerRepositoryMock.Verify(x => x.Add(It.Is<Entities.Owner>(o =>
                o.FullName.Length == 250)),
                Times.Once);
            _petRepositoryMock.Verify(x => x.Add(It.Is<Entities.Pet>(p =>
                p.Name != null && p.Name.Length == 250)),
                Times.Once);
            _addressRepositoryMock.Verify(x => x.Add(It.Is<Entities.Address>(a =>
                a.AddressLineOne != null && a.AddressLineOne.Length == 200)),
                Times.Once);
        }

        private void SetupForValidValidation()
        {
            _breedRepositoryMock.Setup(x => x.FindById(1))
                .ReturnsAsync(new Entities.Breed { Id = 1, Name = "Test Breed", SpeciesId = 1 });

            _colourRepositoryMock.Setup(x => x.FindById(1))
                .ReturnsAsync(new Entities.Colour { Id = 1, Name = "Test Colour", SpeciesId = 1 });

            // Setup all other necessary repository calls
            _userRepositoryMock.Setup(x => x.GetUser(It.IsAny<string>()))
                .ReturnsAsync((Entities.User?)null);

            // Setup SaveChanges for all repositories
            _addressRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _ownerRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _petRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _applicationRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _travelDocumentRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _userRepositoryMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            // Setup Add methods
            _addressRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Address>())).Returns(Task.CompletedTask);
            _ownerRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Owner>())).Returns(Task.CompletedTask);
            _petRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Pet>())).Returns(Task.CompletedTask);
            _applicationRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.Application>())).Returns(Task.CompletedTask);
            _travelDocumentRepositoryMock.Setup(x => x.Add(It.IsAny<Entities.TravelDocument>())).Returns(Task.CompletedTask);
        }


        private void VerifyRepositoryCalls()
        {
            _addressRepositoryMock.Verify(x => x.Add(It.IsAny<Entities.Address>()), Times.AtLeastOnce);
            _ownerRepositoryMock.Verify(x => x.Add(It.IsAny<Entities.Owner>()), Times.Once);
            _petRepositoryMock.Verify(x => x.Add(It.IsAny<Entities.Pet>()), Times.Once);
            _applicationRepositoryMock.Verify(x => x.Add(It.IsAny<Entities.Application>()), Times.Once);
            _travelDocumentRepositoryMock.Verify(x => x.Add(It.IsAny<Entities.TravelDocument>()), Times.Once);
        }

        private static OfflineApplicationQueueModel CreateValidQueueModel()
        {
            var referenceNumber = "GB12345678";
            return new OfflineApplicationQueueModel
            {
                Owner = new OwnerInfo
                {
                    FullName = "Test Owner",
                    Email = "test@example.com",
                    Telephone = "07123456789"
                },
                Applicant = new ApplicantInfo
                {
                    FullName = "Test Applicant",
                    Email = "applicant@example.com",
                    ContactId = Guid.NewGuid().ToString()
                },
                Pet = new PetInfo
                {
                    Name = "Test Pet",
                    SpeciesId = 1,
                    BreedId = 1,
                    ColourId = 1,
                    SexId = 1,
                    MicrochipNumber = "123456789012345",
                    DOB = DateTime.UtcNow.AddYears(-2)
                },
                Application = new ApplicationInfo
                {
                    ReferenceNumber = referenceNumber,
                    Status = "Authorised",
                    DateOfApplication = DateTime.UtcNow,
                    DateAuthorised = DateTime.UtcNow
                },
                OwnerAddress = new AddressInfo
                {
                    AddressLineOne = "123 Test St",
                    TownOrCity = "Test City",
                    PostCode = "SW1A 1AA"
                },
                ApplicantAddress = new AddressInfo
                {
                    AddressLineOne = "456 Test St",
                    TownOrCity = "Test City",
                    PostCode = "SW1A 1AA"
                },
                Ptd = new PtdInfo
                {
                    DocumentReferenceNumber = referenceNumber  
                }
            };
        }
    }
}