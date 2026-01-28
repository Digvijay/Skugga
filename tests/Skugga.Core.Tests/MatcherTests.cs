#nullable enable
using System;
using System.Collections.Generic;
using Skugga.Core;
using Xunit;
using Range = Skugga.Core.Range;

namespace Skugga.Core.Tests
{
    public interface IService
    {
        bool Process(int n);
        bool ProcessNullable(int? n);
        bool SetStatus(string status);
        bool Log(string message);
        bool ValidatePhone(string phone);
        bool ProcessList(IEnumerable<string> items);
    }

    public class MatcherTests
    {
        [Fact]
        public void Mock_It_IsIn()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.SetStatus(It.IsIn("Active", "Pending"))).Returns(true);

            Assert.True(mock.SetStatus("Active"));
            Assert.True(mock.SetStatus("Pending"));
            Assert.False(mock.SetStatus("Complete"));
        }

        [Fact]
        public void Mock_It_IsNotNull()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Log(It.IsNotNull<string>())).Returns(true);

            Assert.True(mock.Log("hello"));
            Assert.False(mock.Log(null!));
        }

        [Fact]
        public void Mock_It_IsNotNull_NullableValueType()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.ProcessNullable(It.IsNotNull<int?>())).Returns(true);

            Assert.True(mock.ProcessNullable(5));
            Assert.False(mock.ProcessNullable(null));
        }

        [Fact]
        public void Mock_It_IsIn_IEnumerable()
        {
            var mock = Mock.Create<IService>();
            var list = new List<string> { "Active", "Pending" };
            mock.Setup(x => x.SetStatus(It.IsIn<string>(list))).Returns(true);

            Assert.True(mock.SetStatus("Active"));
            Assert.True(mock.SetStatus("Pending"));
            Assert.False(mock.SetStatus("Complete"));
        }

        [Fact]
        public void Mock_It_IsNotIn()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.SetStatus(It.IsNotIn("Error", "Deleted"))).Returns(true);

            Assert.True(mock.SetStatus("Active"));
            Assert.False(mock.SetStatus("Error"));
            Assert.False(mock.SetStatus("Deleted"));
        }

        [Fact]
        public void Mock_It_IsInRange()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Process(It.IsInRange(1, 10, Range.Inclusive))).Returns(true);

            Assert.True(mock.Process(1));
            Assert.True(mock.Process(5));
            Assert.True(mock.Process(10));
            Assert.False(mock.Process(0));
            Assert.False(mock.Process(11));
        }

        [Fact]
        public void Mock_It_IsInRange_Exclusive()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Process(It.IsInRange(1, 10, Range.Exclusive))).Returns(true);

            Assert.False(mock.Process(1));
            Assert.True(mock.Process(5));
            Assert.False(mock.Process(10));
        }

        [Fact]
        public void Mock_It_IsRegex()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.ValidatePhone(It.IsRegex(@"^\d{3}-\d{4}$"))).Returns(true);

            Assert.True(mock.ValidatePhone("123-4567"));
            Assert.False(mock.ValidatePhone("1234-567"));
            Assert.False(mock.ValidatePhone("abc-defg"));
        }

        [Fact]
        public void Mock_It_IsRegex_WithOptions()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.SetStatus(It.IsRegex(@"^active$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))).Returns(true);

            Assert.True(mock.SetStatus("ACTIVE"));
            Assert.True(mock.SetStatus("active"));
            Assert.False(mock.SetStatus("inactive"));
        }

        [Fact]
        public void Mock_Throws_NonVoid()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Process(It.IsAny<int>())).Throws(new InvalidOperationException("error"));

            Assert.Throws<InvalidOperationException>(() => mock.Process(1));
        }
    }
}
