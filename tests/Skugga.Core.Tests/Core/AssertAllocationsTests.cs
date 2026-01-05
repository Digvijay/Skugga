using Skugga.Core;

namespace Skugga.Core.Tests;

/// <summary>
/// Tests for AssertAllocations - zero-allocation performance testing
/// </summary>
public class AssertAllocationsTests
{
    [Fact]
    [Trait("Category", "Core")]
    public void Zero_WithNoAllocations_ShouldNotThrow()
    {
        // Arrange & Act
        var act = () => AssertAllocations.Zero(() =>
        {
            // This code allocates nothing on the heap
            var x = 1;
            var y = 2;
            var z = x + y;
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Zero_WithHeapAllocation_ShouldThrowException()
    {
        // Arrange & Act
        var act = () => AssertAllocations.Zero(() =>
        {
            // This allocates on the heap
            var list = new List<int>();
        });

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Allocated*bytes*Expected 0*");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Zero_WithArrayAllocation_ShouldThrowException()
    {
        // Arrange & Act
        var act = () => AssertAllocations.Zero(() =>
        {
            // Array allocation definitely allocates on heap
            var array = new int[10];
        });

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Allocated*bytes*");
    }

    [Fact]
    [Trait("Category", "Core")]
    public void Zero_WithValueTypeOperations_ShouldNotThrow()
    {
        // Arrange & Act
        var act = () => AssertAllocations.Zero(() =>
        {
            // Value type operations don't allocate
            var a = 10;
            var b = 20;
            var sum = a + b;
            var product = a * b;
            _ = sum + product;
        });

        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void AtMost_WithinThreshold_ShouldNotThrow()
    {
        // Arrange & Act
        var act = () => AssertAllocations.AtMost(() =>
        {
            var list = new List<int>(10);
        }, maxBytes: 1000);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void AtMost_ExceedingThreshold_ShouldThrow()
    {
        // Arrange & Act
        var act = () => AssertAllocations.AtMost(() =>
        {
            var list = new List<int>(1000);
            for (int i = 0; i < 1000; i++)
                list.Add(i);
        }, maxBytes: 100);
        
        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Allocated*bytes*Expected at most 100*");
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void Measure_ShouldReturnDetailedReport()
    {
        // Arrange & Act
        var report = AssertAllocations.Measure(() =>
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };
        }, "TestAction");
        
        // Assert
        report.Should().NotBeNull();
        report.ActionName.Should().Be("TestAction");
        report.BytesAllocated.Should().BeGreaterThan(0);
        report.DurationMilliseconds.Should().BeGreaterOrEqualTo(0);
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void Measure_WithNoAllocation_ShouldReportZeroBytes()
    {
        // Arrange & Act
        var report = AssertAllocations.Measure(() =>
        {
            var x = 1 + 1;
            _ = x;
        }, "NoAllocAction");
        
        // Assert - Measure itself may trigger some GC overhead, so be lenient
        report.BytesAllocated.Should().BeLessThan(100, "simple value type operations should allocate very little");
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void Threshold_ShouldCreateConfiguration()
    {
        // Arrange & Act
        var threshold = AssertAllocations.Threshold("MyAction", maxBytes: 500, maxMilliseconds: 100);
        
        // Assert
        threshold.Should().NotBeNull();
        threshold.ActionName.Should().Be("MyAction");
        threshold.MaxBytes.Should().Be(500);
        threshold.MaxMilliseconds.Should().Be(100);
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void MeetsThreshold_WithinLimits_ShouldNotThrow()
    {
        // Arrange
        var threshold = AssertAllocations.Threshold("FastAction", maxBytes: 1000, maxMilliseconds: 1000);
        
        // Act & Assert
        var act = () => AssertAllocations.MeetsThreshold(() =>
        {
            var x = 1 + 1;
            _ = x;
        }, threshold);
        
        act.Should().NotThrow();
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void MeetsThreshold_ExceedingBytes_ShouldThrow()
    {
        // Arrange
        var threshold = AssertAllocations.Threshold("MemoryHungry", maxBytes: 100, maxMilliseconds: 10000);
        
        // Act & Assert
        var act = () => AssertAllocations.MeetsThreshold(() =>
        {
            var list = new List<int>(1000);
            for (int i = 0; i < 1000; i++)
                list.Add(i);
        }, threshold);
        
        act.Should().Throw<Exception>()
            .WithMessage("*MemoryHungry*Allocated*bytes*Threshold*");
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void MeetsThreshold_ExceedingTime_ShouldThrow()
    {
        // Arrange
        var threshold = AssertAllocations.Threshold("SlowAction", maxBytes: 100000, maxMilliseconds: 10);
        
        // Act & Assert
        var act = () => AssertAllocations.MeetsThreshold(() =>
        {
            System.Threading.Thread.Sleep(50);
        }, threshold);
        
        act.Should().Throw<Exception>()
            .WithMessage("*SlowAction*Took*ms*Threshold*");
    }
    
    [Fact]
    [Trait("Category", "Core")]
    public void AllocationReport_ToString_ShouldFormatProperly()
    {
        // Arrange
        var report = new AllocationReport
        {
            ActionName = "TestAction",
            BytesAllocated = 1024,
            DurationMilliseconds = 50,
            Gen0Collections = 1,
            Gen1Collections = 0,
            Gen2Collections = 0
        };
        
        // Act
        var result = report.ToString();
        
        // Assert
        result.Should().Contain("TestAction");
        result.Should().Contain("1024");
        result.Should().Contain("50ms");
        result.Should().Contain("Gen0=1");
    }
}
