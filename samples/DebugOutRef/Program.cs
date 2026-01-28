using Skugga.Core;

namespace DebugOutRef;

public interface IParser
{
    bool TryParse(string input, out int result);
    bool TryParseDouble(string input, out double result);
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Testing Out Parameter Support ===\n");

        // Test 1: int (FAILS in unit tests)
        Console.WriteLine("Test 1: TryParse with out int");
        var mock1 = Mock.Create<IParser>();
        int dummy1 = 0;
        mock1.Setup(m => m.TryParse("42", out dummy1))
            .Returns(true)
            .OutValue(1, 42);
        
        var success1 = mock1.TryParse("42", out int result1);
        Console.WriteLine($"  Success: {success1}");
        Console.WriteLine($"  Result: {result1} (expected: 42)");
        Console.WriteLine($"  Status: {(result1 == 42 ? "✅ PASS" : "❌ FAIL")}\n");

        // Test 2: double (PASSES in unit tests)
        Console.WriteLine("Test 2: TryParseDouble with out double");
        var mock2 = Mock.Create<IParser>();
        double dummy2 = 0.0;
        mock2.Setup(m => m.TryParseDouble("3.14", out dummy2))
            .Returns(true)
            .OutValue(1, 3.14);
        
        var success2 = mock2.TryParseDouble("3.14", out double result2);
        Console.WriteLine($"  Success: {success2}");
        Console.WriteLine($"  Result: {result2} (expected: 3.14)");
        Console.WriteLine($"  Status: {(Math.Abs(result2 - 3.14) < 0.001 ? "✅ PASS" : "❌ FAIL")}\n");

        Console.WriteLine("=== Tests Complete ===");
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
