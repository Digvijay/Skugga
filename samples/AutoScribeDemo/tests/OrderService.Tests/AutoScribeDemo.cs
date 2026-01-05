namespace OrderService.Tests;

using Xunit;
using Xunit.Abstractions;
using Skugga.Core;
using OrderService.Models;
using OrderService.Services;

/// <summary>
/// This shows how AutoScribe helps generate test setup code by recording real service calls.
/// 
/// Run this test to see AutoScribe generate complete [Fact] test methods with proper
/// Arrange/Act/Assert structure that you can copy-paste into your test files.
/// </summary>
public class AutoScribeDemo
{
    private readonly ITestOutputHelper _output;

    public AutoScribeDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Demo_AutoScribe_GeneratesTestSetupCode()
    {
        _output.WriteLine("");
        _output.WriteLine("================================================================================");
        _output.WriteLine("AutoScribe Demo - Automatic Test Code Generation");
        _output.WriteLine("================================================================================");
        _output.WriteLine("");
        _output.WriteLine("Scenario: You want to test OrderController but don't want to manually write");
        _output.WriteLine("all the mock.Setup() calls. AutoScribe can help!");
        _output.WriteLine("");

        // Capture console output
        var consoleOutput = new StringWriter();
        var originalConsole = Console.Out;
        Console.SetOut(consoleOutput);

        // Step 1: Create real implementations (or use your actual services with test data)
        var realUserRepo = new InMemoryUserRepository();
        var realInventory = new InMemoryInventoryService();

        // Step 2: Wrap them with AutoScribe recorders
        var userRecorder = AutoScribe.Capture<IUserRepository>(realUserRepo);
        var inventoryRecorder = AutoScribe.Capture<IInventoryService>(realInventory);

        // Step 3: Execute the operations you need for your test
        // AutoScribe records each call automatically
        _output.WriteLine("Step 1: Recording service calls...");
        var user = await userRecorder.GetUserAsync(1);
        var stockCheck = await inventoryRecorder.CheckStockAsync(101, 2);
        await inventoryRecorder.ReserveStockAsync(101, 2);

        // Step 4: Generate complete test methods
        _output.WriteLine("Step 2: Generating test code...");
        #pragma warning disable IL2026 // Dynamic is OK for development tooling
        dynamic userRecDynamic = userRecorder;
        dynamic inventoryRecDynamic = inventoryRecorder;
        userRecDynamic.PrintTestMethod("OrderController_PlaceOrder_UserSetup");
        inventoryRecDynamic.PrintTestMethod("OrderController_PlaceOrder_InventorySetup");
        #pragma warning restore IL2026

        // Restore console
        Console.SetOut(originalConsole);
        var autoScribeOutput = consoleOutput.ToString();

        _output.WriteLine("");
        _output.WriteLine("================================================================================");
        _output.WriteLine("GENERATED TEST CODE (Copy-paste this into your test file):");
        _output.WriteLine("================================================================================");
        _output.WriteLine(autoScribeOutput);
        _output.WriteLine("================================================================================");
        _output.WriteLine("");
        _output.WriteLine("What to do next:");
        _output.WriteLine("1. Copy the Arrange section from above");
        _output.WriteLine("2. Replace the Act section with your actual system under test:");
        _output.WriteLine("   var controller = new OrderController(mockIUserRepository, mockIInventoryService);");
        _output.WriteLine("   var result = await controller.PlaceOrderAsync(1, items);");
        _output.WriteLine("3. Add your Assert section to verify results");
        _output.WriteLine("4. Done! You have a complete unit test with Skugga mocks");
        _output.WriteLine("");
        _output.WriteLine("Benefits:");
        _output.WriteLine("- No manual mock.Setup() writing");
        _output.WriteLine("- Captures actual values from real execution");
        _output.WriteLine("- Generates complete test structure");
        _output.WriteLine("- Compile-time mocks (no reflection, AOT-compatible)");
        _output.WriteLine("");
        _output.WriteLine("================================================================================");
    }

    // Simple in-memory implementations for demo purposes
    private class InMemoryUserRepository : IUserRepository
    {
        public Task<User> GetUserAsync(int userId)
        {
            return Task.FromResult(new User 
            { 
                Id = userId, 
                Name = "John Doe", 
                Email = "john@example.com" 
            });
        }

        public Task<bool> UserExistsAsync(int userId)
        {
            return Task.FromResult(true);
        }
    }

    private class InMemoryInventoryService : IInventoryService
    {
        public Task<bool> CheckStockAsync(int productId, int quantity)
        {
            return Task.FromResult(true);
        }

        public Task ReserveStockAsync(int productId, int quantity)
        {
            return Task.CompletedTask;
        }
    }
}
