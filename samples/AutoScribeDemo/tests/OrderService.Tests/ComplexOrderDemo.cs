namespace OrderService.Tests;

using Xunit;
using Xunit.Abstractions;
using Skugga.Core;
using OrderService.Models;
using OrderService.Services;

/// <summary>
/// This demonstrates the REAL power of AutoScribe:
/// Testing a complex controller with 9 dependencies without writing tons of setup code!
/// </summary>
public class ComplexOrderDemo
{
    private readonly ITestOutputHelper _output;

    public ComplexOrderDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ManualWay_WritingAllTheSetupByHand_TakesForever()
    {
        _output.WriteLine("");
        _output.WriteLine("================================================================================");
        _output.WriteLine("THE OLD WAY: Writing test setup manually");
        _output.WriteLine("================================================================================");
        _output.WriteLine("");
        _output.WriteLine("For a controller with 9 dependencies, you need to:");
        _output.WriteLine("1. Create 9 mocks");
        _output.WriteLine("2. Write 15+ Setup() calls");
        _output.WriteLine("3. Remember all the parameter values");
        _output.WriteLine("4. Hope you didn't miss any dependencies");
        _output.WriteLine("");
        _output.WriteLine("This can take 10-15 minutes per test!");
        _output.WriteLine("");

        // Arrange - Look at all this manual setup! 😱
        var mockUserRepo = Mock.Create<IUserRepository>();
        var mockInventory = Mock.Create<IInventoryService>();
        var mockPayment = Mock.Create<IPaymentGateway>();
        var mockShipping = Mock.Create<IShippingService>();
        var mockEmail = Mock.Create<IEmailService>();
        var mockTax = Mock.Create<ITaxCalculator>();
        var mockDiscount = Mock.Create<IDiscountService>();
        var mockAudit = Mock.Create<IAuditLogger>();
        var mockNotifications = Mock.Create<INotificationService>();

        // And now all the setups...
        mockUserRepo.Setup(x => x.GetUserAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "John", Email = "john@example.com", State = "CA", Address = "123 Main", PaymentMethod = "card_123" });
        
        mockInventory.Setup(x => x.CheckStockAsync(101, 2)).ReturnsAsync(true);
        mockInventory.Setup(x => x.ReserveStockAsync(101, 2)).Returns(Task.CompletedTask);
        
        mockDiscount.Setup(x => x.CalculateDiscountAsync("SAVE10", 59.98m)).ReturnsAsync(6.00m);
        mockTax.Setup(x => x.CalculateTaxAsync(53.98m, "CA")).ReturnsAsync(4.32m);
        mockShipping.Setup(x => x.GetShippingCostAsync("123 Main", 2)).ReturnsAsync(5.99m);
        mockPayment.Setup(x => x.ChargeAsync("card_123", 64.29m)).ReturnsAsync(true);
        
        mockEmail.Setup(x => x.SendOrderConfirmationAsync("john@example.com", 64.29m)).Returns(Task.CompletedTask);
        mockNotifications.Setup(x => x.SendPushNotificationAsync(1, "Order confirmed!")).Returns(Task.CompletedTask);
        mockAudit.Setup(x => x.LogOrderCreatedAsync(1, 64.29m)).Returns(Task.CompletedTask);

        var items = new List<OrderItem> { new() { ProductId = 101, Quantity = 2, Price = 29.99m } };

        // Act
        var controller = new ComplexOrderController(
            mockUserRepo, mockInventory, mockPayment, mockShipping,
            mockEmail, mockTax, mockDiscount, mockAudit, mockNotifications);
        
        var order = await controller.ProcessPremiumOrderAsync(1, items, "SAVE10");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(1, order.UserId);

        _output.WriteLine("✓ Test passes, but that was exhausting to write!");
        _output.WriteLine("");
    }

    [Fact]
    public async Task AutoScribeWay_RecordRealBehavior_GeneratesEverything()
    {
        _output.WriteLine("");
        _output.WriteLine("================================================================================");
        _output.WriteLine("THE AUTOSCRIBE WAY: Let it write the setup for you!");
        _output.WriteLine("================================================================================");
        _output.WriteLine("");
        _output.WriteLine("Step 1: Run your code with real/stub implementations");
        _output.WriteLine("Step 2: AutoScribe records everything");
        _output.WriteLine("Step 3: Copy-paste the generated test code");
        _output.WriteLine("");
        _output.WriteLine("Time saved: 10+ minutes per test! ⚡");
        _output.WriteLine("");

        // Capture console for AutoScribe output
        var consoleOutput = new StringWriter();
        var originalConsole = Console.Out;
        Console.SetOut(consoleOutput);

        // Create simple stub implementations that return realistic values
        var userRepo = AutoScribe.Capture<IUserRepository>(new StubUserRepo());
        var inventory = AutoScribe.Capture<IInventoryService>(new StubInventory());
        var payment = AutoScribe.Capture<IPaymentGateway>(new StubPayment());
        var shipping = AutoScribe.Capture<IShippingService>(new StubShipping());
        var email = AutoScribe.Capture<IEmailService>(new StubEmail());
        var tax = AutoScribe.Capture<ITaxCalculator>(new StubTax());
        var discount = AutoScribe.Capture<IDiscountService>(new StubDiscount());
        var audit = AutoScribe.Capture<IAuditLogger>(new StubAudit());
        var notifications = AutoScribe.Capture<INotificationService>(new StubNotifications());

        // Just run your actual code - AutoScribe records it all!
        var items = new List<OrderItem> { new() { ProductId = 101, Quantity = 2, Price = 29.99m } };
        var controller = new ComplexOrderController(
            userRepo, inventory, payment, shipping, email, tax, discount, audit, notifications);
        
        var order = await controller.ProcessPremiumOrderAsync(1, items, "SAVE10");

        // Now generate the complete test!
        #pragma warning disable IL2026
        ((dynamic)userRepo).PrintTestMethod("ProcessOrder_HappyPath_Part1");
        ((dynamic)inventory).PrintTestMethod("ProcessOrder_HappyPath_Part2");
        ((dynamic)discount).PrintTestMethod("ProcessOrder_HappyPath_Part3");
        ((dynamic)tax).PrintTestMethod("ProcessOrder_HappyPath_Part4");
        ((dynamic)shipping).PrintTestMethod("ProcessOrder_HappyPath_Part5");
        ((dynamic)payment).PrintTestMethod("ProcessOrder_HappyPath_Part6");
        ((dynamic)email).PrintTestMethod("ProcessOrder_HappyPath_Part7");
        ((dynamic)notifications).PrintTestMethod("ProcessOrder_HappyPath_Part8");
        ((dynamic)audit).PrintTestMethod("ProcessOrder_HappyPath_Part9");
        #pragma warning restore IL2026

        Console.SetOut(originalConsole);
        var generated = consoleOutput.ToString();

        _output.WriteLine("================================================================================");
        _output.WriteLine("GENERATED TEST CODE - Just copy and paste!");
        _output.WriteLine("================================================================================");
        _output.WriteLine(generated);
        _output.WriteLine("================================================================================");
        _output.WriteLine("");
        _output.WriteLine("That's it! All your mock setups are ready to use.");
        _output.WriteLine("Just combine them, add your Act/Assert, and you're done!");
        _output.WriteLine("");
    }

    // Simple stubs that return realistic test data
    private class StubUserRepo : IUserRepository
    {
        public Task<User> GetUserAsync(int userId) => 
            Task.FromResult(new User { Id = userId, Name = "John Doe", Email = "john@example.com", State = "CA", Address = "123 Main St", PaymentMethod = "card_123" });
        public Task<bool> UserExistsAsync(int userId) => Task.FromResult(true);
    }

    private class StubInventory : IInventoryService
    {
        public Task<bool> CheckStockAsync(int productId, int quantity) => Task.FromResult(true);
        public Task ReserveStockAsync(int productId, int quantity) => Task.CompletedTask;
    }

    private class StubPayment : IPaymentGateway
    {
        public Task<bool> ChargeAsync(string paymentMethod, decimal amount) => Task.FromResult(true);
    }

    private class StubShipping : IShippingService
    {
        public Task<decimal> GetShippingCostAsync(string address, int totalItems) => Task.FromResult(5.99m);
    }

    private class StubEmail : IEmailService
    {
        public Task SendOrderConfirmationAsync(string email, decimal amount) => Task.CompletedTask;
    }

    private class StubTax : ITaxCalculator
    {
        public Task<decimal> CalculateTaxAsync(decimal amount, string state) => Task.FromResult(amount * 0.08m);
    }

    private class StubDiscount : IDiscountService
    {
        public Task<decimal> CalculateDiscountAsync(string promoCode, decimal subtotal) => 
            Task.FromResult(promoCode == "SAVE10" ? subtotal * 0.10m : 0m);
    }

    private class StubAudit : IAuditLogger
    {
        public Task LogOrderCreatedAsync(int userId, decimal amount) => Task.CompletedTask;
    }

    private class StubNotifications : INotificationService
    {
        public Task SendPushNotificationAsync(int userId, string message) => Task.CompletedTask;
    }
}
