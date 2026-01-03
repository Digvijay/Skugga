using OrdersApi.Models;

namespace OrdersApi.Services;

public class CustomerService : ICustomerService
{
    public Task<Customer?> GetCustomerByIdAsync(string customerId)
    {
        return Task.FromResult<Customer?>(new Customer { Id = customerId, Name = "Test Customer" });
    }
}
