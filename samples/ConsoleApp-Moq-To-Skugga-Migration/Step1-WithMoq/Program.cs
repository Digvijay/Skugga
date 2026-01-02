using Step1_WithMoq;
using Step1_WithMoq.Models;
using Step1_WithMoq.Services;

Console.WriteLine("🚀 Order Processing System - Step 1 (With Moq)");
Console.WriteLine("⚠️  This version uses Moq and will NOT work with Native AOT");
Console.WriteLine();

// This is a demonstration app
// Real implementation would have actual service implementations
// Tests demonstrate why Moq fails with AOT

Console.WriteLine("✅ Application structure created");
Console.WriteLine("✅ Services defined (IOrderService, IPaymentService, etc.)");
Console.WriteLine("✅ OrderProcessor business logic implemented");
Console.WriteLine();
Console.WriteLine("📝 Run the tests to see Moq in action:");
Console.WriteLine("   cd Step1-WithMoq.Tests");
Console.WriteLine("   dotnet test");
Console.WriteLine();
Console.WriteLine("💥 Try to enable AOT and watch it fail:");
Console.WriteLine("   Add <PublishAot>true</PublishAot> to .csproj");
Console.WriteLine("   dotnet publish -c Release");
Console.WriteLine();
Console.WriteLine("❌ Expected: AOT compilation errors due to reflection");
