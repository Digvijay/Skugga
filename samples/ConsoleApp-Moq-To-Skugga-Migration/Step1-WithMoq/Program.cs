using Moq;
using Step1_WithMoq;
using Step1_WithMoq.Models;
using Step1_WithMoq.Services;

Console.WriteLine("🚀 Order Processing System - Step 1 (With Moq)");
Console.WriteLine("⚠️  This version uses Moq with Native AOT ENABLED");
Console.WriteLine();

try
{
    Console.WriteLine("Attempting to create a mock with Moq under AOT...");
    var mock = new Mock<IOrderService>();
    mock.Setup(x => x.GetPrice(100)).Returns(50.0m);
    
    var price = mock.Object.GetPrice(100);
    Console.WriteLine($"✅ Mock created successfully! Price: ${price}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ FAILED: {ex.GetType().Name}");
    Console.WriteLine($"   Message: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("💡 This is expected! Moq uses reflection which is incompatible with Native AOT.");
    Console.WriteLine("   See Step2-WithSkugga for the working solution!");
}
