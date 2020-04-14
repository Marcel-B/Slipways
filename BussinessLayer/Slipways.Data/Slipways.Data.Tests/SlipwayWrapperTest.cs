using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Data.Repositories;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slipways.Data.Tests
{
    public class Reporter<T> : IObserver<T>
    {
        private IDisposable unsubscriber;
        public void OnCompleted()
        {
            Console.WriteLine("The Location Tracker has completed transmitting data");
            this.Unsubscribe();
        }

        public void OnError(
            Exception error)
        {
            Console.WriteLine("Error happens");
        }

        public void OnNext(
            T value)
        {
            Console.WriteLine("Updated");
        }

        public virtual void Unsubscribe()
        {
            unsubscriber.Dispose();
        }
    }

    [TestFixture]
    public class SlipwayWrapperTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SlipwayWrapper_ToDto_ExtrasIsEmptyWhenNoExtrasInClass()
        {
            // // Arrange
            // var slipway = new Slipway
            // {
            //     Id = Guid.Parse("F2638A1A-9F5C-4858-BC04-786149E9ECFF"),
            //     Name = "Test",
            //     City = "K�ln",
            //     WaterFk = Guid.Parse("5790B3B2-A002-45C3-821D-6D8EC194397E")
            // };

            // // Act
            // var actual = slipway.ToDto().Extras;

            // // Assert
            // Assert.IsEmpty(actual);
        }

        [Test]
        public void SlipwayWrapper_ToDto_ExtrasAreWrappedCorrectly()
        {
            // // Arrange
            // var expected = 2;
            // var slipway = new Slipway
            // {
            //     Id = Guid.Parse("F2638A1A-9F5C-4858-BC04-786149E9ECFF"),
            //     Name = "Test",
            //     City = "K�ln",
            //     WaterFk = Guid.Parse("5790B3B2-A002-45C3-821D-6D8EC194397E"),
            //     Extras = new List<Extra> { new Extra { Id = Guid.NewGuid(), Name = "Eins" }, new Extra { Id = Guid.NewGuid(), Name = "Zwei" } }
            // };

            // // Act
            // var extras = slipway.ToDto().Extras;
            // var actual = extras.ToList().Count;

            // // Assert
            // Assert.AreEqual(expected, actual);
        }
    }
}
