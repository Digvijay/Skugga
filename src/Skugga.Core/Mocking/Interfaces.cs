#nullable enable
namespace Skugga.Core
{
    /// <summary>
    /// Core interface implemented by all generated mocks.
    /// Provides access to the underlying mock handler for setup and verification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All mocks created by Mock.Create&lt;T&gt;() implement this interface automatically.
    /// Use Mock.Get() to retrieve this interface from a mock instance.
    /// </para>
    /// <para>
    /// This interface enables:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Setting up method behavior via Setup()</description></item>
    /// <item><description>Verifying method calls via Verify()</description></item>
    /// <item><description>Accessing invocation history</description></item>
    /// <item><description>Configuring mock behavior and default values</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IService&gt;();
    /// mock.Setup(x => x.GetData()).Returns("test");
    ///
    /// // Retrieve IMockSetup interface
    /// var mockSetup = Mock.Get(mock);
    ///
    /// // Access handler for verification
    /// mockSetup.Handler.Verify(x => x.GetData(), Times.Once());
    /// </code>
    /// </example>
    public interface IMockSetup
    {
        /// <summary>
        /// Gets the mock handler that manages setup, invocations, and verification.
        /// </summary>
        MockHandler Handler { get; }
    }

    /// <summary>
    /// Interface for setting up protected members on mocks using string-based method names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Protected members cannot be accessed in lambda expressions, so this interface
    /// uses string-based method names. The source generator validates these at compile time.
    /// </para>
    /// <para>
    /// Access this interface via the Protected() extension method on mock instances.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;MyBaseClass&gt;();
    ///
    /// // Setup protected method
    /// mock.Protected().Setup&lt;string&gt;("GetProtectedData", 42).Returns("result");
    ///
    /// // Setup protected property
    /// mock.Protected().SetupGet&lt;int&gt;("ProtectedCount").Returns(10);
    /// </code>
    /// </example>
    public interface IProtectedMockSetup<T> where T : class
    {
        /// <summary>
        /// Gets the mock handler for accessing setup and verification capabilities.
        /// </summary>
        MockHandler Handler { get; }

        /// <summary>
        /// Sets up a protected method with a return value.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="methodName">The exact name of the protected method</param>
        /// <param name="args">The method arguments (use ItExpr.IsAny&lt;T&gt;() for wildcards)</param>
        /// <returns>Setup context for configuring return value and callbacks</returns>
        SetupContext<T, TResult> Setup<TResult>(string methodName, params object?[] args);

        /// <summary>
        /// Sets up a protected void method.
        /// </summary>
        /// <param name="methodName">The exact name of the protected void method</param>
        /// <param name="args">The method arguments (use ItExpr.IsAny&lt;T&gt;() for wildcards)</param>
        /// <returns>Setup context for configuring callbacks</returns>
        VoidSetupContext<T> Setup(string methodName, params object?[] args);

        /// <summary>
        /// Sets up a protected property getter.
        /// </summary>
        /// <typeparam name="TResult">The property type</typeparam>
        /// <param name="propertyName">The exact name of the protected property</param>
        /// <returns>Setup context for configuring return value</returns>
        SetupContext<T, TResult> SetupGet<TResult>(string propertyName);

        /// <summary>
        /// Verifies that a protected method was called.
        /// </summary>
        void Verify(string methodName, Times times, params object?[] args);

        /// <summary>
        /// Verifies that a protected property getter was called.
        /// </summary>
        void VerifyGet<TResult>(string propertyName, Times times);
    }
}
