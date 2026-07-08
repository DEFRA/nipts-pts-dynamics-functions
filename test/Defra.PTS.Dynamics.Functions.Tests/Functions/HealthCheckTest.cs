using Defra.PTS.Common.ApiServices.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using testFunc = Defra.PTS.Dynamics.Functions.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Dynamics.Functions.Tests.Functions
{
    public class HealthCheckTest
    {
        private Mock<HttpRequest> _requestMock = new();
        private Mock<ILogger> _loggerMock = new();
        private Mock<IApplicationService> _applicationServiceMock = new();
        private Mock<IDynamicsService> _dynamicsServiceMock = new();
        testFunc.HealthCheck? sut;

        [SetUp]
        public void SetUp()
        {
            sut = new testFunc.HealthCheck(_applicationServiceMock.Object, _dynamicsServiceMock.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _requestMock = new();
            _dynamicsServiceMock = new();
            _applicationServiceMock = new();    
            _loggerMock = new();
        }

        [Test]
        public void HealthCheck_WhenTrue_Then_ReturnsServiceAvailable()
        {
            _applicationServiceMock.Setup(a => a.PerformHealthCheckLogic()).Returns(Task.FromResult(true));
            _dynamicsServiceMock.Setup(a => a.PerformHealthCheckLogic()).Returns(Task.FromResult(true));
            var result = sut!.Run(_requestMock.Object, _loggerMock.Object);
            var okResult = result.Result as OkResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, okResult?.StatusCode);
            _applicationServiceMock.Verify(a => a.PerformHealthCheckLogic(), Times.Once);
            _dynamicsServiceMock.Verify(a => a.PerformHealthCheckLogic(), Times.Once);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]

        [TestCase(true, false)]
        public void HealthCheck_WhenFalse_Then_ReturnsServiceUnavailable(bool isSqlHealthy, bool isDynamicshealthy)
        {
            _applicationServiceMock.Setup(a => a.PerformHealthCheckLogic()).Returns(Task.FromResult(isSqlHealthy));
            _dynamicsServiceMock.Setup(a => a.PerformHealthCheckLogic()).Returns(Task.FromResult(isDynamicshealthy));
            var result = sut!.Run(_requestMock.Object, _loggerMock.Object);
            var okResult = result.Result as StatusCodeResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(503, okResult?.StatusCode);
            _applicationServiceMock.Verify(a => a.PerformHealthCheckLogic(), Times.Once);
            _dynamicsServiceMock.Verify(a => a.PerformHealthCheckLogic(), Times.Once);
        }
    }
}
