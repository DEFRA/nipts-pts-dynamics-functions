using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.Models;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Defra.PTS.Common.ApiServices.Tests.Implementation
{
    [TestFixture]
    public class IdcomsMappingValidatorTests
    {
        private const string VALID_EMAIL = "test@example.com";
        private const string VALID_PHONE = "07123456789";
        private const string VALID_POSTCODE = "SW1A 1AA";
        private const string VALID_REFERENCE = "GB12345678";
        private const string VALID_MICROCHIP = "123456789012345";
        private const string INVALID_EMAIL = "invalid-email";
        private const string INVALID_PHONE = "123";
        private const string INVALID_POSTCODE = "INVALID";
        private const string INVALID_REFERENCE = "12345678";
        private const string INVALID_MICROCHIP = "123";

        private Mock<ILogger<IdcomsMappingValidator>> _loggerMock = null!;
        private Mock<IBreedRepository> _breedRepositoryMock = null!;
        private Mock<IColourRepository> _colourRepositoryMock = null!;
        private IdcomsMappingValidator _validator = null!;


        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<IdcomsMappingValidator>>();
            _breedRepositoryMock = new Mock<IBreedRepository>();
            _colourRepositoryMock = new Mock<IColourRepository>();

            _validator = new IdcomsMappingValidator(
                _loggerMock.Object,
                _breedRepositoryMock.Object,
                _colourRepositoryMock.Object
            );
        }

        [Test]
        public async Task ValidateMapping_NullModel_ReturnsError()
        {
            
            var result = await _validator.ValidateMapping(null);

            
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Field, Is.EqualTo("Model"));
        }

        [Test]
        public async Task ValidateMapping_ValidModel_ReturnsSuccess()
        {
            
            var model = CreateValidQueueModel();
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsTrue(result.IsValid);
            Assert.That(result.Errors, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task ValidateMapping_InvalidOwnerData_ReturnsErrors()
        {
            
            var model = CreateValidQueueModel();
            model.Owner.Email = INVALID_EMAIL;
            model.Owner.Telephone = INVALID_PHONE;
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
            Assert.That(result.Errors.Exists(e => e.Field == "OwnerEmail"));
            Assert.That(result.Errors.Exists(e => e.Field == "OwnerPhone"));
        }

        [Test]
        public async Task ValidateMapping_InvalidPetData_ReturnsErrors()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.MicrochipNumber = INVALID_MICROCHIP;
            model.Pet.DOB = DateTime.UtcNow.AddDays(1); 
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "MicrochipNumber"));
            Assert.That(result.Errors.Exists(e => e.Field == "DOB"));
        }

        [Test]
        public async Task ValidateMapping_InvalidReferenceNumber_ReturnsErrors()
        {
            
            var model = CreateValidQueueModel();
            model.Application.ReferenceNumber = INVALID_REFERENCE;
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "ReferenceNumber"));
        }

        [Test]
        public async Task ValidateMapping_InvalidAddress_ReturnsErrors()
        {
            
            var model = CreateValidQueueModel();
            model.OwnerAddress.PostCode = INVALID_POSTCODE;
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "OwnerPostCode"));
        }

        [Test]
        public async Task ValidateMapping_SpecialBreedId_ValidatesCorrectly()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.BreedId = 99;
            model.Pet.AdditionalInfoMixedBreedOrUnknown = "Mixed breed description";
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task ValidateMapping_SpecialColourId_RequiresOtherColour()
        {
            
            var model = CreateValidQueueModel();
            model.Pet.ColourId = 11;
            model.Pet.OtherColour = ""; 
            SetupValidRepositoryResponses();

            
            var result = await _validator.ValidateMapping(model);

            
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "OtherColour"));
        }

       

        private static OfflineApplicationQueueModel CreateValidQueueModel()
        {
            return new OfflineApplicationQueueModel
            {
                Owner = new OwnerInfo
                {
                    FullName = "Test Owner",
                    Email = VALID_EMAIL,
                    Telephone = VALID_PHONE
                },
                Pet = new PetInfo
                {
                    Name = "Test Pet",
                    SpeciesId = 1,
                    BreedId = 1,
                    SexId = 1,
                    ColourId = 1,
                    MicrochipNumber = VALID_MICROCHIP,
                    DOB = DateTime.UtcNow.AddYears(-1)
                },
                Application = new ApplicationInfo
                {
                    ReferenceNumber = VALID_REFERENCE,
                    Status = "Authorised",
                    DateOfApplication = DateTime.UtcNow.AddDays(-1),
                    DateAuthorised = DateTime.UtcNow
                },
                OwnerAddress = new AddressInfo
                {
                    AddressLineOne = "123 Test St",
                    TownOrCity = "Test City",
                    PostCode = VALID_POSTCODE
                },
                Ptd = new PtdInfo
                {
                    DocumentReferenceNumber = VALID_REFERENCE
                },
                ApplicantAddress = new AddressInfo
                {
                    AddressLineOne = "456 Test St",
                    TownOrCity = "Test City",
                    PostCode = VALID_POSTCODE
                },
                Applicant = new ApplicantInfo
                {
                    FullName = "Test Applicant",
                    Email = VALID_EMAIL,
                    Telephone = VALID_PHONE
                }
            };
        }

        private void SetupValidRepositoryResponses()
        {
            _breedRepositoryMock.Setup(x => x.FindById(It.IsAny<int>()))
                .ReturnsAsync(new Entities.Breed { Id = 1, SpeciesId = 1 });

            _colourRepositoryMock.Setup(x => x.FindById(It.IsAny<int>()))
                .ReturnsAsync(new Entities.Colour { Id = 1 });
        }
    }
}