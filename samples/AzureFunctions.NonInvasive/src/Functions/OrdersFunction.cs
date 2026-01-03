using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrdersApi.Models;
using OrdersApi.Services;
using System.Net;

namespace OrdersApi.Functions;

public class OrdersFunction
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrdersFunction> _logger;

    public OrdersFunction(
        IOrderService orderService,
        ICustomerService customerService,
        INotificationService notificationService,
        ILogger<OrdersFunction> logger)
    {
        _orderService = orderService;
        _customerService = customerService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [Function("GetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        _logger.LogInformation("Getting order {OrderId}", orderId);

        var order = await _orderService.GetOrderByIdAsync(orderId);
        
        if (order == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(order);
        return response;
    }

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new order");

        var order = await req.ReadFromJsonAsync<Order>();
        if (order == null)
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            return badRequestResponse;
        }

        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync("Customer not found");
            return notFoundResponse;
        }

        var createdOrder = await _orderService.CreateOrderAsync(order);
        await _notificationService.SendOrderConfirmationAsync(createdOrder.Id, customer.Email);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(createdOrder);
        return response;
    }

    [Function("CancelOrder")]
    public async Task<HttpResponseData> CancelOrder(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        _logger.LogInformation("Cancelling order {OrderId}", orderId);

        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return notFoundResponse;
        }

        await _orderService.CancelOrderAsync(orderId);

        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer != null)
        {
            await _notificationService.SendOrderCancellationAsync(orderId, customer.Email);
        }

        var response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
