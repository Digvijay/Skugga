using System;
using System.Collections.Generic;

namespace Skugga.Core.Exceptions
{
    /// <summary>
    /// Base exception for HTTP status code errors.
    /// Used for testing error handling in API integrations.
    /// </summary>
    public class HttpStatusException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the error response body (if any).
        /// </summary>
        public object? ErrorBody { get; }

        /// <summary>
        /// Gets additional HTTP headers associated with the error.
        /// </summary>
        public Dictionary<string, string>? Headers { get; }

        /// <summary>
        /// Initializes a new instance of HttpStatusException.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="message">The error message.</param>
        public HttpStatusException(int statusCode, string message) 
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of HttpStatusException with an error body.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="errorBody">The structured error response body.</param>
        public HttpStatusException(int statusCode, string message, object? errorBody) 
            : base(message)
        {
            StatusCode = statusCode;
            ErrorBody = errorBody;
        }

        /// <summary>
        /// Initializes a new instance of HttpStatusException with headers.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="errorBody">The structured error response body.</param>
        /// <param name="headers">Additional HTTP headers.</param>
        public HttpStatusException(int statusCode, string message, object? errorBody, Dictionary<string, string>? headers) 
            : base(message)
        {
            StatusCode = statusCode;
            ErrorBody = errorBody;
            Headers = headers;
        }
    }

    /// <summary>
    /// Exception for 400 Bad Request errors.
    /// Use for validation errors and malformed requests.
    /// </summary>
    public class BadRequestException : HttpStatusException
    {
        /// <summary>
        /// Gets the validation errors (if applicable).
        /// </summary>
        public IReadOnlyList<ValidationError>? ValidationErrors { get; }

        /// <summary>
        /// Initializes a new instance of BadRequestException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BadRequestException(string message) 
            : base(400, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of BadRequestException with validation errors.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationErrors">The validation errors.</param>
        public BadRequestException(string message, IReadOnlyList<ValidationError> validationErrors) 
            : base(400, message, validationErrors)
        {
            ValidationErrors = validationErrors;
        }
    }

    /// <summary>
    /// Exception for 401 Unauthorized errors.
    /// Use for authentication failures (missing/invalid credentials).
    /// </summary>
    public class UnauthorizedException : HttpStatusException
    {
        /// <summary>
        /// Initializes a new instance of UnauthorizedException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public UnauthorizedException(string message) 
            : base(401, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of UnauthorizedException with WWW-Authenticate header.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="authenticateHeader">The WWW-Authenticate header value.</param>
        public UnauthorizedException(string message, string authenticateHeader) 
            : base(401, message, null, new Dictionary<string, string> { { "WWW-Authenticate", authenticateHeader } })
        {
        }
    }

    /// <summary>
    /// Exception for 403 Forbidden errors.
    /// Use for authorization failures (authenticated but not allowed).
    /// </summary>
    public class ForbiddenException : HttpStatusException
    {
        /// <summary>
        /// Initializes a new instance of ForbiddenException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ForbiddenException(string message) 
            : base(403, message)
        {
        }
    }

    /// <summary>
    /// Exception for 404 Not Found errors.
    /// Use when requested resource doesn't exist.
    /// </summary>
    public class NotFoundException : HttpStatusException
    {
        /// <summary>
        /// Gets the resource type that was not found.
        /// </summary>
        public string? ResourceType { get; }

        /// <summary>
        /// Gets the resource identifier that was not found.
        /// </summary>
        public string? ResourceId { get; }

        /// <summary>
        /// Initializes a new instance of NotFoundException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public NotFoundException(string message) 
            : base(404, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of NotFoundException with resource details.
        /// </summary>
        /// <param name="resourceType">The type of resource (e.g., "User", "Order").</param>
        /// <param name="resourceId">The resource identifier.</param>
        public NotFoundException(string resourceType, string resourceId) 
            : base(404, $"{resourceType} with id '{resourceId}' not found")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
    }

    /// <summary>
    /// Exception for 429 Too Many Requests errors.
    /// Use for rate limiting scenarios.
    /// </summary>
    public class TooManyRequestsException : HttpStatusException
    {
        /// <summary>
        /// Gets the retry-after duration (if specified).
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Initializes a new instance of TooManyRequestsException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public TooManyRequestsException(string message) 
            : base(429, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of TooManyRequestsException with retry-after.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfter">How long to wait before retrying.</param>
        public TooManyRequestsException(string message, TimeSpan retryAfter) 
            : base(429, message, null, new Dictionary<string, string> { { "Retry-After", ((int)retryAfter.TotalSeconds).ToString() } })
        {
            RetryAfter = retryAfter;
        }
    }

    /// <summary>
    /// Exception for 500 Internal Server Error.
    /// Use for simulating server-side failures.
    /// </summary>
    public class InternalServerErrorException : HttpStatusException
    {
        /// <summary>
        /// Initializes a new instance of InternalServerErrorException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public InternalServerErrorException(string message) 
            : base(500, message)
        {
        }
    }

    /// <summary>
    /// Exception for 503 Service Unavailable errors.
    /// Use for simulating temporary service outages.
    /// </summary>
    public class ServiceUnavailableException : HttpStatusException
    {
        /// <summary>
        /// Gets the retry-after duration (if specified).
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Initializes a new instance of ServiceUnavailableException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServiceUnavailableException(string message) 
            : base(503, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of ServiceUnavailableException with retry-after.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfter">How long to wait before retrying.</param>
        public ServiceUnavailableException(string message, TimeSpan retryAfter) 
            : base(503, message, null, new Dictionary<string, string> { { "Retry-After", ((int)retryAfter.TotalSeconds).ToString() } })
        {
            RetryAfter = retryAfter;
        }
    }
}
