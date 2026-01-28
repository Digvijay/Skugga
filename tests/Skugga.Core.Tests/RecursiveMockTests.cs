#nullable enable
using System;
using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests
{
    public interface IAddress
    {
        string City { get; }
    }

    public interface IPerson
    {
        IAddress Address { get; }
    }

    public class RecursiveMockTests
    {
        [Fact]
        public void Mock_RecursiveSetup_Works()
        {
            var mock = Mock.Create<IPerson>(DefaultValue.Mock);

            // If the source generator handles this, it shouldn't even execute the lambda
            mock.Setup(x => x.Address.City).Returns("Seattle");

            // But wait, who is returning the Address mock?
            // If we have DefaultValue.Mock, mock.Address should be a mock.
            // When we called mock.Setup(x => x.Address.City), the source generator
            // intercepted it.

            Assert.NotNull(mock.Address);
            Assert.Equal("Seattle", mock.Address.City);
        }
    }
}
