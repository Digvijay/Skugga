using System.Net;
using AzureFunctions.Models;
using AzureFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctions;

public class OrderFunctions
{
    private readonly ILogger<OrderFunctions> _logger;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notificationService;

    public OrderFunctions(
        ILogger<OrderFunctions> logger,
        IOrderService orderService,
        IPaymentService paymentService,
        INotificationService notificationService)
    {
        _logger = logger;
        _orderService = orderService;
        _paymentService = paymentService;
        _notificationService = notificationService;
    }

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new order");

        var customerId = req.Query["customerId"] ?? "default-customer";
        var amountStr = req.Query["amount"];
        
        if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid amount");
            return badResponse;
        }

        var order = await _orderService.CreateOrderAsync(customerId, amount);
        
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(order);
        return response;
    }

    [Function("ProcessPayment")]
    public async Task<HttpResponseData> ProcessPayment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/{orderId}/payment")] HttpRequestData req,
        string orderId)
    {
        _logger.LogInformation($"Processing payment for order {orderId}");

        var order = await _orderService.GetOrderAsync(orderId);
        if (order == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync("Order not found");
            return notFoundResponse;
        }

        // Update order status
        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.PaymentProcessing);

        // Process payment
        var paymentSuccess = await _paymentService.ProcessPaymentAsync(orderId, order.TotalAmount);

        if (paymentSuccess)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Confirmed);
            await _notificationService.SendOrderConfirmationAsync(order.CustomerId, orderId);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Payment processed successfully");
            return response;
        }
        else
        {
            await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Failed);
            await _notificationService.SendPaymentFailureNotificationAsync(order.CustomerId, orderId);
            
            var failureResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await failureResponse.WriteStringAsync("Payment processing failed");
            return failureResponse;
        }
    }

    [Function("CancelOrder")]
    public async Task<HttpResponseData> CancelOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/{orderId}/cancel")] HttpRequestData req,
        string orderId)
    {
        _logger.LogInformation($"Cancelling order {orderId}");

        var order = await _orderService.GetOrderAsync(orderId);
        if (order == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return notFoundResponse;
        }

        var cancelled = await _orderService.CancelOrderAsync(orderId);
        if (cancelled)
        {
            await _notificationService.SendCancellationNotificationAsync(order.CustomerId, orderId);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Order cancelled");
            return response;
        }
        
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Cannot cancel order");
        return badResponse;
    }
}
