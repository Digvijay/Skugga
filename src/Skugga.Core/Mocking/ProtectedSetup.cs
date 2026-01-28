#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Context for setting up protected members using string-based method names.
    /// Enables testing of protected methods without needing inheritance or reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Protected members cannot be accessed directly in lambda expressions, so this API
    /// uses string-based method names. The source generator validates these at compile time.
    /// </para>
    /// <para>
    /// <b>Method Naming:</b> Use the exact method name as declared.
    /// <b>Property Naming:</b> Use the property name (get_/set_ prefixes are added automatically).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;MyBase&gt;();
    ///
    /// // Setup protected method
    /// mock.Protected().Setup&lt;string&gt;("GetData", 42).Returns("result");
    ///
    /// // Setup protected void method with callback
    /// mock.Protected().Setup("Initialize").Callback(() => Console.WriteLine("Init"));
    ///
    /// // Setup protected property getter
    /// mock.Protected().SetupGet&lt;int&gt;("Count").Returns(10);
    /// </code>
    /// </example>
    public class ProtectedMockSetup<T> : IProtectedMockSetup<T> where T : class
    {
        /// <summary>
        /// Gets the mock handler for this protected setup context.
        /// </summary>
        public MockHandler Handler { get; }

        /// <summary>
        /// Initializes a new instance with the specified handler.
        /// </summary>
        /// <param name="handler">The mock handler</param>
        public ProtectedMockSetup(MockHandler handler)
        {
            Handler = handler;
        }

        /// <summary>
        /// Sets up a protected method with a return value.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="methodName">The exact name of the protected method</param>
        /// <param name="args">The method arguments (use It.IsAny&lt;T&gt;() for wildcards)</param>
        /// <returns>Setup context for configuring return value and callbacks</returns>
        /// <remarks>
        /// <para>
        /// The method name must match the declared name exactly (case-sensitive).
        /// Arguments can use argument matchers like It.IsAny&lt;T&gt;() for flexible matching.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Setup with exact arguments
        /// mock.Protected().Setup&lt;string&gt;("ProcessData", 42, "test").Returns("done");
        ///
        /// // Setup with matchers
        /// mock.Protected().Setup&lt;int&gt;("Calculate", It.IsAny&lt;int&gt;()).Returns(100);
        /// </code>
        /// </example>
        public SetupContext<T, TResult> Setup<TResult>(string methodName, params object?[] args)
        {
            return new SetupContext<T, TResult>(Handler, methodName, args);
        }

        /// <summary>
        /// Sets up a protected void method.
        /// </summary>
        /// <param name="methodName">The exact name of the protected void method</param>
        /// <param name="args">The method arguments (use It.IsAny&lt;T&gt;() for wildcards)</param>
        /// <returns>Setup context for configuring callbacks</returns>
        /// <remarks>
        /// Use this for protected methods that return void. Configure callbacks
        /// to verify the method was called or trigger side effects.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool initialized = false;
        /// mock.Protected().Setup("Initialize").Callback(() => initialized = true);
        ///
        /// // Call protected method (via generated mock)
        /// mock.Initialize();
        /// Assert.True(initialized);
        /// </code>
        /// </example>
        public VoidSetupContext<T> Setup(string methodName, params object?[] args)
        {
            return new VoidSetupContext<T>(Handler, methodName, args);
        }

        /// <summary>
        /// Sets up a protected property getter.
        /// </summary>
        /// <typeparam name="TResult">The property type</typeparam>
        /// <param name="propertyName">The exact name of the protected property</param>
        /// <returns>Setup context for configuring return value</returns>
        /// <remarks>
        /// Use the property name directly, without "get_" prefix.
        /// The prefix is added automatically to match the compiler-generated method.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.Protected().SetupGet&lt;int&gt;("InternalCount").Returns(42);
        ///
        /// // Access via generated mock
        /// int value = mock.InternalCount; // Returns 42
        /// </code>
        /// </example>
        public SetupContext<T, TResult> SetupGet<TResult>(string propertyName)
        {
            // Compiler generates get_PropertyName methods for properties
            return new SetupContext<T, TResult>(Handler, "get_" + propertyName, Array.Empty<object?>());
        }

        /// <summary>
        /// Verifies that a protected method was called a specific number of times.
        /// </summary>
        /// <param name="methodName">The exact name of the protected method</param>
        /// <param name="times">The expected number of times the method was called</param>
        /// <param name="args">The method arguments to match (use It.IsAny&lt;T&gt;() for wildcards)</param>
        /// <example>
        /// <code>
        /// mock.Protected().Verify("ProcessData", Times.Once(), 42, "test");
        /// mock.Protected().Verify("Log", Times.AtLeast(2), It.IsAny&lt;string&gt;());
        /// </code>
        /// </example>
        public void Verify(string methodName, Times times, params object?[] args)
        {
            Handler.Verify(methodName, args, times);
        }

        /// <summary>
        /// Verifies that a protected property getter was called a specific number of times.
        /// </summary>
        /// <typeparam name="TResult">The property type (not used for matching, but for clarity)</typeparam>
        /// <param name="propertyName">The exact name of the protected property</param>
        /// <param name="times">The expected number of times the property getter was called</param>
        /// <example>
        /// <code>
        /// mock.Protected().VerifyGet&lt;int&gt;("InternalCount", Times.Exactly(3));
        /// </code>
        /// </example>
        public void VerifyGet<TResult>(string propertyName, Times times)
        {
            Handler.Verify("get_" + propertyName, Array.Empty<object?>(), times);
        }
    }
}
