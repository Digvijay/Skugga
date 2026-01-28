using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for out and ref parameter support in Skugga mocks.
/// </summary>
public class OutRefParameterTests
{
    // Test interface with out/ref parameters
    public interface IParser
    {
        bool TryParse(string input, out int result);
        bool TryGetValue(string key, out string value);
        void Increment(ref int value);
        void Transform(int input, ref int output);
        bool TryParseMultiple(string input1, out int result1, string input2, out int result2);
    }

    [Fact]
    public void OutParameter_WithStaticValue_SetsValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(x => x.TryParse("123", out dummy))
            .OutValue(1, 123)
            .Returns(true);

        // Act
        int result;
        bool success = mock.TryParse("123", out result);

        // Assert
        Assert.True(success);
        Assert.Equal(123, result);
    }

    [Fact]
    public void OutParameter_WithDynamicValue_ComputesFromArguments()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(x => x.TryParse(It.IsAny<string>(), out dummy))
            .OutValueFunc(1, args => int.Parse((string)args[0]!))
            .Returns(true);

        // Act
        int result;
        bool success = mock.TryParse("456", out result);

        // Assert
        Assert.True(success);
        Assert.Equal(456, result);
    }

    [Fact]
    public void OutParameter_StringType_SetsValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        string dummy = "";
        mock.Setup(x => x.TryGetValue("key1", out dummy))
            .OutValue(1, "value1")
            .Returns(true);

        // Act
        string value;
        bool success = mock.TryGetValue("key1", out value);

        // Assert
        Assert.True(success);
        Assert.Equal("value1", value);
    }

    [Fact]
    public void RefParameter_WithStaticValue_ModifiesValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(x => x.Increment(ref dummy))
            .RefValue(0, 42);

        // Act
        int value = 10;
        mock.Increment(ref value);

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void RefParameter_WithDynamicValue_ComputesFromArguments()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(x => x.Transform(It.IsAny<int>(), ref dummy))
            .RefValueFunc(1, args => ((int)args[0]!) * 2);

        // Act
        int output = 0;
        mock.Transform(5, ref output);

        // Assert
        Assert.Equal(10, output);
    }

    [Fact]
    public void MultipleOutParameters_WithStaticValues_SetsBoth()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy1 = 0, dummy2 = 0;
        mock.Setup(x => x.TryParseMultiple("10", out dummy1, "20", out dummy2))
            .OutValue(1, 10)
            .OutValue(3, 20)
            .Returns(true);

        // Act
        int result1, result2;
        bool success = mock.TryParseMultiple("10", out result1, "20", out result2);

        // Assert
        Assert.True(success);
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
    }

    [Fact]
    public void OutParameter_NoSetup_UsesDefaultValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        // No setup - should return default values

        // Act
        int result;
        bool success = mock.TryParse("123", out result);

        // Assert
        Assert.False(success); // No setup, so returns default(bool) = false
        Assert.Equal(0, result); // Out parameter gets default(int) = 0
    }

    [Fact]
    public void OutParameter_CanVerifyCall()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(x => x.TryParse(It.IsAny<string>(), out dummy))
            .OutValue(1, 999)
            .Returns(true);

        // Act
        int result;
        mock.TryParse("test", out result);

        // Assert - verify the call was made
        int verifyDummy = 0;
        mock.Verify(x => x.TryParse("test", out verifyDummy), Times.Once());
    }

    [Fact]
    public void RefParameter_CanVerifyCall()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(x => x.Increment(ref dummy))
            .RefValue(0, 100);

        // Act
        int value = 50;
        mock.Increment(ref value);

        // Assert - verify the call was made
        int verifyDummy = 0;
        mock.Verify(x => x.Increment(ref verifyDummy), Times.Once());
    }
}

