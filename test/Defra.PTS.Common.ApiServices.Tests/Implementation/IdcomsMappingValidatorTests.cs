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
        private const string INVALID_REFERENCE = "12345678";  
        private const string VALID_MICROCHIP = "123456789012345";
        private const string INVALID_EMAIL = "invalid-email";
        private const string INVALID_PHONE = "123";
        private const string INVALID_POSTCODE = "INVALID";
        private const string INVALID_MICROCHIP = "123";

        private Mock<ILogger<IdcomsMappingValidator>> _loggerMock = null!;
        private Mock<IBreedRepository> _breedRepositoryMock = null!;
        private Mock<IColourRepository> _colourRepositoryMock = null!;
        private IdcomsMappingValidator _validator = null!;

        private const string VALID_GB_REFERENCE = "GB12345678";

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

        [Test]
        public async Task ValidateMapping_TimeoutInBreedValidation_ReturnsError()
        {
            var model = CreateValidQueueModel();
            _breedRepositoryMock.Setup(x => x.FindById(It.IsAny<int>()))
                .Returns(async () =>
                {
                    await Task.Delay(31000);
                    return new Entities.Breed { Id = 1, SpeciesId = 1 };
                });

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "Timeout"));
        }

        [Test]
        public async Task ValidateMapping_InvalidDynamicId_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Application.DynamicId = "invalid-guid";
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "DynamicId"));
        }

        [Test]
        public async Task ValidateMapping_AuthorizationDateBeforeApplication_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Application.DateOfApplication = DateTime.UtcNow;
            model.Application.DateAuthorised = DateTime.UtcNow.AddDays(-1);  
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "DateAuthorised"));
        }

        [Test]
        public async Task ValidateMapping_MismatchedBreedSpecies_ReturnsError()
        {
            var model = CreateValidQueueModel();
            _breedRepositoryMock.Setup(x => x.FindById(It.IsAny<int>()))
                .ReturnsAsync(new Entities.Breed { Id = 1, SpeciesId = 2 });

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "PetBreedId"));
        }

        [Test]
        public async Task ValidateMapping_InvalidApplicantData_ReturnsErrors()
        {
            var model = CreateValidQueueModel();
            model.Applicant.FirstName = new string('A', 101);
            model.Applicant.LastName = new string('B', 101);
            model.Applicant.Email = INVALID_EMAIL;
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "ApplicantFirstName"));
            Assert.That(result.Errors.Exists(e => e.Field == "ApplicantLastName"));
            Assert.That(result.Errors.Exists(e => e.Field == "ApplicantEmail"));
        }

        [Test]
        public async Task ValidateMapping_MissingTravelDocument_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Ptd = new PtdInfo();
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "ReferenceNumber"));
        }


        [Test]
        public async Task ValidateMapping_InvalidUniqueFeatureDescription_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Pet.UniqueFeatureDescription = new string('A', 301);
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "UniqueFeatureDescription"));
        }

        [Test]
        public async Task ValidateMapping_InvalidApplicationStatus_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Application.Status = "Pending";
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "Status"));
        }

        [Test]
        public async Task ValidateMapping_NonMatchingDocumentReference_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Ptd.DocumentReferenceNumber = "GB87654321";  
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "ReferenceNumber"));
        }

        [Test]
        public async Task ValidateMapping_TimeoutInColorValidation_ReturnsError()
        {
            var model = CreateValidQueueModel();
            _colourRepositoryMock.Setup(x => x.FindById(It.IsAny<int>()))
                .Returns(async () =>
                {
                    await Task.Delay(31000);
                    return new Entities.Colour { Id = 1 };
                });

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "Timeout"));
        }

        [Test]
        public async Task ValidateMapping_InvalidPetGenderType_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Pet.SexId = 999; 

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "PetSexId"));
        }

        [Test]
        public async Task ValidateMapping_OwnerNameTooShort_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Owner.FullName = "A"; 

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "OwnerName"));
        }

        [Test]
        public async Task ValidateMapping_InvalidEmailFormat_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Owner.Email = "@invalid@email@test.com"; 

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "OwnerEmail"));
        }

        [Test]
        public async Task ValidateMapping_NullApplicantAddress_Validates()
        {
            var model = CreateValidQueueModel();
#nullable disable
            model.ApplicantAddress = null;
#nullable enable
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsTrue(result.IsValid);
        }


        [Test]
        public async Task ValidateMapping_InvalidAddressLine2Length_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.OwnerAddress.AddressLineTwo = new string('A', 251);
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "OwnerAddressLineTwo"));
        }

        [Test]
        public async Task ValidateMapping_TimeoutInTravelDocumentValidation_ReturnsError()
        {
            
            var model = CreateValidQueueModel();

            
            var delayTask = Task.Delay(TimeSpan.FromSeconds(31));

            
            _colourRepositoryMock.Setup(x => x.FindById(It.IsAny<int>()))
                .Returns(async () =>
                {
                    await delayTask;
                    return new Entities.Colour { Id = 1 };
                });

           
            var result = await _validator.ValidateMapping(model);

           
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationError>(e => e.Field == "Timeout" && e.Message.Contains("Validation operation timed out")));
            });
        }

        [Test]
        public async Task ValidateMapping_NullFields_ReturnsMultipleErrors()
        {
            var model = CreateValidQueueModel();
#nullable disable
            model.Owner.Email = null;
#nullable enable

#nullable disable
            model.OwnerAddress.PostCode = null;
#nullable enable
#nullable disable
            model.Pet.Name = null;
#nullable enable


            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Count, Is.GreaterThan(2));
        }

        [Test]
        public async Task ValidateMapping_MicrochipDateInFuture_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Pet.MicrochippedDate = DateTime.UtcNow.AddDays(1);
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "MicrochippedDate"));
        }

        [Test]
        public async Task ValidateMapping_AssistedDigitalReferenceNumber_ReturnsSuccess()
        {
            var model = CreateValidQueueModel();
            model.Application.ReferenceNumber = "GB826AD1F2E";
            model.Ptd.DocumentReferenceNumber = "GB826AD1F2E";
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task ValidateMapping_InvalidAssistedDigitalReferenceNumberTooLong_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Application.ReferenceNumber = "GB826AD12345"; 
            model.Ptd.DocumentReferenceNumber = "GB826AD12345";
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "ReferenceNumber"));
        }

        [Test]
        public async Task ValidateMapping_InvalidAssistedDigitalReferenceNumberNonHex_ReturnsError()
        {
            var model = CreateValidQueueModel();
            model.Application.ReferenceNumber = "GB826ADGHIJ"; 
            model.Ptd.DocumentReferenceNumber = "GB826ADGHIJ";
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors.Exists(e => e.Field == "ReferenceNumber"));
        }

        [Test]
        public async Task ValidateMapping_AuthorizationDateSameAsApplicationDate_ReturnsSuccess()
        {
            var model = CreateValidQueueModel();
            var baseDate = DateTime.UtcNow.Date;
            model.Application.DateOfApplication = baseDate.AddHours(9);  
            model.Application.DateAuthorised = baseDate.AddHours(17);   
            SetupValidRepositoryResponses();

            var result = await _validator.ValidateMapping(model);

            Assert.IsTrue(result.IsValid);
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
                    ReferenceNumber = VALID_GB_REFERENCE,
                    Status = "Authorised",
                    DateOfApplication = DateTime.UtcNow.Date.AddHours(-1),
                    DateAuthorised = DateTime.UtcNow.Date
                },
                OwnerAddress = new AddressInfo
                {
                    AddressLineOne = "123 Test St",
                    TownOrCity = "Test City",
                    PostCode = VALID_POSTCODE
                },
                Ptd = new PtdInfo
                {
                    DocumentReferenceNumber = VALID_GB_REFERENCE
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
                    Telephone = VALID_PHONE,
                    FirstName = "Test",
                    LastName = "Applicant"
                },
                CreatedBy = Guid.NewGuid()
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