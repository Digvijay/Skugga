#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// Base class for providing default values when mock members are invoked without setup.
    /// </summary>
    /// <remarks>
    /// Default value providers determine what value is returned when a mock member is called
    /// but no setup has been configured for it. Skugga provides two built-in implementations:
    /// <list type="bullet">
    /// <item><description><see cref="EmptyDefaultValueProvider"/> - Returns CLR defaults</description></item>
    /// <item><description><see cref="MockDefaultValueProvider"/> - Returns mock instances (recursive mocking)</description></item>
    /// </list>
    /// </remarks>
    public abstract class DefaultValueProvider
    {
        /// <summary>
        /// Gets the default value for the specified type when no setup is configured.
        /// </summary>
        /// <param name="type">The type to get a default value for</param>
        /// <param name="mock">The mock instance requesting the value</param>
        /// <returns>The default value for the type, or null if no default can be determined</returns>
        public abstract object? GetDefaultValue(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type type,
            object mock);
    }

    /// <summary>
    /// Provides empty/default values for un-setup mock members.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the default value provider used by Skugga. It returns:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Value types</b>: Default instance (0, false, etc.)</description></item>
    /// <item><description><b>Strings</b>: Empty string ("")</description></item>
    /// <item><description><b>Arrays</b>: Empty array of the element type</description></item>
    /// <item><description><b>Collections</b>: Empty List&lt;T&gt; or Dictionary&lt;TKey,TValue&gt;</description></item>
    /// <item><description><b>Reference types</b>: null</description></item>
    /// </list>
    /// <para>
    /// This provider is AOT-safe and uses <see cref="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"/>
    /// to ensure required constructors are preserved during trimming.
    /// </para>
    /// </remarks>
    public class EmptyDefaultValueProvider : DefaultValueProvider
    {
        /// <summary>
        /// Gets the default value for the specified type.
        /// </summary>
        /// <param name="type">The type to get a default value for</param>
        /// <param name="mock">The mock instance (not used by this implementation)</param>
        /// <returns>The default value: default(T) for value types, empty string for string, 
        /// empty collections for collection types, null for reference types</returns>
        public override object? GetDefaultValue(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type type,
            object mock)
        {
            // Value types - return default instance (0, false, default struct, etc.)
            // Using DynamicallyAccessedMembers ensures constructor is preserved for AOT
            if (type.IsValueType)
            {
                return CreateDefaultValueType(type);
            }

            // String is special - return empty string instead of null for better UX
            if (type == typeof(string))
            {
                return string.Empty;
            }

            // Arrays - return empty array of the element type
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                return Array.CreateInstance(elementType, 0);
            }

            // Generic collections - return empty collection instances
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();

                // IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
                // All return empty List<T>
                if (genericType == typeof(IEnumerable<>) ||
                    genericType == typeof(ICollection<>) ||
                    genericType == typeof(IList<>) ||
                    genericType == typeof(IReadOnlyCollection<>) ||
                    genericType == typeof(IReadOnlyList<>))
                {
                    var elementType = type.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    return Activator.CreateInstance(listType);
                }

                // List<T> - return empty list
                if (genericType == typeof(List<>))
                {
                    var elementType = type.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    return CreateDefaultValueType(listType);
                }

                // IDictionary<TKey,TValue>, IReadOnlyDictionary<TKey,TValue>, Dictionary<TKey,TValue>
                // All return empty Dictionary<TKey,TValue>
                if (genericType == typeof(Dictionary<,>) ||
                    genericType == typeof(IDictionary<,>) ||
                    genericType == typeof(IReadOnlyDictionary<,>))
                {
                    if (type.IsInterface)
                    {
                        var keyType = type.GetGenericArguments()[0];
                        var valueType = type.GetGenericArguments()[1];
                        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                        return CreateDefaultValueType(dictType);
                    }
                    return CreateDefaultValueType(type);
                }
            }

            // Everything else - return null
            return null;
        }

        /// <summary>
        /// AOT-safe helper to create value type instances using Activator.CreateInstance.
        /// The DynamicallyAccessedMembers attribute ensures the parameterless constructor is preserved during trimming.
        /// </summary>
        private static object CreateDefaultValueType(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type type)
        {
            return Activator.CreateInstance(type)!;
        }
    }

    /// <summary>
    /// Provides mock instances for interface/abstract types (recursive mocking).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider enables "fluent" or "recursive" mocking where properties that return
    /// interfaces/abstract classes automatically return mock instances instead of null.
    /// This allows chaining property access without explicit setup for every level.
    /// </para>
    /// <para>
    /// For example:
    /// <code>
    /// var mock = Mock.Create&lt;IRepository&gt;(DefaultValue.Mock);
    /// // mock.Configuration returns a mock IConfiguration
    /// // mock.Configuration.Logger returns a mock ILogger
    /// // All without explicit setup
    /// </code>
    /// </para>
    /// <para>
    /// The provider maintains a cache of created mocks to ensure the same mock instance
    /// is returned for repeated property accesses.
    /// </para>
    /// </remarks>
    public class MockDefaultValueProvider : DefaultValueProvider
    {
        /// <summary>
        /// Empty provider used as fallback for value types and collections.
        /// </summary>
        private readonly EmptyDefaultValueProvider _emptyProvider = new();

        /// <summary>
        /// Cache of created mock instances to ensure consistency across repeated property access.
        /// </summary>
        private readonly Dictionary<Type, object> _mockCache = new();

        /// <summary>
        /// Global registry of mock factory functions registered by generated code.
        /// This is thread-safe for concurrent access during initialization.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object>> _mockFactories = new();

        /// <summary>
        /// Registers a mock factory for a specific type. Called automatically by generated code.
        /// </summary>
        /// <typeparam name="T">The type to register a factory for</typeparam>
        /// <param name="factory">The factory function that creates mock instances</param>
        /// <remarks>
        /// This API is not intended for direct use by application code and is hidden from IntelliSense.
        /// It is called by the Skugga source generator to register factory methods for creating mocks.
        /// </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void RegisterMockFactory<T>(Func<T> factory)
        {
            _mockFactories[typeof(T)] = () => factory()!;
        }

        /// <summary>
        /// Gets the default value for the specified type, returning mock instances for interfaces/abstract classes.
        /// </summary>
        /// <param name="type">The type to get a default value for</param>
        /// <param name="mock">The mock instance requesting the value</param>
        /// <returns>A mock instance for interfaces/abstract types, empty collections for collection types, 
        /// default values for value types, or null for concrete reference types</returns>
        public override object? GetDefaultValue(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type type,
            object mock)
        {
            // Delegate value types, arrays, and collections to the empty provider
            if (type.IsValueType || type.IsArray || IsCollectionType(type))
            {
                return _emptyProvider.GetDefaultValue(type, mock);
            }

            // String is special - return empty string
            if (type == typeof(string))
            {
                return string.Empty;
            }

            // For interfaces and abstract classes - return mock instance (recursive mocking)
            if (type.IsInterface || type.IsAbstract)
            {
                // Check cache first to ensure consistent mock instances across repeated property access
                if (_mockCache.TryGetValue(type, out var cachedMock))
                {
                    return cachedMock;
                }

                // Check if we have a registered factory for this type (from generated code)
                if (_mockFactories.TryGetValue(type, out var factory))
                {
                    var newMock = factory();
                    _mockCache[type] = newMock;
                    return newMock;
                }

                // Try using reflection as fallback (may not work in AOT scenarios)
                try
                {
                    // Use reflection to call Mock.Create<T>(DefaultValue.Mock)
                    var mockType = typeof(Mock);

                    // Find the Create<T>(DefaultValue) method
                    var createMethods = mockType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                        .Where(m => m.Name == "Create" && m.IsGenericMethodDefinition);

                    System.Reflection.MethodInfo? createMethod = null;
                    foreach (var method in createMethods)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(DefaultValue))
                        {
                            createMethod = method;
                            break;
                        }
                    }

                    if (createMethod != null)
                    {
                        var genericCreate = createMethod.MakeGenericMethod(type);
                        var newMock = genericCreate.Invoke(null, new object[] { DefaultValue.Mock });
                        if (newMock != null)
                        {
                            _mockCache[type] = newMock;
                            return newMock;
                        }
                    }
                }
                catch
                {
                    // If reflection-based mocking fails (e.g., in AOT), return null
                    // This is expected in trimmed/AOT scenarios where Mock.Create may not be preserved
                }
            }

            // Everything else (concrete reference types) - return null
            return null;
        }

        /// <summary>
        /// Determines if a type is a collection type that should be handled by the empty provider.
        /// </summary>
        private static bool IsCollectionType(Type type)
        {
            if (!type.IsGenericType) return false;

            var genericType = type.GetGenericTypeDefinition();
            return genericType == typeof(IEnumerable<>) ||
                   genericType == typeof(ICollection<>) ||
                   genericType == typeof(IList<>) ||
                   genericType == typeof(IReadOnlyCollection<>) ||
                   genericType == typeof(IReadOnlyList<>) ||
                   genericType == typeof(List<>) ||
                   genericType == typeof(Dictionary<,>) ||
                   genericType == typeof(IDictionary<,>) ||
                   genericType == typeof(IReadOnlyDictionary<,>);
        }
    }
}
