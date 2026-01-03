using Xunit;
using Skugga.Core;

namespace Skugga.Core.Tests;

public interface IParser
{
    bool TryParse(string input, out int result);
    bool TryParseDouble(string input, out double result);
    void GetValues(out int x, out int y);
    bool TryGetValue(string key, out string? value);
}

public interface IRefService
{
    void ModifyValue(ref int value);
    void SwapValues(ref int a, ref int b);
    int ProcessRef(ref string input);
}

public interface IMixedService
{
    bool TryProcess(string input, ref int counter, out string result);
    void MixedParameters(int normal, ref int refParam, out int outParam, string another);
}

public interface IMultiOutService
{
    void GetValues(int input, out int intResult, out string strResult);
}

public interface IVoidOutService
{
    void ProcessValue(string input, out int result);
}

public class OutRefTests
{
    [Fact]
    public void OutParameter_SingleInt_ReturnsConfiguredValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(m => m.TryParse("42", out dummy))
            .Returns(true)
            .OutValue(1, 42);

        // Act
        var success = mock.TryParse("42", out int result);

        // Assert
        Assert.True(success);
        Assert.Equal(42, result);
    }

    [Fact]
    public void OutParameter_FailureCase_ReturnsZero()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(m => m.TryParse("invalid", out dummy))
            .Returns(false)
            .OutValue(1, 0);

        // Act
        var success = mock.TryParse("invalid", out int result);

        // Assert
        Assert.False(success);
        Assert.Equal(0, result);
    }

    [Fact]
    public void OutParameter_Double_ReturnsConfiguredValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        double dummy = 0.0;
        mock.Setup(m => m.TryParseDouble("3.14", out dummy))
            .Returns(true)
            .OutValue(1, 3.14);

        // Act
        var success = mock.TryParseDouble("3.14", out double result);

        // Assert
        Assert.True(success);
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void OutParameter_MultipleOut_ReturnsAllValues()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummyX = 0, dummyY = 0;
        mock.Setup(m => m.GetValues(out dummyX, out dummyY))
            .OutValue(0, 10)
            .OutValue(1, 20);

        // Act
        mock.GetValues(out int x, out int y);

        // Assert
        Assert.Equal(10, x);
        Assert.Equal(20, y);
    }

    [Fact]
    public void OutParameter_String_ReturnsConfiguredValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        string? dummy = null;
        mock.Setup(m => m.TryGetValue("key1", out dummy))
            .Returns(true)
            .OutValue(1, "value1");

        // Act
        var success = mock.TryGetValue("key1", out string? result);

        // Assert
        Assert.True(success);
        Assert.Equal("value1", result);
    }

    [Fact]
    public void OutParameter_NotFound_ReturnsNull()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        string? dummy = null;
        mock.Setup(m => m.TryGetValue("missing", out dummy))
            .Returns(false)
            .OutValue(1, null);

        // Act
        var success = mock.TryGetValue("missing", out string? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void RefParameter_ModifiesValue()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();
        int dummy = It.IsAny<int>();  // Use It.IsAny to match any value
        mock.Setup(m => m.ModifyValue(ref dummy))
            .RefValue(0, 100);

        // Act
        int value = 50;
        mock.ModifyValue(ref value);

        // Assert
        Assert.Equal(100, value);
    }

    [Fact]
    public void RefParameter_SwapValues()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();
        int dummyA = It.IsAny<int>(), dummyB = It.IsAny<int>();
        mock.Setup(m => m.SwapValues(ref dummyA, ref dummyB))
            .RefValue(0, 20)
            .RefValue(1, 10);

        // Act
        int a = 10;
        int b = 20;
        mock.SwapValues(ref a, ref b);

        // Assert
        Assert.Equal(20, a);
        Assert.Equal(10, b);
    }

    [Fact]
    public void RefParameter_WithReturnValue()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();
        string dummy = It.IsAny<string>();
        mock.Setup(m => m.ProcessRef(ref dummy))
            .Returns(42)
            .RefValue(0, "modified");

        // Act
        string input = "original";
        int result = mock.ProcessRef(ref input);

        // Assert
        Assert.Equal(42, result);
        Assert.Equal("modified", input);
    }

    [Fact]
    public void MixedParameters_OutAndRef_WorkTogether()
    {
        // Arrange
        var mock = Mock.Create<IMixedService>();
        int dummyCounter = It.IsAny<int>();
        string dummyResult = It.IsAny<string>();
        mock.Setup(m => m.TryProcess("input", ref dummyCounter, out dummyResult))
            .Returns(true)
            .RefValue(1, 5)
            .OutValue(2, "processed");

        // Act
        int counter = 3;
        var success = mock.TryProcess("input", ref counter, out string result);

        // Assert
        Assert.True(success);
        Assert.Equal(5, counter);
        Assert.Equal("processed", result);
    }

    [Fact]
    public void MixedParameters_AllParameterTypes()
    {
        // Arrange
        var mock = Mock.Create<IMixedService>();
        int dummyRef = It.IsAny<int>(), dummyOut = It.IsAny<int>();
        mock.Setup(m => m.MixedParameters(1, ref dummyRef, out dummyOut, "test"))
            .RefValue(1, 99)
            .OutValue(2, 88);

        // Act
        int refParam = 50;
        mock.MixedParameters(1, ref refParam, out int outParam, "test");

        // Assert
        Assert.Equal(99, refParam);
        Assert.Equal(88, outParam);
    }

    [Fact]
    public void OutParameter_Verification_TracksCall()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(m => m.TryParse("42", out dummy))
            .Returns(true)
            .OutValue(1, 42);

        // Act
        mock.TryParse("42", out int result);

        // Assert - just verify the call was made
        // Verification with out parameters works since we track the call
        mock.Verify(m => m.TryParse("42", out dummy), Times.Once());
    }

    [Fact]
    public void OutParameter_WithCallback_CallbackExecutes()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        bool callbackCalled = false;
        int dummy = 0;

        mock.Setup(m => m.TryParse("42", out dummy))
            .Returns(true)
            .Callback(() => callbackCalled = true)
            .OutValue(1, 42);

        // Act
        mock.TryParse("42", out int result);

        // Assert
        Assert.True(callbackCalled);
        Assert.Equal(42, result);
    }

    [Fact]
    public void OutParameter_NoSetup_ReturnsDefault()
    {
        // Arrange
        var mock = Mock.Create<IParser>();

        // Act
        var success = mock.TryParse("42", out int result);

        // Assert
        Assert.False(success);
        Assert.Equal(0, result); // Default int value
    }

    [Fact]
    public void RefParameter_NoSetup_KeepsOriginalValue()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();

        // Act
        int value = 50;
        mock.ModifyValue(ref value);

        // Assert
        Assert.Equal(50, value); // Value unchanged when no setup
    }

    [Fact]
    public void OutParameter_DifferentInputs_DifferentOutputs()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        mock.Setup(m => m.TryParse("1", out dummy))
            .Returns(true)
            .OutValue(1, 1);
        mock.Setup(m => m.TryParse("2", out dummy))
            .Returns(true)
            .OutValue(1, 2);

        // Act
        mock.TryParse("1", out int result1);
        mock.TryParse("2", out int result2);

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(2, result2);
    }

    [Fact]
    public void OutParameter_InSequence_WorksCorrectly()
    {
        // Arrange
        var sequence = new MockSequence();
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        
        mock.Setup(m => m.TryParse("first", out dummy))
            .Returns(true)
            .OutValue(1, 1)
            .InSequence(sequence);
        
        mock.Setup(m => m.TryParse("second", out dummy))
            .Returns(true)
            .OutValue(1, 2)
            .InSequence(sequence);

        // Act
        mock.TryParse("first", out int result1);
        mock.TryParse("second", out int result2);

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(2, result2);
    }

    [Fact]
    public void OutValueFunc_ParsesInput_DynamicValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        
        mock.Setup(m => m.TryParse(It.IsAny<string>(), out dummy))
            .Returns(true)
            .OutValueFunc(1, args => int.Parse((string)args[0]!));

        // Act
        bool success1 = mock.TryParse("42", out int result1);
        bool success2 = mock.TryParse("100", out int result2);
        bool success3 = mock.TryParse("999", out int result3);

        // Assert - First check if setup is being matched at all
        Assert.True(success1);
        Assert.True(success2);
        Assert.True(success3);
        
        // Then check the out values
        Assert.Equal(42, result1);
        Assert.Equal(100, result2);
        Assert.Equal(999, result3);
    }

    [Fact]
    public void RefValueFunc_DoublesInput_DynamicValue()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();
        int dummy = 0;
        
        mock.Setup(m => m.ModifyValue(ref dummy))
            .RefValueFunc(0, args => (int)args[0]! * 2);

        // Act
        int value1 = 5;
        mock.ModifyValue(ref value1);
        
        int value2 = 25;
        mock.ModifyValue(ref value2);
        
        int value3 = 100;
        mock.ModifyValue(ref value3);

        // Assert
        Assert.Equal(10, value1);
        Assert.Equal(50, value2);
        Assert.Equal(200, value3);
    }

    [Fact]
    public void OutValueFunc_WithMatcher_ComputesFromFirstArgument()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        
        mock.Setup(m => m.TryParse(It.Is<string>(s => s.StartsWith("valid")), out dummy))
            .Returns(true)
            .OutValueFunc(1, args => ((string)args[0]!).Length);

        // Act
        mock.TryParse("valid", out int result1);
        mock.TryParse("validInput", out int result2);

        // Assert
        Assert.Equal(5, result1);
        Assert.Equal(10, result2);
    }

    [Fact]
    public void RefValueFunc_WithMatcher_ModifiesBasedOnCondition()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();
        int dummy = 0;
        
        mock.Setup(m => m.ModifyValue(ref dummy))
            .RefValueFunc(0, args => {
                int val = (int)args[0]!;
                return val < 10 ? val * 3 : val + 100;
            });

        // Act
        int value1 = 3;
        mock.ModifyValue(ref value1);
        
        int value2 = 15;
        mock.ModifyValue(ref value2);

        // Assert
        Assert.Equal(9, value1);   // 3 * 3
        Assert.Equal(115, value2); // 15 + 100
    }

    [Fact]
    public void OutValueFunc_MixedWithStaticOutValue_FactoryTakesPrecedence()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        
        mock.Setup(m => m.TryParse(It.IsAny<string>(), out dummy))
            .Returns(true)
            .OutValue(1, 999) // Static value
            .OutValueFunc(1, args => int.Parse((string)args[0]!)); // Factory should override

        // Act
        mock.TryParse("42", out int result);

        // Assert
        Assert.Equal(42, result); // Factory value, not static 999
    }

    [Fact]
    public void OutValueFunc_MultipleOutParameters_DifferentFactories()
    {
        // Arrange
        var mock = Mock.Create<IMultiOutService>();
        int dummy1 = 0;
        string dummy2 = "";
        
        mock.Setup(m => m.GetValues(It.IsAny<int>(), out dummy1, out dummy2))
            .OutValueFunc(1, args => (int)args[0]! * 10)
            .OutValueFunc(2, args => $"value{args[0]}");

        // Act
        mock.GetValues(5, out int intResult, out string strResult);

        // Assert
        Assert.Equal(50, intResult);
        Assert.Equal("value5", strResult);
    }

    [Fact]
    public void OutValueFunc_VoidMethod_WorksCorrectly()
    {
        // Arrange
        var mock = Mock.Create<IVoidOutService>();
        int dummy = 0;
        
        mock.Setup(m => m.ProcessValue(It.IsAny<string>(), out dummy))
            .OutValueFunc(1, args => ((string)args[0]!).Length * 2);

        // Act
        mock.ProcessValue("test", out int result1);
        mock.ProcessValue("longer", out int result2);

        // Assert
        Assert.Equal(8, result1);  // "test".Length * 2 = 8
        Assert.Equal(12, result2); // "longer".Length * 2 = 12
    }

    // Delegate types for CallbackRefOut tests
    public delegate void TryParseCallback(string input, out int result);
    public delegate void ProcessValueCallback(string input, out int result);
    public delegate void ModifyValueCallback(ref int value);

    [Fact]
    public void CallbackRefOut_WithOutParameter_ModifiesValue()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        bool callbackWasCalled = false;
        
        mock.Setup(m => m.TryParse(It.IsAny<string>(), out dummy))
            .Returns(true)
            .CallbackRefOut((TryParseCallback)((string input, out int result) => 
            {
                callbackWasCalled = true;
                result = int.Parse(input) * 10;
            }));

        // Act
        mock.TryParse("5", out int result1);
        mock.TryParse("7", out int result2);

        // Assert
        Assert.True(callbackWasCalled, "Callback should have been called");
        Assert.Equal(50, result1);
        Assert.Equal(70, result2);
    }

    [Fact]
    public void CallbackRefOut_WithRefParameter_ModifiesValue()
    {
        // Arrange
        var mock = Mock.Create<IRefService>();
        int dummy = 0;
        
        mock.Setup(m => m.ModifyValue(ref dummy))
            .CallbackRefOut((ModifyValueCallback)((ref int value) => 
            {
                value = value + 100;
            }));

        // Act
        int value1 = 5;
        mock.ModifyValue(ref value1);
        
        int value2 = 25;
        mock.ModifyValue(ref value2);

        // Assert
        Assert.Equal(105, value1);
        Assert.Equal(125, value2);
    }

    [Fact]
    public void CallbackRefOut_VoidMethod_WithOutParameter()
    {
        // Arrange
        var mock = Mock.Create<IVoidOutService>();
        int dummy = 0;
        
        mock.Setup(m => m.ProcessValue(It.IsAny<string>(), out dummy))
            .CallbackRefOut((ProcessValueCallback)((string input, out int result) => 
            {
                result = input.ToUpper().Length;
            }));

        // Act
        mock.ProcessValue("hello", out int result1);
        mock.ProcessValue("world", out int result2);

        // Assert
        Assert.Equal(5, result1);
        Assert.Equal(5, result2);
    }

    [Fact]
    public void CallbackRefOut_CanCombineWithReturns()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        bool wasCalled = false;
        
        mock.Setup(m => m.TryParse("42", out dummy))
            .Returns(true)
            .CallbackRefOut((TryParseCallback)((string input, out int result) => 
            {
                wasCalled = true;
                result = 999;
            }));

        // Act
        bool success = mock.TryParse("42", out int result);

        // Assert
        Assert.True(success);
        Assert.True(wasCalled);
        Assert.Equal(999, result);
    }

    [Fact]
    public void CallbackRefOut_WithOutValueFunc_CallbackTakesPrecedence()
    {
        // Arrange
        var mock = Mock.Create<IParser>();
        int dummy = 0;
        
        mock.Setup(m => m.TryParse(It.IsAny<string>(), out dummy))
            .Returns(true)
            .OutValueFunc(1, args => 111)  // This should be overridden
            .CallbackRefOut((TryParseCallback)((string input, out int result) => 
            {
                result = 222;  // Callback takes precedence
            }));

        // Act
        mock.TryParse("42", out int result);

        // Assert
        Assert.Equal(222, result);  // Callback value, not OutValueFunc
    }
}

