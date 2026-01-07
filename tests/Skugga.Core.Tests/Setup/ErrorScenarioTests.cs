using System;
using System.Linq;
using Skugga.Core.Exceptions;
using Xunit;

namespace Skugga.Core.Tests.Setup
{
    /// <summary>
    /// Tests for Error Scenario Testing.
    /// Validates HTTP exception types and error scenario extension methods for testing error handling.
    /// </summary>
    [Trait("Category", "Error Scenarios")]
    public class ErrorScenarioTests
    {
        public interface IPaymentGateway
        {
            PaymentResult ProcessPayment(Payment payment);
            void ValidateAccount(string accountId);
            Invoice GetInvoice(string invoiceId);
            User GetUser(int userId);
        }

        public class Payment { public decimal Amount { get; set; } public string Currency { get; set; } = "USD"; }
        public class PaymentResult { public string TransactionId { get; set; } = ""; public bool Success { get; set; } }
        public class Invoice { public string Id { get; set; } = ""; public decimal Amount { get; set; } }
        public class User { public int Id { get; set; } public string Name { get; set; } = ""; }

        #region HTTP Exception Types Tests

        [Fact]
        public void HttpStatusException_HasStatusCode()
        {
            var exception = new HttpStatusException(404, "Not found");

            Assert.Equal(404, exception.StatusCode);
            Assert.Equal("Not found", exception.Message);
            Assert.Null(exception.ErrorBody);
        }

        [Fact]
        public void HttpStatusException_CanHaveErrorBody()
        {
            var errorBody = new { Code = "RESOURCE_NOT_FOUND", Detail = "User not found" };
            var exception = new HttpStatusException(404, "Not found", errorBody);

            Assert.Equal(404, exception.StatusCode);
            Assert.Equal(errorBody, exception.ErrorBody);
        }

        [Fact]
        public void HttpStatusException_CanHaveHeaders()
        {
            var headers = new System.Collections.Generic.Dictionary<string, string>
            {
                { "X-Request-Id", "abc123" },
                { "X-Error-Code", "AUTH_FAILED" }
            };
            var exception = new HttpStatusException(401, "Unauthorized", null, headers);

            Assert.Equal(401, exception.StatusCode);
            Assert.NotNull(exception.Headers);
            Assert.Equal(2, exception.Headers.Count);
            Assert.Equal("abc123", exception.Headers["X-Request-Id"]);
        }

        [Fact]
        public void BadRequestException_HasStatusCode400()
        {
            var exception = new BadRequestException("Invalid request");

            Assert.Equal(400, exception.StatusCode);
            Assert.Equal("Invalid request", exception.Message);
        }

        [Fact]
        public void BadRequestException_CanHaveValidationErrors()
        {
            var errors = new[]
            {
                new ValidationError("amount", "Must be positive"),
                new ValidationError("currency", "Invalid currency code", "INVALID_FORMAT")
            };
            var exception = new BadRequestException("Validation failed", errors);

            Assert.Equal(400, exception.StatusCode);
            Assert.NotNull(exception.ValidationErrors);
            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Equal("amount", exception.ValidationErrors[0].Field);
            Assert.Equal("Must be positive", exception.ValidationErrors[0].Message);
        }

        [Fact]
        public void UnauthorizedException_HasStatusCode401()
        {
            var exception = new UnauthorizedException("Invalid token");

            Assert.Equal(401, exception.StatusCode);
            Assert.Equal("Invalid token", exception.Message);
        }

        [Fact]
        public void UnauthorizedException_CanHaveAuthenticateHeader()
        {
            var exception = new UnauthorizedException("Token expired", "Bearer realm=\"api\"");

            Assert.Equal(401, exception.StatusCode);
            Assert.NotNull(exception.Headers);
            Assert.Contains("WWW-Authenticate", exception.Headers.Keys);
            Assert.Equal("Bearer realm=\"api\"", exception.Headers["WWW-Authenticate"]);
        }

        [Fact]
        public void ForbiddenException_HasStatusCode403()
        {
            var exception = new ForbiddenException("Access denied");

            Assert.Equal(403, exception.StatusCode);
            Assert.Equal("Access denied", exception.Message);
        }

        [Fact]
        public void NotFoundException_HasStatusCode404()
        {
            var exception = new NotFoundException("Resource not found");

            Assert.Equal(404, exception.StatusCode);
            Assert.Equal("Resource not found", exception.Message);
        }

        [Fact]
        public void NotFoundException_CanHaveResourceDetails()
        {
            var exception = new NotFoundException("User", "123");

            Assert.Equal(404, exception.StatusCode);
            Assert.Equal("User", exception.ResourceType);
            Assert.Equal("123", exception.ResourceId);
            Assert.Contains("User", exception.Message);
            Assert.Contains("123", exception.Message);
        }

        [Fact]
        public void TooManyRequestsException_HasStatusCode429()
        {
            var exception = new TooManyRequestsException("Rate limit exceeded");

            Assert.Equal(429, exception.StatusCode);
            Assert.Equal("Rate limit exceeded", exception.Message);
        }

        [Fact]
        public void TooManyRequestsException_CanHaveRetryAfter()
        {
            var retryAfter = TimeSpan.FromSeconds(60);
            var exception = new TooManyRequestsException("Rate limit exceeded", retryAfter);

            Assert.Equal(429, exception.StatusCode);
            Assert.Equal(retryAfter, exception.RetryAfter);
            Assert.NotNull(exception.Headers);
            Assert.Contains("Retry-After", exception.Headers.Keys);
            Assert.Equal("60", exception.Headers["Retry-After"]);
        }

        [Fact]
        public void InternalServerErrorException_HasStatusCode500()
        {
            var exception = new InternalServerErrorException("Database connection failed");

            Assert.Equal(500, exception.StatusCode);
            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public void ServiceUnavailableException_HasStatusCode503()
        {
            var exception = new ServiceUnavailableException("Service temporarily unavailable");

            Assert.Equal(503, exception.StatusCode);
            Assert.Equal("Service temporarily unavailable", exception.Message);
        }

        [Fact]
        public void ServiceUnavailableException_CanHaveRetryAfter()
        {
            var retryAfter = TimeSpan.FromMinutes(5);
            var exception = new ServiceUnavailableException("Maintenance mode", retryAfter);

            Assert.Equal(503, exception.StatusCode);
            Assert.Equal(retryAfter, exception.RetryAfter);
            Assert.NotNull(exception.Headers);
            Assert.Contains("Retry-After", exception.Headers.Keys);
            Assert.Equal("300", exception.Headers["Retry-After"]);
        }

        #endregion

        #region Extension Methods Tests

        [Fact]
        public void ReturnsError_ThrowsHttpStatusException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsError(401, "Invalid API key");

            var exception = Assert.Throws<UnauthorizedException>(() =>
                mock.ProcessPayment(new Payment()));

            Assert.Equal(401, exception.StatusCode);
            Assert.Equal("Invalid API key", exception.Message);
        }

        [Fact]
        public void ReturnsError_WithErrorBody_IncludesBody()
        {
            var mock = Mock.Create<IPaymentGateway>();
            var errorBody = new { Code = "AUTH_FAILED", Detail = "API key is invalid or expired" };

            mock.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsError(401, "Unauthorized", errorBody);

            var exception = Assert.Throws<HttpStatusException>(() =>
                mock.ProcessPayment(new Payment()));

            Assert.Equal(401, exception.StatusCode);
            Assert.Equal(errorBody, exception.ErrorBody);
        }

        [Fact]
        public void ReturnsBadRequest_ThrowsBadRequestException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ProcessPayment(It.Is<Payment>(p => p.Amount <= 0)))
                .ReturnsBadRequest("Amount must be positive");

            var exception = Assert.Throws<BadRequestException>(() =>
                mock.ProcessPayment(new Payment { Amount = -10 }));

            Assert.Equal(400, exception.StatusCode);
            Assert.Equal("Amount must be positive", exception.Message);
        }

        [Fact]
        public void ReturnsValidationError_ThrowsBadRequestWithValidationErrors()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsValidationError("Validation failed",
                    new ValidationError("amount", "Must be positive"),
                    new ValidationError("currency", "Invalid currency code"));

            var exception = Assert.Throws<BadRequestException>(() =>
                mock.ProcessPayment(new Payment()));

            Assert.Equal(400, exception.StatusCode);
            Assert.NotNull(exception.ValidationErrors);
            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Equal("amount", exception.ValidationErrors[0].Field);
            Assert.Equal("currency", exception.ValidationErrors[1].Field);
        }

        [Fact]
        public void ReturnsUnauthorized_ThrowsUnauthorizedException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.GetInvoice(It.IsAny<string>()))
                .ReturnsUnauthorized("Token expired");

            var exception = Assert.Throws<UnauthorizedException>(() =>
                mock.GetInvoice("inv_123"));

            Assert.Equal(401, exception.StatusCode);
            Assert.Equal("Token expired", exception.Message);
        }

        [Fact]
        public void ReturnsForbidden_ThrowsForbiddenException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.GetInvoice("inv_admin"))
                .ReturnsForbidden("Insufficient permissions");

            var exception = Assert.Throws<ForbiddenException>(() =>
                mock.GetInvoice("inv_admin"));

            Assert.Equal(403, exception.StatusCode);
            Assert.Equal("Insufficient permissions", exception.Message);
        }

        [Fact]
        public void ReturnsNotFound_ThrowsNotFoundException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.GetInvoice("inv_nonexistent"))
                .ReturnsNotFound("Invoice not found");

            var exception = Assert.Throws<NotFoundException>(() =>
                mock.GetInvoice("inv_nonexistent"));

            Assert.Equal(404, exception.StatusCode);
            Assert.Equal("Invoice not found", exception.Message);
        }

        [Fact]
        public void ReturnsNotFound_WithResourceDetails_IncludesDetails()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.GetUser(999))
                .ReturnsNotFound("User", "999");

            var exception = Assert.Throws<NotFoundException>(() =>
                mock.GetUser(999));

            Assert.Equal(404, exception.StatusCode);
            Assert.Equal("User", exception.ResourceType);
            Assert.Equal("999", exception.ResourceId);
            Assert.Contains("User", exception.Message);
            Assert.Contains("999", exception.Message);
        }

        [Fact]
        public void ReturnsTooManyRequests_ThrowsTooManyRequestsException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsTooManyRequests("Rate limit exceeded");

            var exception = Assert.Throws<TooManyRequestsException>(() =>
                mock.ProcessPayment(new Payment()));

            Assert.Equal(429, exception.StatusCode);
            Assert.Equal("Rate limit exceeded", exception.Message);
        }

        [Fact]
        public void ReturnsTooManyRequests_WithRetryAfter_IncludesRetryAfter()
        {
            var mock = Mock.Create<IPaymentGateway>();
            var retryAfter = TimeSpan.FromSeconds(60);

            mock.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsTooManyRequests("Rate limit exceeded", retryAfter);

            var exception = Assert.Throws<TooManyRequestsException>(() =>
                mock.ProcessPayment(new Payment()));

            Assert.Equal(429, exception.StatusCode);
            Assert.Equal(retryAfter, exception.RetryAfter);
            Assert.NotNull(exception.Headers);
            Assert.Equal("60", exception.Headers["Retry-After"]);
        }

        [Fact]
        public void ReturnsInternalServerError_ThrowsInternalServerErrorException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsInternalServerError("Database connection failed");

            var exception = Assert.Throws<InternalServerErrorException>(() =>
                mock.ProcessPayment(new Payment()));

            Assert.Equal(500, exception.StatusCode);
            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public void ReturnsServiceUnavailable_ThrowsServiceUnavailableException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.GetInvoice(It.IsAny<string>()))
                .ReturnsServiceUnavailable("Service temporarily unavailable");

            var exception = Assert.Throws<ServiceUnavailableException>(() =>
                mock.GetInvoice("inv_123"));

            Assert.Equal(503, exception.StatusCode);
            Assert.Equal("Service temporarily unavailable", exception.Message);
        }

        [Fact]
        public void VoidSetup_ReturnsError_ThrowsHttpStatusException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ValidateAccount(It.IsAny<string>()))
                .ReturnsError(401, "Invalid credentials");

            var exception = Assert.Throws<UnauthorizedException>(() =>
                mock.ValidateAccount("acc_123"));

            Assert.Equal(401, exception.StatusCode);
            Assert.Equal("Invalid credentials", exception.Message);
        }

        [Fact]
        public void VoidSetup_ReturnsValidationError_ThrowsBadRequestException()
        {
            var mock = Mock.Create<IPaymentGateway>();
            mock.Setup(x => x.ValidateAccount(It.IsAny<string>()))
                .ReturnsValidationError("Validation failed",
                    new ValidationError("accountId", "Cannot be empty"));

            var exception = Assert.Throws<BadRequestException>(() =>
                mock.ValidateAccount(""));

            Assert.Equal(400, exception.StatusCode);
            Assert.NotNull(exception.ValidationErrors);
            Assert.Single(exception.ValidationErrors);
        }

        [Fact]
        public void MultipleSetups_DifferentErrors_WorkCorrectly()
        {
            var mock = Mock.Create<IPaymentGateway>();

            // Setup different error scenarios
            mock.Setup(x => x.GetUser(404)).ReturnsNotFound("User", "404");
            mock.Setup(x => x.GetUser(401)).ReturnsUnauthorized("Token expired");
            mock.Setup(x => x.GetUser(500)).ReturnsInternalServerError("Database error");

            // Test each scenario
            var notFoundEx = Assert.Throws<NotFoundException>(() => mock.GetUser(404));
            Assert.Equal(404, notFoundEx.StatusCode);

            var unauthorizedEx = Assert.Throws<UnauthorizedException>(() => mock.GetUser(401));
            Assert.Equal(401, unauthorizedEx.StatusCode);

            var serverErrorEx = Assert.Throws<InternalServerErrorException>(() => mock.GetUser(500));
            Assert.Equal(500, serverErrorEx.StatusCode);
        }

        #endregion
    }
}
