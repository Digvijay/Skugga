#nullable enable
using System;
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests
{
    public abstract class BaseService
    {
        public virtual string GetName() => "Base";
        public virtual int Count { get; set; } = 10;
        public abstract string GetAbstract();
    }

    public class CallBaseTests
    {
        [Fact]
        public void Mock_CallBase_Method()
        {
            var mock = Mock.Create<BaseService>();
            Mock.Get(mock).Handler.CallBase = true;

            // No setup - should call base
            Assert.Equal("Base", mock.GetName());

            // Setup - should call setup
            Mock.Get(mock).Handler.AddSetup("GetName", Array.Empty<object?>(), "Mocked", null);
            Assert.Equal("Mocked", mock.GetName());
        }

        [Fact]
        public void Mock_CallBase_Property()
        {
            var mock = Mock.Create<BaseService>();
            Mock.Get(mock).Handler.CallBase = true;

            // No setup - should call base
            Assert.Equal(10, mock.Count);

            mock.Count = 20;
            Assert.Equal(20, mock.Count);

            // Setup getter - should call setup
            Mock.Get(mock).Handler.AddSetup("get_Count", Array.Empty<object?>(), 50, null);
            Assert.Equal(50, mock.Count);
        }

        [Fact]
        public void Mock_CallBase_AbstractMethod_Throws()
        {
            var mock = Mock.Create<BaseService>();
            Mock.Get(mock).Handler.CallBase = true;

            // Abstract method has no base implementation.
            // In Skugga, it will return default(string) i.e null, because CallBase check fails for abstract methods.
            Assert.Null(mock.GetAbstract());
        }
    }
}
