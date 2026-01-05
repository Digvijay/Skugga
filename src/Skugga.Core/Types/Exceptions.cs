#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Exception thrown when a mock operation fails.
    /// </summary>
    /// <remarks>
    /// This exception is typically thrown in the following scenarios:
    /// <list type="bullet">
    /// <item><description>When a strict mock receives a call to an un-setup member</description></item>
    /// <item><description>When an invalid setup is attempted</description></item>
    /// <item><description>When mock configuration is incorrect</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Strict mock throws MockException for un-setup members
    /// var mock = Mock.Create&lt;IService&gt;(MockBehavior.Strict);
    /// try
    /// {
    ///     mock.GetData(); // Throws MockException
    /// }
    /// catch (MockException ex)
    /// {
    ///     Console.WriteLine($"Mock error: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public class MockException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MockException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when verification of mock interactions fails.
    /// </summary>
    /// <remarks>
    /// This exception is thrown by the <c>Verify</c> methods when the expected number
    /// of invocations does not match the actual number of invocations.
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IService&gt;();
    /// mock.Setup(x => x.Save()).Returns(true);
    /// 
    /// // No calls made
    /// 
    /// try
    /// {
    ///     mock.Verify(x => x.Save(), Times.Once()); // Throws VerificationException
    /// }
    /// catch (VerificationException ex)
    /// {
    ///     Console.WriteLine($"Verification failed: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public class VerificationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the verification failure.</param>
        public VerificationException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown by chaos mode when simulating random failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is thrown when chaos mode is enabled and the configured failure
    /// rate triggers a random failure. It can wrap other exceptions configured in the
    /// <see cref="ChaosPolicy.PossibleExceptions"/> list.
    /// </para>
    /// <para>
    /// Chaos mode is used for resilience testing to ensure your code handles failures gracefully.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IService&gt;();
    /// mock.Setup(x => x.GetData()).Returns("data");
    /// 
    /// // Configure chaos mode with 50% failure rate
    /// mock.Chaos(policy =>
    /// {
    ///     policy.FailureRate = 0.5;
    ///     policy.PossibleExceptions = new[] { new TimeoutException() };
    /// });
    /// 
    /// // 50% of calls will throw TimeoutException wrapped in ChaosException
    /// for (int i = 0; i &lt; 10; i++)
    /// {
    ///     try
    ///     {
    ///         mock.GetData();
    ///     }
    ///     catch (ChaosException ex)
    ///     {
    ///         Console.WriteLine($"Chaos triggered: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ChaosException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChaosException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the simulated failure.</param>
        public ChaosException(string message) : base(message) { }
    }
}
