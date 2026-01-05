using System;

namespace Skugga.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when an API response violates its OpenAPI contract.
    /// Used by Enhancement (Runtime Contract Verification) to detect schema mismatches.
    /// </summary>
    public class ContractViolationException : Exception
    {
        /// <summary>
        /// Gets the field path where the violation occurred (e.g., "user.address.zipCode").
        /// </summary>
        public string? FieldPath { get; }

        /// <summary>
        /// Gets the expected schema type or constraint.
        /// </summary>
        public string? Expected { get; }

        /// <summary>
        /// Gets the actual value or type that was received.
        /// </summary>
        public string? Actual { get; }

        /// <summary>
        /// Initializes a new instance of ContractViolationException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ContractViolationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of ContractViolationException with detailed violation info.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="fieldPath">The path to the field that violated the contract.</param>
        /// <param name="expected">The expected value or constraint.</param>
        /// <param name="actual">The actual value that was received.</param>
        public ContractViolationException(string message, string? fieldPath, string? expected, string? actual) 
            : base(message)
        {
            FieldPath = fieldPath;
            Expected = expected;
            Actual = actual;
        }

        /// <summary>
        /// Initializes a new instance of ContractViolationException with an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that caused this violation.</param>
        public ContractViolationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
