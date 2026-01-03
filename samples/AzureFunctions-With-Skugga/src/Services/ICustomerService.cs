using OrdersApi.Models;

namespace OrdersApi.Services;

public interface ICustomerService
{
    Task<Customer?> GetCustomerByIdAsync(string customerId);
}
