#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Represents a recorded method invocation on a mock for verification purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each method call on a mock creates an Invocation instance that's stored in the handler.
    /// Verify() methods use these to check if expected methods were called with expected arguments.
    /// </para>
    /// <para>
    /// The Matches() method supports argument matchers like It.IsAny&lt;T&gt;() for flexible verification.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IService&gt;();
    /// mock.GetData(42); // Creates Invocation("GetData", [42])
    /// 
    /// // Verify checks invocations list for matches
    /// mock.Verify(x => x.GetData(42), Times.Once());
    /// mock.Verify(x => x.GetData(It.IsAny&lt;int&gt;()), Times.Once());
    /// </code>
    /// </example>
    public class Invocation
    {
        /// <summary>
        /// Gets the method signature (e.g., "GetData" or "get_Name" for properties).
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Gets the arguments passed to the method during invocation.
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// Initializes a new invocation record.
        /// </summary>
        /// <param name="signature">The method signature</param>
        /// <param name="args">The method arguments</param>
        public Invocation(string signature, object?[] args)
        {
            Signature = signature;
            Args = args;
        }

        /// <summary>
        /// Determines if this invocation matches the specified signature and arguments.
        /// </summary>
        /// <param name="signature">The method signature to match</param>
        /// <param name="args">The arguments to match (may include ArgumentMatcher instances)</param>
        /// <returns>True if the signature and all arguments match; otherwise false</returns>
        /// <remarks>
        /// <para>
        /// Matching supports two argument types:
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>ArgumentMatcher:</b> Uses matcher's Matches() method for flexible matching</description></item>
        /// <item><description><b>Regular values:</b> Uses Equals() for exact matching</description></item>
        /// </list>
        /// <para>
        /// This enables verification patterns like:
        /// </para>
        /// <code>
        /// // Exact match
        /// mock.Verify(x => x.Process(42), Times.Once());
        /// 
        /// // Matcher-based
        /// mock.Verify(x => x.Process(It.IsAny&lt;int&gt;()), Times.Once());
        /// mock.Verify(x => x.Process(It.Is&lt;int&gt;(n => n > 0)), Times.Once());
        /// </code>
        /// </remarks>
        public bool Matches(string signature, object?[] args)
        {
            // Quick checks: signature and argument count must match
            if (Signature != signature || Args.Length != args.Length)
                return false;

            // Check each argument
            for (int i = 0; i < Args.Length; i++)
            {
                // If the expected arg is a matcher, use its Matches() method
                if (args[i] is ArgumentMatcher matcher)
                {
                    if (!matcher.Matches(Args[i]))
                        return false;
                }
                else
                {
                    // Standard equality check for non-matcher arguments
                    // Note: null args match only null, non-null must use Equals()
                    if (args[i] != null && !args[i]!.Equals(Args[i]))
                        return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Specifies the number of times a method is expected to be called for verification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used with Verify() methods to assert that methods were called the expected number of times.
    /// Provides both exact and range-based verification options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IService&gt;();
    /// mock.Setup(x => x.GetData()).Returns("test");
    /// 
    /// mock.GetData();
    /// mock.GetData();
    /// 
    /// // Various verification patterns
    /// mock.Verify(x => x.GetData(), Times.Exactly(2)); // Pass
    /// mock.Verify(x => x.GetData(), Times.AtLeast(1)); // Pass
    /// mock.Verify(x => x.GetData(), Times.AtMost(5));  // Pass
    /// mock.Verify(x => x.GetData(), Times.Between(1, 3)); // Pass
    /// mock.Verify(x => x.GetData(), Times.Never());    // Fail - was called
    /// </code>
    /// </example>
    public class Times
    {
        private readonly Func<int, bool> _validator;

        /// <summary>
        /// Gets a human-readable description of the expected call count.
        /// </summary>
        /// <remarks>
        /// Used in error messages when verification fails.
        /// Examples: "exactly 1", "at least 2", "between 1 and 5"
        /// </remarks>
        public string Description { get; }

        /// <summary>
        /// Initializes a new Times instance with a custom validator.
        /// </summary>
        /// <param name="validator">Function that returns true if actual calls match expectation</param>
        /// <param name="description">Human-readable description for error messages</param>
        private Times(Func<int, bool> validator, string description)
        {
            _validator = validator;
            Description = description;
        }

        /// <summary>
        /// Validates if the actual call count matches the expectation.
        /// </summary>
        /// <param name="actualCalls">The actual number of times the method was called</param>
        /// <returns>True if the count meets the expectation; otherwise false</returns>
        public bool Validate(int actualCalls) => _validator(actualCalls);

        /// <summary>
        /// Expects exactly one call.
        /// </summary>
        /// <returns>Times instance that validates exactly 1 call</returns>
        /// <remarks>
        /// This is the most common verification pattern. Use when a method should be called exactly once.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.GetData();
        /// mock.Verify(x => x.GetData(), Times.Once()); // Pass
        /// </code>
        /// </example>
        public static Times Once() => new Times(c => c == 1, "exactly 1");

        /// <summary>
        /// Expects no calls.
        /// </summary>
        /// <returns>Times instance that validates 0 calls</returns>
        /// <remarks>
        /// Use to verify a method was NOT called during the test.
        /// Useful for negative testing scenarios.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Don't call GetData()
        /// mock.Verify(x => x.GetData(), Times.Never()); // Pass
        /// </code>
        /// </example>
        public static Times Never() => new Times(c => c == 0, "exactly 0");

        /// <summary>
        /// Expects exactly n calls.
        /// </summary>
        /// <param name="callCount">The exact number of expected calls</param>
        /// <returns>Times instance that validates exactly callCount calls</returns>
        /// <remarks>
        /// Use when you need precise verification of call count.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.GetData();
        /// mock.GetData();
        /// mock.GetData();
        /// mock.Verify(x => x.GetData(), Times.Exactly(3)); // Pass
        /// </code>
        /// </example>
        public static Times Exactly(int callCount) => new Times(c => c == callCount, $"exactly {callCount}");

        /// <summary>
        /// Expects at least n calls.
        /// </summary>
        /// <param name="callCount">The minimum number of expected calls</param>
        /// <returns>Times instance that validates at least callCount calls</returns>
        /// <remarks>
        /// Use when you want to ensure a method was called at least once (or n times),
        /// but don't care about the exact count.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.Log("info");
        /// mock.Log("info");
        /// mock.Log("info");
        /// mock.Verify(x => x.Log(It.IsAny&lt;string&gt;()), Times.AtLeast(1)); // Pass
        /// </code>
        /// </example>
        public static Times AtLeast(int callCount) => new Times(c => c >= callCount, $"at least {callCount}");

        /// <summary>
        /// Expects at most n calls.
        /// </summary>
        /// <param name="callCount">The maximum number of expected calls</param>
        /// <returns>Times instance that validates at most callCount calls</returns>
        /// <remarks>
        /// Use to verify a method wasn't called too many times.
        /// Useful for rate limiting or resource usage testing.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.GetData();
        /// mock.Verify(x => x.GetData(), Times.AtMost(5)); // Pass - only called once
        /// </code>
        /// </example>
        public static Times AtMost(int callCount) => new Times(c => c <= callCount, $"at most {callCount}");

        /// <summary>
        /// Expects between min and max calls (inclusive).
        /// </summary>
        /// <param name="callCountFrom">The minimum number of expected calls</param>
        /// <param name="callCountTo">The maximum number of expected calls</param>
        /// <returns>Times instance that validates between callCountFrom and callCountTo calls</returns>
        /// <remarks>
        /// Use for range-based verification when you need flexibility but want to ensure
        /// calls are within reasonable bounds.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.Process();
        /// mock.Process();
        /// mock.Verify(x => x.Process(), Times.Between(1, 3)); // Pass - called 2 times
        /// </code>
        /// </example>
        public static Times Between(int callCountFrom, int callCountTo) =>
            new Times(c => c >= callCountFrom && c <= callCountTo, $"between {callCountFrom} and {callCountTo}");
    }
}
