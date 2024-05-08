using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Defra.PTS.Common.Models.Helper;

namespace Defra.PTS.Dynamics.Functions.Tests.Models
{
    public class EnumExtensionsTest
    {
        // Define an enum for testing
        public enum TestEnum
        {
            [System.ComponentModel.Description("Value 1 Description")]
            Value1,
            [System.ComponentModel.Description("Value 2 Description")]
            Value2,
            Value3
        }

        [Test]
        public void GetDescription_EnumValueWithDescriptionAttribute_ReturnsDescription()
        {
            // Arrange
            TestEnum enumValue = TestEnum.Value1;

            // Act
            string description = enumValue.GetDescription();

            // Assert
            Assert.AreEqual("Value 1 Description", description);
        }

        [Test]
        public void GetDescription_EnumValueWithoutDescriptionAttribute_ReturnsEnumToString()
        {
            // Arrange
            TestEnum enumValue = TestEnum.Value3;

            // Act
            string description = enumValue.GetDescription();

            // Assert
            Assert.AreEqual("Value3", description);
        }
    }
}
