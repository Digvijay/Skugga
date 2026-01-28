#nullable enable
using System;
using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests
{
    public interface IConfig
    {
        int Id { get; }
        string Name { get; }
        bool IsActive { get; }
        bool IsDeleted { get; }
    }

    public class LinqToMocksTests
    {
        [Fact]
        public void Mock_Of_SetsProperties()
        {
            var config = Mock.Of<IConfig>(x => x.Id == 123 && x.Name == "Test");
            
            Assert.Equal(123, config.Id);
            Assert.Equal("Test", config.Name);
        }

        [Fact]
        public void Mock_Of_SetsBooleanProperties()
        {
            // x.IsActive implies == true
            // !x.IsDeleted implies == false
            var config = Mock.Of<IConfig>(x => x.IsActive && !x.IsDeleted);
            
            Assert.True(config.IsActive);
            Assert.False(config.IsDeleted);
        }
        
        [Fact]
        public void Mock_Of_MixedConditions()
        {
            var config = Mock.Of<IConfig>(x => x.Id == 99 && x.IsActive);
            
            Assert.Equal(99, config.Id);
            Assert.True(config.IsActive);
        }

        [Fact]
        public void Mock_Of_EvaluatesValue()
        {
            var expectedId = 500;
            var config = Mock.Of<IConfig>(x => x.Id == expectedId);
            
            Assert.Equal(500, config.Id);
        }
    }
}
