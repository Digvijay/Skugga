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
    public class ProtectedMockSetup : IProtectedMockSetup
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
        public ProtectedSetupContext<TResult> Setup<TResult>(string methodName, params object?[] args)
        {
            return new ProtectedSetupContext<TResult>(Handler, methodName, args);
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
        public ProtectedVoidSetupContext Setup(string methodName, params object?[] args)
        {
            return new ProtectedVoidSetupContext(Handler, methodName, args);
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
        public ProtectedSetupContext<TResult> SetupGet<TResult>(string propertyName)
        {
            // Compiler generates get_PropertyName methods for properties
            return new ProtectedSetupContext<TResult>(Handler, "get_" + propertyName, Array.Empty<object?>());
        }
    }

    /// <summary>
    /// Setup context for protected methods with return values.
    /// Provides fluent API for configuring return values and callbacks.
    /// </summary>
    /// <typeparam name="TResult">The return type of the protected method</typeparam>
    public class ProtectedSetupContext<TResult>
    {
        private readonly MockHandler _handler;
        private readonly string _methodName;
        private readonly object?[] _args;

        /// <summary>
        /// Initializes a new instance for the specified protected method.
        /// </summary>
        public ProtectedSetupContext(MockHandler handler, string methodName, object?[] args)
        {
            _handler = handler;
            _methodName = methodName;
            _args = args;
        }

        /// <summary>
        /// Configures the return value for this protected method setup.
        /// </summary>
        /// <param name="value">The value to return when the method is called</param>
        /// <remarks>
        /// The value is returned for any invocation matching the setup arguments.
        /// For sequence-based returns, use SetupSequence instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.Protected().Setup&lt;string&gt;("GetData", 42).Returns("result");
        /// 
        /// string value = mock.GetData(42); // "result"
        /// </code>
        /// </example>
        public void Returns(TResult value)
        {
            _handler.AddSetup(_methodName, _args, value);
        }

        /// <summary>
        /// Configures a callback to execute when the protected method is called.
        /// </summary>
        /// <param name="callback">The callback action to execute</param>
        /// <returns>This setup context for fluent chaining</returns>
        /// <remarks>
        /// The callback executes before the return value is provided.
        /// Use this for side effects like tracking calls or modifying state.
        /// </remarks>
        /// <example>
        /// <code>
        /// int callCount = 0;
        /// mock.Protected()
        ///     .Setup&lt;string&gt;("Process", It.IsAny&lt;int&gt;())
        ///     .Callback(() => callCount++)
        ///     .Returns("done");
        /// 
        /// mock.Process(42); // callCount is now 1
        /// </code>
        /// </example>
        public ProtectedSetupContext<TResult> Callback(Action callback)
        {
            var setup = _handler.AddSetup(_methodName, _args, default(TResult));
            setup.Callback = _ => callback();
            return this;
        }
    }

    /// <summary>
    /// Setup context for protected void methods.
    /// Provides API for configuring callbacks on void methods.
    /// </summary>
    public class ProtectedVoidSetupContext
    {
        private readonly MockHandler _handler;
        private readonly string _methodName;
        private readonly object?[] _args;

        /// <summary>
        /// Initializes a new instance for the specified protected void method.
        /// </summary>
        public ProtectedVoidSetupContext(MockHandler handler, string methodName, object?[] args)
        {
            _handler = handler;
            _methodName = methodName;
            _args = args;
        }

        /// <summary>
        /// Configures a callback to execute when the protected void method is called.
        /// </summary>
        /// <param name="callback">The callback action to execute</param>
        /// <remarks>
        /// Since void methods have no return value, callbacks are the primary way
        /// to observe and react to method invocations.
        /// </remarks>
        /// <example>
        /// <code>
        /// var called = false;
        /// mock.Protected()
        ///     .Setup("Initialize")
        ///     .Callback(() => called = true);
        /// 
        /// mock.Initialize();
        /// Assert.True(called);
        /// </code>
        /// </example>
        public void Callback(Action callback)
        {
            var setup = _handler.AddSetup(_methodName, _args, null);
            setup.Callback = _ => callback();
        }
    }
}
