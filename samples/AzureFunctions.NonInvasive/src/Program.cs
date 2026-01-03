using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrdersApi.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Register services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<INotificationService, NotificationService>();
    })
    .Build();

await host.RunAsync();
