using System.Collections.Generic;

namespace Skugga.Core
{
    /// <summary>
    /// Wraps an API response with its headers.
    /// Used when OpenAPI operations define response headers.
    /// </summary>
    /// <typeparam name="T">The type of the response body</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        public T Body { get; set; }

        /// <summary>
        /// Gets or sets the response headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Initializes a new instance of ApiResponse.
        /// </summary>
        public ApiResponse()
        {
            Body = default!;
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of ApiResponse with a body.
        /// </summary>
        /// <param name="body">The response body</param>
        public ApiResponse(T body) : this()
        {
            Body = body;
        }

        /// <summary>
        /// Initializes a new instance of ApiResponse with a body and headers.
        /// </summary>
        /// <param name="body">The response body</param>
        /// <param name="headers">The response headers</param>
        public ApiResponse(T body, Dictionary<string, string> headers)
        {
            Body = body;
            Headers = headers ?? new Dictionary<string, string>();
        }
    }
}
