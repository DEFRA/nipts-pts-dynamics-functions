using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.Models.CustomException;
using Defra.PTS.Common.Models.Enums;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Entity = Defra.PTS.Common.Entities;
using Model = Defra.PTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Repositories.Implementation;

namespace Defra.PTS.Common.ApiServices.Tests.Implementation
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository>? _userRepository;
        private Mock<IRepository<Entity.Address>>? _repoAddressService;
        UserService? sut;

        [SetUp]
        public void SetUp()
        {
            _userRepository = new Mock<IUserRepository>();
            _repoAddressService = new Mock<IRepository<Entity.Address>>();
        }

        [Test]
        public async Task CreateUser_WhenValidData_ReturnsGuid()
        {
            Guid addressGuid = Guid.Empty;
            var modelAddress = new Model.Address()
            {
                AddressLineOne = "19 First Avenue",
                AddressLineTwo = "",
                TownOrCity = "Grays",
                County = "Essex",
                CountryName = "UK",
                PostCode = "RM13 4FT",

                IsActive = true,
                CreatedBy = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };

            var modelUser = new Model.User
            {
                Address = modelAddress,
                Email = "cuan@test.com",
                FullName = "Cuan Brown",
                FirstName = "Cuan",
                LastName = "Brown",
                Telephone = "9999999999",
                ContactId = Guid.Parse("EB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                Uniquereference = "123",
                SignInDateTime = DateTime.Now,
                SignOutDateTime = DateTime.Now,
                CreatedBy = Guid.Parse("FB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };



            var address = new Entity.Address()
            {
                AddressLineOne = "19 First Avenue",
                AddressLineTwo = "",
                TownOrCity = "Grays",
                County = "Essex",
                CountryName = "UK",
                PostCode = "RM13 4FT",
                AddressType = AddressType.User.ToString(),
                IsActive = true,
                CreatedBy = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };
            _repoAddressService!.Setup(a => a.Add(address)).Returns(Task.FromResult(address.Id = addressGuid));
            await _repoAddressService.Object.Add(address);
            _repoAddressService.Setup(a => a.SaveChanges()).ReturnsAsync(1);

            Guid userGuid = Guid.Empty;
            var user = new Entity.User
            {
                Id = userGuid,
                Email = "cuan@test.com",
                FullName = "Cuan Brown",
                FirstName = "Cuan",
                LastName = "Brown",
                AddressId = addressGuid,
                Telephone = "9999999999",
                ContactId = Guid.Parse("EB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                Uniquereference = "123",
                SignInDateTime = DateTime.Now,
                SignOutDateTime = DateTime.Now,
                CreatedBy = Guid.Parse("FB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };

            _userRepository!.Setup(a => a.Add(user)).Returns(Task.FromResult(user.Id = userGuid));

            await _userRepository.Object.Add(user);
            _userRepository.Setup(a => a.SaveChanges()).ReturnsAsync(1);


            sut = new UserService(_userRepository.Object, _repoAddressService.Object);

            var result = sut.CreateUser(modelUser);
            Assert.AreEqual(userGuid, result.Result);
            _userRepository.Verify(a => a.Add(user), Times.Once);
        }

        [Test]
        public void UpdateUser_WhenValidData_ReturnsGuid()
        {

            Guid addressGuid = Guid.NewGuid();

            Guid userGuid = Guid.Empty;
            var user = new Entity.User
            {
                Id = userGuid,
                Email = "cuan@test.com",
                FullName = "Cuan Brown",
                FirstName = "Cuan",
                LastName = "Brown",
                AddressId = addressGuid,
                Telephone = "9999999999",
                ContactId = Guid.Parse("EB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                Uniquereference = "123",
                SignInDateTime = DateTime.Now,
                SignOutDateTime = DateTime.Now,
                CreatedBy = Guid.Parse("FB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };
            _userRepository!.Setup(a => a.GetUser(It.IsAny<string>())).Returns(Task.FromResult(user)!);
            _userRepository.Setup(a => a.Update(user));
            _userRepository.Setup(a => a.SaveChanges()).ReturnsAsync(1);


            sut = new UserService(_userRepository.Object, _repoAddressService!.Object);

            var result = sut.UpdateUser("firstname", "lastname", "cuan@test.com", "9999999999", addressGuid);
            Assert.AreEqual(userGuid, result.Result);
            _userRepository.Verify(a => a.Update(user), Times.Once);
            _userRepository.Verify(a => a.SaveChanges(), Times.Once);
        }

        [Test]
        public void DoesUserExists_WhenInValidData_ReturnsError()
        {
            sut = new UserService(_userRepository!.Object, _repoAddressService!.Object);
            var expectedResult = $"Invalid ContactId";
            var result = Assert.ThrowsAsync<UserFunctionException>(() => sut.DoesUserExists(Guid.Empty));

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult, result?.Message);
        }


        [Test]
        public void DoesUserExists_WheValidData_ReturnsSuccess()
        {
            _userRepository!.Setup(a => a.DoesUserExists(It.IsAny<Guid>())).Returns(Task.FromResult(true));


            sut = new UserService(_userRepository.Object, _repoAddressService!.Object);
            var result = sut.DoesUserExists(Guid.NewGuid());
            Assert.IsTrue(result.Result);
        }

        [Test]
        public void DoesAddressExists_WhenValidData_ReturnsSuccess()
        {
            bool expectedResult = true;
            _userRepository!.Setup(a => a.DoesAddresssExists(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            sut = new UserService(_userRepository.Object, _repoAddressService!.Object);
            var result = sut.DoesAddressExists(Guid.Empty);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult, result.Result);
        }

        [Test]
        public void GetUserModel_WhenValidData_ReturnsModel()
        {
            sut = new UserService(_userRepository!.Object, _repoAddressService!.Object);

            var json = "{" +
                    "\"id\":\"00000000-0000-0000-0000-000000000000\"," +
                    "\"FullName\":null," +
                    "\"Email\":null," +
                    "\"FirstName\":\"Cuan\"," +
                    "\"LastName\":\"Brown\"," +
                    "\"AddressId\":null," +
                    "\"Role\":\"test\"," +
                    "\"Telephone\":null," +
                    "\"ContactId\":null," +
                    "\"SignInDateTime\":null," +
                    "\"SignOutDateTime\":null," +
                    "\"CreatedBy\":null," +
                    "\"CreatedOn\":null," +
                    "\"UpdatedBy\":null," +
                    "\"UpdatedOn\":null," +
                    "\"Address\":null" +
                "}";

            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            sut = new UserService(_userRepository.Object, _repoAddressService.Object);
            var result = sut.GetUserModel(memoryStream);
            Assert.IsNotNull(result);
            Assert.AreEqual("Cuan", result.Result.FirstName);
            Assert.AreEqual("Brown", result.Result.LastName);

        }

        [Test]
        public void GetUserEmailModel_WhenValidData_ReturnsModel()
        {
            sut = new UserService(_userRepository!.Object, _repoAddressService!.Object);

            var json = "{" +
                 "\"Email\":\"tt@tt.com\"," +
                    "\"Type\":null" +

                "}";

            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            sut = new UserService(_userRepository.Object, _repoAddressService.Object);
            var result = sut.GetUserModel(memoryStream);
            Assert.IsNotNull(result);
            Assert.AreEqual("tt@tt.com", result.Result.Email);
        }

        [Test]
        public void GetUserDetails_WhenValidData_ReturnsValidDataBack()
        {
            Guid item1 = Guid.NewGuid();
            Guid item2 = Guid.NewGuid();
            string item3 = "test";
            _userRepository!.Setup(a => a.GetUserDetails(It.IsAny<Guid>())).ReturnsAsync((item1, item2, item3));

            sut = new UserService(_userRepository.Object, _repoAddressService!.Object);
            var result = sut.GetUserDetails(Guid.Empty);

            Assert.IsNotNull(result);
            Assert.AreEqual(item1, result.Item1);
            Assert.AreEqual(item2, result.Item2);
            Assert.AreEqual(item3, result.Item3);
        }

        [Test]
        public void GetUserRequestModel_WhenValidData_ReturnsValidDataBack()
        {
            Guid contactId = Guid.NewGuid();
            var json = "{" +
                    "\"ContactId\":\"" + contactId.ToString() + "\"," +
                    "\"Address\":null" +
                "}";

            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            sut = new UserService(_userRepository!.Object, _repoAddressService!.Object);
            var result = sut.GetUserRequestModel(memoryStream);

            Assert.IsNotNull(result);
            Assert.AreEqual(contactId, result.Result.ContactId);
        }

        [Test]
        public void AddAddress_WhenValidData_ReturnsEmptyGuidBack()
        {
            Guid addressId = Guid.Empty;

            Model.UserRequest userRequest = new Model.UserRequest();
            userRequest.Address = null;

            _repoAddressService!.Setup(a => a.Add(It.IsAny<Entity.Address>())).Returns(Task.CompletedTask);
            _repoAddressService.Setup(a => a.SaveChanges()).ReturnsAsync(1);


            sut = new UserService(_userRepository!.Object, _repoAddressService.Object);
            var result = sut.AddAddress(userRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(addressId, result.Result);
        }

        [Test]
        public void AddAddress_WhenValidData_ReturnsValidDataBack()
        {
            Guid addressId = Guid.Empty;
            var modelAddress = new Model.Address()
            {
                AddressLineOne = "19 First Avenue",
                AddressLineTwo = "",
                TownOrCity = "Grays",
                County = "Essex",
                CountryName = "UK",
                PostCode = "RM13 4FT",

                IsActive = true,
                CreatedBy = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };

            Model.UserRequest userRequest = new Model.UserRequest();
            userRequest.Address = modelAddress;

            _repoAddressService!.Setup(a => a.Add(It.IsAny<Entity.Address>())).Returns(Task.CompletedTask);
            _repoAddressService.Setup(a => a.SaveChanges()).ReturnsAsync(1);


            sut = new UserService(_userRepository!.Object, _repoAddressService.Object);
            var result = sut.AddAddress(userRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(addressId, result.Result);
        }

        [Test]
        public void UpdateAddress_CreateEntry_WhenNullData_ReturnsValidDataBack()
        {
            Guid addressId = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6");
            var modelAddress = new Model.Address()
            {
                Id = addressId,
                AddressLineOne = "19 First Avenue",
                AddressLineTwo = "",
                TownOrCity = "Grays",
                County = "Essex",
                CountryName = "UK",
                PostCode = "RM13 4FT",
                IsActive = true,
                CreatedBy = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };

            _repoAddressService!
            .Setup(a => a.Find(It.IsAny<Guid>()))
            .Returns(Task.FromResult<Entity.Address>(null!));

            Model.UserRequest userRequest = new Model.UserRequest();
            userRequest.Address = modelAddress;

            sut = new UserService(_userRepository!.Object, _repoAddressService.Object);
            var result = sut.UpdateAddress(userRequest, addressId);

            Assert.IsNotNull(result);
            Assert.AreEqual(Guid.Empty, result.Result);
        }

        [Test]
        public void UpdateAddress_WhenValidData_ReturnsValidDataBack()
        {
            Guid addressId = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6");
            var modelAddress = new Model.Address()
            {
                Id = addressId,
                AddressLineOne = "19 First Avenue",
                AddressLineTwo = "",
                TownOrCity = "Grays",
                County = "Essex",
                CountryName = "UK",
                PostCode = "RM13 4FT",
                IsActive = true,
                CreatedBy = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };

            var address = new Entity.Address()
            {
                Id = addressId,
                AddressLineOne = "19 First Avenue",
                AddressLineTwo = "",
                TownOrCity = "Grays",
                County = "Essex",
                CountryName = "UK",
                PostCode = "RM13 4FT",
                IsActive = true,
                CreatedBy = Guid.Parse("AB4ECAEA-877C-4560-EDE4-08DBD163F0B6"),
                CreatedOn = DateTime.Now
            };

            _repoAddressService!.Setup(a => a.Find(It.IsAny<Guid>())).Returns(Task.FromResult(address));

            Model.UserRequest userRequest = new Model.UserRequest();
            userRequest.Address = modelAddress;

            sut = new UserService(_userRepository!.Object, _repoAddressService.Object);
            var result = sut.UpdateAddress(userRequest, addressId);

            Assert.IsNotNull(result);
            Assert.AreEqual(addressId, result.Result);
        }

        [Test]
        public void GetUserRequestModel_WhenInvalidData_ThrowsUserException()
        {
            var json = "{" +
                "junk" +
                "}";

            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            sut = new UserService(_userRepository!.Object, _repoAddressService!.Object);
            Assert.ThrowsAsync<UserFunctionException>(() => sut.GetUserRequestModel(memoryStream));
        }

        [Test]
        public void GetUserModel_WhenInvalidData_ThrowsUserException()
        {
            var json = "{" +
                "junk" +
                "}";

            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            sut = new UserService(_userRepository!.Object, _repoAddressService!.Object);
            Assert.ThrowsAsync<UserFunctionException>(() => sut.GetUserModel(memoryStream));
        }
    }
}
