using System;
using System.Linq;
using NUnit.Framework;
using truepele.Common;

namespace Tests
{
    [TestFixture]
    internal class RandomExtensionsTests
    {
        [TestCase(1)]
        [TestCase(9)]
        [TestCase(1000000)]
        public void NextIntEnumerable_ReturnsCorrectCount(int count)
        {
            // Arrange
            var r = new Random();

            // Act 
            var result = r.NextIntEnumerable(count);

            // Assert
            Assert.AreEqual(count, result.Count());
        }

        
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(9)]
        [TestCase(22)]
        [TestCase(1100)]
        [TestCase(1105)]
        [TestCase(1000000)]
        public void NextIntEnumerable_ReturnsCorrectMaxValue(int maxValue)
        {
            // Arrange
            var r = new Random();

            // Act 
            var result = r.NextIntEnumerable(100, maxValue);

            // Assert
            Assert.LessOrEqual(result.Max(), maxValue);
        }


        [Test]
        public void NextIntEnumerable_Throws_ForNullInstance()
        {
            // Arrange
            Random r = null;

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => r.NextIntEnumerable(100));
        }


        [Test]
        public void NextIntEnumerable_DoeNotThrow_ForNullMaxValue()
        {
            // Arrange
            var r = new Random();

            // Act / Assert
            Assert.DoesNotThrow(() => r.NextIntEnumerable(100, null));
        }


        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-1000000)]
        public void NextIntEnumerable_Throws_ForCountLessThenOne(int count)
        {
            // Arrange
            var r = new Random();

            // Act / Assert
            Assert.Throws<ArgumentException>(() => r.NextIntEnumerable(count));
        }
    }
}