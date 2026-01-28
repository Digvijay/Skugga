#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// The primary factory for creating mock objects.
    /// Provides Create methods that are intercepted by the Skugga source generator at compile time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Compile-Time Only:</b> Skugga is a zero-reflection, compile-time mocking library.
    /// The source generator intercepts calls to Mock.Create<T>() and generates type-specific
    /// mock implementations using C# interceptors (preview feature).
    /// </para>
    /// <para>
    /// <b>Setup Requirements:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Reference Skugga.Generator with OutputItemType="Analyzer"</description></item>
    /// <item><description>Add InterceptorsPreviewNamespaces="Skugga.Generated" to your .csproj</description></item>
    /// <item><description>Enable interceptors feature preview</description></item>
    /// <item><description>Use a clean build after any generator changes</description></item>
    /// </list>
    /// <para>
    /// If you see InvalidOperationException at runtime, the source generator failed to intercept
    /// the call. Verify your project configuration and perform a clean build.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create with default loose behavior
    /// var mock = Mock.Create&lt;IService&gt;();
    ///
    /// // Create with strict behavior (throws on un-setup calls)
    /// var strictMock = Mock.Create&lt;IService&gt;(MockBehavior.Strict);
    ///
    /// // Create with mock default value strategy
    /// var mockingMock = Mock.Create&lt;IService&gt;(DefaultValue.Mock);
    ///
    /// // Full control
    /// var customMock = Mock.Create&lt;IService&gt;(MockBehavior.Strict, DefaultValue.Empty);
    /// </code>
    /// </example>
    public static class Mock
    {
        /// <summary>
        /// Creates a mock object for the specified type with the given behavior.
        /// </summary>
        /// <typeparam name="T">The type to mock (interface or abstract class)</typeparam>
        /// <param name="behavior">The mock behavior (Loose or Strict)</param>
        /// <returns>A mocked instance of T</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the source generator failed to intercept this call.
        /// This indicates a project configuration issue.
        /// </exception>
        /// <remarks>
        /// This method is intercepted at compile time by the source generator.
        /// If you see an exception at runtime, ensure your project is configured correctly.
        /// </remarks>
        public static T Create<T>(MockBehavior behavior = MockBehavior.Loose)
        {
            // NO FALLBACK: Skugga is compile-time only - source generator MUST intercept this call
            // If you see this error, ensure:
            // 1. Skugga.Generator is referenced with OutputItemType="Analyzer"
            // 2. InterceptorsPreviewNamespaces includes "Skugga.Generated"
            // 3. Build is clean (dotnet clean && dotnet build)
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept Mock.Create<{typeof(T).Name}>().\n" +
                "Skugga is a COMPILE-TIME mocking library with zero reflection.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.\n" +
                "See: https://github.com/Digvijay/Skugga/blob/main/README.md#setup");
        }

        /// <summary>
        /// Creates a mock object with the specified behavior and default value strategy.
        /// </summary>
        /// <typeparam name="T">The type to mock (interface or abstract class)</typeparam>
        /// <param name="behavior">The mock behavior (Loose or Strict)</param>
        /// <param name="defaultValue">The default value strategy (Empty or Mock)</param>
        /// <returns>A mocked instance of T</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the source generator failed to intercept this call.
        /// </exception>
        /// <remarks>
        /// Combining Strict behavior with Mock default value creates a fully configured mock
        /// that throws on un-setup members but automatically creates mock instances for
        /// complex return types.
        /// </remarks>
        public static T Create<T>(MockBehavior behavior, DefaultValue defaultValue)
        {
            // Same as above - generator must intercept
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept Mock.Create<{typeof(T).Name}>().\n" +
                "Skugga is a COMPILE-TIME mocking library with zero reflection.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.\n" +
                "See: https://github.com/Digvijay/Skugga/blob/main/README.md#setup");
        }

        /// <summary>
        /// Creates a mock object with the specified default value strategy and default (Loose) behavior.
        /// </summary>
        /// <typeparam name="T">The type to mock (interface or abstract class)</typeparam>
        /// <param name="defaultValue">The default value strategy (Empty or Mock)</param>
        /// <returns>A mocked instance of T</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the source generator failed to intercept this call.
        /// </exception>
        /// <remarks>
        /// Convenience overload for specifying only the default value strategy.
        /// Equivalent to Create<T>(MockBehavior.Loose, defaultValue).
        /// </remarks>
        public static T Create<T>(DefaultValue defaultValue)
        {
            // Convenience overload: default behavior with specified default value strategy
            return Create<T>(MockBehavior.Loose, defaultValue);
        }

        /// <summary>
        /// Retrieves the IMockSetup interface from a mocked object for verification and configuration.
        /// </summary>
        /// <typeparam name="T">The type of the mocked object</typeparam>
        /// <param name="mocked">The mocked instance created via Mock.Create</param>
        /// <returns>The mock setup interface providing access to verification and configuration</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided object is not a Skugga mock.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Use this method to access verification methods and additional configuration
        /// after the mock has been created. All mocks implement IMockSetup internally.
        /// </para>
        /// <para>
        /// This is useful when:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Verifying method calls after test execution</description></item>
        /// <item><description>Accessing the Handler for advanced scenarios</description></item>
        /// <item><description>Inspecting invocation history</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create and use mock
        /// var mock = Mock.Create&lt;IService&gt;();
        /// mock.Setup(x => x.GetName()).Returns("John");
        ///
        /// var result = mock.GetName(); // "John"
        ///
        /// // Retrieve setup interface for verification
        /// var mockSetup = Mock.Get(mock);
        /// mockSetup.Verify(x => x.GetName(), Times.Once());
        ///
        /// // Access invocation count
        /// var count = mockSetup.Handler.Invocations.Count; // 1
        /// </code>
        /// </example>
        public static IMockSetup Get<T>(T mocked) where T : class
        {
            // All generated mocks implement IMockSetup
            if (mocked is IMockSetup directMock)
                return directMock;

            throw new ArgumentException(
                $"Object is not a Skugga mock. Use Mock.Create<T>() to create mocks.",
                nameof(mocked));
        }

        /// <summary>
        /// Creates a mock and sets up properties based on the provided expression.
        /// </summary>
        /// <typeparam name="T">The type to mock.</typeparam>
        /// <param name="predicate">A LINQ expression defining property values (e.g. x => x.Id == 1 && x.Name == "Test").</param>
        /// <returns>The configured mock instance.</returns>
        public static T Of<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
        {
            var mock = Create<T>();
            LinqToMocks.ApplyFromExpression(mock, predicate);
            return mock;
        }
    }
}
