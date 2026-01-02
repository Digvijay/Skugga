using Skugga.Core;

namespace Skugga.Core.Tests;

public class CallbackTests
{
    public interface ITestService
    {
        void Execute();
        void ExecuteWithArgs(int value);
        void Process(string data);
        string GetData();
        int Calculate(int a, int b);
        void MultipleArgs(int a, string b, bool c);
        
        // Methods for testing 4-8 argument callbacks
        int Method4Args(int a, int b, int c, int d);
        string Method5Args(string p1, string p2, string p3, string p4, string p5);
        bool Method6Args(int i1, int i2, int i3, int i4, int i5, int i6);
        void Method7Args(string s1, string s2, string s3, string s4, string s5, string s6, string s7);
        int Method8Args(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8);
    }

    [Fact]
    public void Callback_WithAction_ShouldExecuteOnMethodCall()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackExecuted = false;
        
        mock.Setup(x => x.Execute())
            .Callback(() => callbackExecuted = true);
        
        // Act
        mock.Execute();
        
        // Assert
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public void Callback_ShouldExecuteBeforeReturningValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var executionOrder = new List<string>();
        
        mock.Setup(x => x.GetData())
            .Callback(() => executionOrder.Add("callback"))
            .Returns("data");
        
        // Act
        executionOrder.Add("before");
        var result = mock.GetData();
        executionOrder.Add("after");
        
        // Assert
        executionOrder.Should().Equal("before", "callback", "after");
        result.Should().Be("data");
    }

    [Fact]
    public void Callback_WithSingleArgument_ShouldReceiveArgumentValue()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        int capturedValue = 0;
        
        mock.Setup(x => x.ExecuteWithArgs(42))
            .Callback((int value) => capturedValue = value);
        
        // Act
        mock.ExecuteWithArgs(42);
        
        // Assert
        capturedValue.Should().Be(42);
    }

    [Fact]
    public void Callback_WithTwoArguments_ShouldReceiveBothValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        int capturedA = 0;
        int capturedB = 0;
        
        mock.Setup(x => x.Calculate(10, 20))
            .Callback((int a, int b) =>
            {
                capturedA = a;
                capturedB = b;
            })
            .Returns(0);
        
        // Act
        mock.Calculate(10, 20);
        
        // Assert
        capturedA.Should().Be(10);
        capturedB.Should().Be(20);
    }

    [Fact]
    public void Callback_WithThreeArguments_ShouldReceiveAllValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var captured = new List<object>();
        
        mock.Setup(x => x.MultipleArgs(42, "test", true))
            .Callback((int a, string b, bool c) =>
            {
                captured.Add(a);
                captured.Add(b);
                captured.Add(c);
            });
        
        // Act
        mock.MultipleArgs(42, "test", true);
        
        // Assert
        captured.Should().Equal(42, "test", true);
    }

    [Fact]
    public void Callback_CalledMultipleTimes_ShouldExecuteEachTime()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        int callCount = 0;
        
        mock.Setup(x => x.Execute())
            .Callback(() => callCount++);
        
        // Act
        mock.Execute();
        mock.Execute();
        mock.Execute();
        
        // Assert
        callCount.Should().Be(3);
    }

    [Fact]
    public void Callback_WithReturns_ShouldChainFluently()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackExecuted = false;
        
        mock.Setup(x => x.GetData())
            .Callback(() => callbackExecuted = true)
            .Returns("test");
        
        // Act
        var result = mock.GetData();
        
        // Assert
        callbackExecuted.Should().BeTrue();
        result.Should().Be("test");
    }

    [Fact]
    public void Callback_ReturnsFirst_ThenCallback_ShouldWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackExecuted = false;
        
        mock.Setup(x => x.GetData())
            .Returns("test")
            .Callback(() => callbackExecuted = true);
        
        // Act
        var result = mock.GetData();
        
        // Assert
        callbackExecuted.Should().BeTrue();
        result.Should().Be("test");
    }

    [Fact]
    public void Callback_WithException_ShouldThrowException()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.Execute())
            .Callback(() => throw new InvalidOperationException("Test exception"));
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => mock.Execute());
        exception.Message.Should().Be("Test exception");
    }

    [Fact]
    public void Callback_WithArgumentValidation_ShouldValidateArguments()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        
        mock.Setup(x => x.ExecuteWithArgs(10))
            .Callback((int value) =>
            {
                if (value < 0)
                    throw new ArgumentException("Value must be positive");
            });
        
        // Act & Assert - valid value should work
        mock.ExecuteWithArgs(10); // Should not throw
    }

    [Fact]
    public void Callback_MultipleCallbacks_OnDifferentMethods_ShouldExecuteIndependently()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callback1Executed = false;
        var callback2Executed = false;
        
        mock.Setup(x => x.Execute())
            .Callback(() => callback1Executed = true);
            
        mock.Setup(x => x.Process("data"))
            .Callback(() => callback2Executed = true);
        
        // Act
        mock.Execute();
        
        // Assert
        callback1Executed.Should().BeTrue();
        callback2Executed.Should().BeFalse();
        
        // Act
        mock.Process("data");
        
        // Assert
        callback2Executed.Should().BeTrue();
    }

    [Fact]
    public void Callback_WithSideEffects_ShouldModifyExternalState()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var log = new List<string>();
        
        // Setup each specific call
        mock.Setup(x => x.Process("item1"))
            .Callback((string data) => log.Add($"Processing: {data}"));
        mock.Setup(x => x.Process("item2"))
            .Callback((string data) => log.Add($"Processing: {data}"));
        mock.Setup(x => x.Process("item3"))
            .Callback((string data) => log.Add($"Processing: {data}"));
        
        // Act
        mock.Process("item1");
        mock.Process("item2");
        mock.Process("item3");
        
        // Assert
        log.Should().Equal("Processing: item1", "Processing: item2", "Processing: item3");
    }

    [Fact]
    public void Callback_WithComplexLogic_ShouldExecuteCorrectly()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var sumOfValues = 0;
        var callCount = 0;
        
        // Setup each specific call
        mock.Setup(x => x.ExecuteWithArgs(10))
            .Callback((int value) => { callCount++; sumOfValues += value; });
        mock.Setup(x => x.ExecuteWithArgs(20))
            .Callback((int value) => { callCount++; sumOfValues += value; });
        mock.Setup(x => x.ExecuteWithArgs(30))
            .Callback((int value) => { callCount++; sumOfValues += value; });
        
        // Act
        mock.ExecuteWithArgs(10);
        mock.ExecuteWithArgs(20);
        mock.ExecuteWithArgs(30);
        
        // Assert
        callCount.Should().Be(3);
        sumOfValues.Should().Be(60);
    }

    [Fact]
    public void Callback_WithVerify_ShouldBothWork()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackExecuted = false;
        
        mock.Setup(x => x.Execute())
            .Callback(() => callbackExecuted = true);
        
        // Act
        mock.Execute();
        mock.Execute();
        
        // Assert
        callbackExecuted.Should().BeTrue();
        mock.Verify(x => x.Execute(), Times.Exactly(2));
    }

    [Fact]
    public void Callback_WithStrictMock_ShouldStillExecute()
    {
        // Arrange
        var mock = Mock.Create<ITestService>(MockBehavior.Strict);
        var callbackExecuted = false;
        
        mock.Setup(x => x.Execute())
            .Callback(() => callbackExecuted = true);
        
        // Act
        mock.Execute();
        
        // Assert
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public void Callback_OnMethodWithoutSetup_ShouldNotExecute()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var callbackExecuted = false;
        
        mock.Setup(x => x.Execute())
            .Callback(() => callbackExecuted = true);
        
        // Act - call a different method
        mock.ExecuteWithArgs(5);
        
        // Assert - callback should NOT execute
        callbackExecuted.Should().BeFalse();
    }
    
    // Tests for typed callbacks with 4-8 arguments
    
    [Fact]
    public void Callback_WithFourArguments_ShouldReceiveAllValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        int captured1 = 0, captured2 = 0, captured3 = 0, captured4 = 0;
        
        mock.Setup(m => m.Method4Args(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int a, int b, int c, int d) =>
            {
                captured1 = a;
                captured2 = b;
                captured3 = c;
                captured4 = d;
            })
            .Returns(100);
        
        // Act
        mock.Method4Args(10, 20, 30, 40);
        
        // Assert
        captured1.Should().Be(10);
        captured2.Should().Be(20);
        captured3.Should().Be(30);
        captured4.Should().Be(40);
    }
    
    [Fact]
    public void Callback_WithFiveArguments_ShouldReceiveAllValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var captured = new string[5];
        
        mock.Setup(m => m.Method5Args(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                                       It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string p1, string p2, string p3, string p4, string p5) =>
            {
                captured[0] = p1;
                captured[1] = p2;
                captured[2] = p3;
                captured[3] = p4;
                captured[4] = p5;
            })
            .Returns("result");
        
        // Act
        mock.Method5Args("a", "b", "c", "d", "e");
        
        // Assert
        captured[0].Should().Be("a");
        captured[1].Should().Be("b");
        captured[2].Should().Be("c");
        captured[3].Should().Be("d");
        captured[4].Should().Be("e");
    }
    
    [Fact]
    public void Callback_WithSixArguments_ShouldReceiveAllValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var captured = new int[6];
        
        mock.Setup(m => m.Method6Args(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 
                                       It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int i1, int i2, int i3, int i4, int i5, int i6) =>
            {
                captured[0] = i1;
                captured[1] = i2;
                captured[2] = i3;
                captured[3] = i4;
                captured[4] = i5;
                captured[5] = i6;
            })
            .Returns(true);
        
        // Act
        mock.Method6Args(1, 2, 3, 4, 5, 6);
        
        // Assert
        captured[0].Should().Be(1);
        captured[1].Should().Be(2);
        captured[2].Should().Be(3);
        captured[3].Should().Be(4);
        captured[4].Should().Be(5);
        captured[5].Should().Be(6);
    }
    
    [Fact]
    public void Callback_WithSevenArguments_ShouldReceiveAllValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var captured = new string[7];
        
        mock.Setup(m => m.Method7Args(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                                       It.IsAny<string>()))
            .Callback((string s1, string s2, string s3, string s4, string s5, string s6, string s7) =>
                {
                    captured[0] = s1;
                    captured[1] = s2;
                    captured[2] = s3;
                    captured[3] = s4;
                    captured[4] = s5;
                    captured[5] = s6;
                    captured[6] = s7;
                });
        
        // Act
        mock.Method7Args("a", "b", "c", "d", "e", "f", "g");
        
        // Assert
        captured[0].Should().Be("a");
        captured[1].Should().Be("b");
        captured[2].Should().Be("c");
        captured[3].Should().Be("d");
        captured[4].Should().Be("e");
        captured[5].Should().Be("f");
        captured[6].Should().Be("g");
    }
    
    [Fact]
    public void Callback_WithEightArguments_ShouldReceiveAllValues()
    {
        // Arrange
        var mock = Mock.Create<ITestService>();
        var captured = new int[8];
        
        mock.Setup(m => m.Method8Args(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 
                                       It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 
                                       It.IsAny<int>(), It.IsAny<int>()))
            .Callback((int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) =>
                {
                    captured[0] = a1;
                    captured[1] = a2;
                    captured[2] = a3;
                    captured[3] = a4;
                    captured[4] = a5;
                    captured[5] = a6;
                    captured[6] = a7;
                    captured[7] = a8;
                })
            .Returns(999);
        
        // Act
        mock.Method8Args(10, 20, 30, 40, 50, 60, 70, 80);
        
        // Assert
        captured[0].Should().Be(10);
        captured[1].Should().Be(20);
        captured[2].Should().Be(30);
        captured[3].Should().Be(40);
        captured[4].Should().Be(50);
        captured[5].Should().Be(60);
        captured[6].Should().Be(70);
        captured[7].Should().Be(80);
    }
}
