#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Skugga.Core
{
    public enum MockBehavior { Loose, Strict }
    
    /// <summary>
    /// Specifies the default value strategy for un-setup members.
    /// </summary>
    public enum DefaultValue
    {
        /// <summary>
        /// Returns default CLR values (null for reference types, 0 for value types).
        /// </summary>
        Empty,
        
        /// <summary>
        /// Returns mock instances for interface/abstract types, empty collections for IEnumerable types.
        /// </summary>
        Mock
    }
    
    /// <summary>
    /// Base class for providing default values when mock members are invoked without setup.
    /// </summary>
    public abstract class DefaultValueProvider
    {
        /// <summary>
        /// Gets the default value for the specified type.
        /// </summary>
        /// <param name="type">The type to get a default value for</param>
        /// <param name="mock">The mock instance (for recursive mocking)</param>
        /// <returns>The default value for the type</returns>
        public abstract object? GetDefaultValue([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] 
            Type type, object mock);
    }
    
    /// <summary>
    /// Provides empty/default values - null for reference types, default for value types, empty for collections.
    /// </summary>
    public class EmptyDefaultValueProvider : DefaultValueProvider
    {
        public override object? GetDefaultValue([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] 
            Type type, object mock)
        {
            // Value types - return default instance (AOT-safe with DynamicallyAccessedMembers)
            if (type.IsValueType)
            {
                // Use generic method to avoid Activator.CreateInstance for better AOT compatibility
                return CreateDefaultValueType(type);
            }
            
            // String is special - return empty string instead of null
            if (type == typeof(string))
            {
                return string.Empty;
            }
            
            // Arrays - return empty array
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                return Array.CreateInstance(elementType, 0);
            }
            
            // Generic collections - return empty collection
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                
                // IEnumerable<T>, ICollection<T>, IList<T>
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
                
                // List<T>
                if (genericType == typeof(List<>))
                {
                    // Create List<T> directly without Activator for AOT compatibility
                    var elementType = type.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    return CreateDefaultValueType(listType);
                }
                
                // Dictionary<TKey, TValue>
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
        
        // AOT-safe helper to create value type instances
        // The DynamicallyAccessedMembers attribute on the parameter ensures the constructor is preserved
        private static object CreateDefaultValueType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
        {
            return Activator.CreateInstance(type)!;
        }
    }
    
    /// <summary>
    /// Provides mock instances for interface/abstract types (recursive mocking).
    /// </summary>
    public class MockDefaultValueProvider : DefaultValueProvider
    {
        private readonly EmptyDefaultValueProvider _emptyProvider = new();
        private readonly Dictionary<Type, object> _mockCache = new();
        
        // Suppress warning for Activator.CreateInstance used in GetDefaultValue
        // The DynamicallyAccessedMembers attribute on the parameter ensures types are preserved
        
        // Global registry of mock factory functions - thread-safe for static constructor initialization
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object>> _mockFactories = new();
        
        /// <summary>
        /// Registers a mock factory for a specific type. Called automatically by generated code.
        /// This API is not intended for direct use and is hidden from IntelliSense.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void RegisterMockFactory<T>(Func<T> factory)
        {
            _mockFactories[typeof(T)] = () => factory()!;
        }
        
        public override object? GetDefaultValue([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] 
            Type type, object mock)
        {
            // First try empty provider for collections and value types
            if (type.IsValueType || type.IsArray || IsCollectionType(type))
            {
                return _emptyProvider.GetDefaultValue(type, mock);
            }
            
            // String is special - return empty string
            if (type == typeof(string))
            {
                return string.Empty;
            }
            
            // For interfaces and abstract classes - return mock instance
            if (type.IsInterface || type.IsAbstract)
            {
                // Check cache first to avoid creating multiple mocks for same type
                if (_mockCache.TryGetValue(type, out var cachedMock))
                {
                    return cachedMock;
                }
                
                // Check if we have a registered factory for this type
                if (_mockFactories.TryGetValue(type, out var factory))
                {
                    var newMock = factory();
                    _mockCache[type] = newMock;
                    return newMock;
                }
                
                // Try using reflection as fallback (may not work in AOT)
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
                    // If mocking fails, return null
                }
            }
            
            // Everything else - return null
            return null;
        }
        
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
    
    /// <summary>
    /// Exception thrown when a mock operation fails.
    /// </summary>
    public class MockException : Exception 
    { 
        public MockException(string message) : base(message) { } 
    }
    
    /// <summary>
    /// Exception thrown when verification fails.
    /// </summary>
    public class VerificationException : Exception
    {
        public VerificationException(string message) : base(message) { }
    }
    
    /// <summary>
    /// Exception thrown by chaos mode when simulating failures.
    /// </summary>
    public class ChaosException : Exception
    {
        public ChaosException(string message) : base(message) { }
    }
    
    /// <summary>
    /// Tracks the expected order of mock invocations for sequential verification.
    /// Used with InSequence() to ensure methods are called in a specific order.
    /// </summary>
    public class MockSequence
    {
        private int _setupStep = 0;
        private int _invocationStep = 0;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Gets the current invocation step in the sequence.
        /// </summary>
        internal int CurrentInvocationStep => _invocationStep;
        
        /// <summary>
        /// Registers that a setup is part of this sequence and returns the step number.
        /// </summary>
        internal int RegisterStep()
        {
            lock (_lock)
            {
                return _setupStep++;
            }
        }
        
        /// <summary>
        /// Records that a step was invoked and verifies it's in the correct order.
        /// </summary>
        internal void RecordInvocation(int expectedStep, string methodSignature)
        {
            lock (_lock)
            {
                if (expectedStep != _invocationStep)
                {
                    throw new MockException(
                        $"Method '{methodSignature}' invoked out of sequence. " +
                        $"Expected step {_invocationStep}, but method is at step {expectedStep}.");
                }
                _invocationStep++;
            }
        }
    }
    
    /// <summary>
    /// Represents a value to be assigned to an out parameter.
    /// </summary>
    /// <typeparam name="T">The type of the out parameter</typeparam>
    public class OutValue<T>
    {
        public T Value { get; set; }
        
        public OutValue(T value)
        {
            Value = value;
        }
    }
    
    /// <summary>
    /// Represents a value to be assigned to a ref parameter, or a validator for ref parameters.
    /// </summary>
    /// <typeparam name="T">The type of the ref parameter</typeparam>
    public class RefValue<T>
    {
        public T? Value { get; set; }
        public Func<T, bool>? Validator { get; set; }
        public bool IsAnyValue { get; set; }
        
        public RefValue(T value)
        {
            Value = value;
        }
        
        public RefValue(Func<T, bool> validator)
        {
            Validator = validator;
        }
        
        private RefValue(bool isAny)
        {
            IsAnyValue = isAny;
        }
        
        public static RefValue<T> IsAny => new RefValue<T>(true);
        
        public bool Matches(T value)
        {
            if (IsAnyValue) return true;
            if (Validator != null) return Validator(value);
            return EqualityComparer<T>.Default.Equals(Value, value);
        }
    }

    public static class Mock
    {
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
        
        public static T Create<T>(MockBehavior behavior, DefaultValue defaultValue)
        {
            // Same as above - generator must intercept
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept Mock.Create<{typeof(T).Name}>().\n" +
                "Skugga is a COMPILE-TIME mocking library with zero reflection.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.\n" +
                "See: https://github.com/Digvijay/Skugga/blob/main/README.md#setup");
        }
        
        public static T Create<T>(DefaultValue defaultValue)
        {
            // Convenience overload: default behavior with specified default value strategy
            return Create<T>(MockBehavior.Loose, defaultValue);
        }
        
        /// <summary>
        /// Retrieves the IMockSetup interface from a mocked object for verification and configuration.
        /// Provides access to the Handler for verification and additional setup after mock creation.
        /// </summary>
        /// <typeparam name="T">The type of the mocked object</typeparam>
        /// <param name="mocked">The mocked instance created via Mock.Create</param>
        /// <returns>The mock setup interface</returns>
        /// <example>
        /// var foo = Mock.Create&lt;IFoo&gt;();
        /// foo.Setup(f => f.Name).Returns(\"bar\");
        /// var mock = Mock.Get(foo);
        /// foo.Name; // Access property
        /// mock.Verify(f => f.Name, Times.Once());
        /// </example>
        public static IMockSetup Get<T>(T mocked) where T : class
        {
            if (mocked is IMockSetup directMock)
                return directMock;
                
            throw new ArgumentException($"Object is not a Skugga mock. Use Mock.Create<T>() to create mocks.", nameof(mocked));
        }
    }
    
    /// <summary>
    /// Context for setting up protected members using string-based method names.
    /// </summary>
    public class ProtectedMockSetup : IProtectedMockSetup
    {
        public MockHandler Handler { get; }
        
        public ProtectedMockSetup(MockHandler handler)
        {
            Handler = handler;
        }
        
        /// <summary>
        /// Sets up a protected method with return value.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="methodName">The name of the protected method</param>
        /// <param name="args">The method arguments (use It.IsAny for wildcards)</param>
        /// <returns>Setup context for configuring return value</returns>
        public ProtectedSetupContext<TResult> Setup<TResult>(string methodName, params object?[] args)
        {
            return new ProtectedSetupContext<TResult>(Handler, methodName, args);
        }
        
        /// <summary>
        /// Sets up a protected void method.
        /// </summary>
        /// <param name="methodName">The name of the protected method</param>
        /// <param name="args">The method arguments (use It.IsAny for wildcards)</param>
        /// <returns>Setup context for configuring callbacks</returns>
        public ProtectedVoidSetupContext Setup(string methodName, params object?[] args)
        {
            return new ProtectedVoidSetupContext(Handler, methodName, args);
        }
        
        /// <summary>
        /// Sets up a protected property getter.
        /// </summary>
        /// <typeparam name="TResult">The property type</typeparam>
        /// <param name="propertyName">The name of the protected property</param>
        /// <returns>Setup context for configuring return value</returns>
        public ProtectedSetupContext<TResult> SetupGet<TResult>(string propertyName)
        {
            return new ProtectedSetupContext<TResult>(Handler, "get_" + propertyName, Array.Empty<object?>());
        }
    }
    
    /// <summary>
    /// Setup context for protected methods with return values.
    /// </summary>
    public class ProtectedSetupContext<TResult>
    {
        private readonly MockHandler _handler;
        private readonly string _methodName;
        private readonly object?[] _args;
        
        public ProtectedSetupContext(MockHandler handler, string methodName, object?[] args)
        {
            _handler = handler;
            _methodName = methodName;
            _args = args;
        }
        
        /// <summary>
        /// Configures the return value for this protected method setup.
        /// </summary>
        public void Returns(TResult value)
        {
            _handler.AddSetup(_methodName, _args, value);
        }
        
        /// <summary>
        /// Configures a callback to execute when the protected method is called.
        /// </summary>
        public ProtectedSetupContext<TResult> Callback(Action callback)
        {
            var setup = _handler.AddSetup(_methodName, _args, default(TResult));
            setup.Callback = _ => callback();
            return this;
        }
    }
    
    /// <summary>
    /// Setup context for protected void methods.
    /// </summary>
    public class ProtectedVoidSetupContext
    {
        private readonly MockHandler _handler;
        private readonly string _methodName;
        private readonly object?[] _args;
        
        public ProtectedVoidSetupContext(MockHandler handler, string methodName, object?[] args)
        {
            _handler = handler;
            _methodName = methodName;
            _args = args;
        }
        
        /// <summary>
        /// Configures a callback to execute when the protected void method is called.
        /// </summary>
        public void Callback(Action callback)
        {
            var setup = _handler.AddSetup(_methodName, _args, null);
            setup.Callback = _ => callback();
        }
    }

    public static class MockExtensions
    {
        /// <summary>
        /// Setups a method or property on the mock to return a specific value.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method or property</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the method call or property access</param>
        /// <returns>A setup context that can be configured with Returns()</returns>
        /// <example>
        /// mock.Setup(x => x.GetData(42)).Returns("test");
        /// mock.Setup(x => x.Name).Returns("John");
        /// </example>
        public static SetupContext<TMock, TResult> Setup<TMock, TResult>(this TMock mock, Expression<Func<TMock, TResult>> expression)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            // Handle both method calls and property access
            if (expression.Body is MethodCallExpression methodCall)
            {
                // Method call: mock.Setup(x => x.GetData(42))
                var args = methodCall.Arguments.Select(GetArgumentValue).ToArray();
                return new SetupContext<TMock, TResult>(setup.Handler, methodCall.Method.Name, args);
            }
            else if (expression.Body is MemberExpression memberAccess && memberAccess.Member.MemberType == System.Reflection.MemberTypes.Property)
            {
                // Property access: mock.Setup(x => x.Name)
                return new SetupContext<TMock, TResult>(setup.Handler, "get_" + memberAccess.Member.Name, Array.Empty<object?>());
            }
            
            throw new ArgumentException($"Expression must be a method call or property access, got: {expression.Body.GetType().Name}");
        }
        
        /// <summary>
        /// Setups a void method on the mock.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the void method call</param>
        /// <returns>A setup context that can be configured with Callback()</returns>
        /// <example>
        /// mock.Setup(x => x.Execute()).Callback(() => Console.WriteLine("Called"));
        /// </example>
        public static VoidSetupContext<TMock> Setup<TMock>(this TMock mock, Expression<Action<TMock>> expression)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            if (expression.Body is MethodCallExpression methodCall)
            {
                var args = methodCall.Arguments.Select(GetArgumentValue).ToArray();
                return new VoidSetupContext<TMock>(setup.Handler, methodCall.Method.Name, args);
            }
            
            throw new ArgumentException($"Expression must be a method call, got: {expression.Body.GetType().Name}");
        }

        /// <summary>
        /// Configures a method to return a sequence of values.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the method call</param>
        /// <returns>A sequence setup context that can be configured with Returns()</returns>
        /// <example>
        /// mock.SetupSequence(x => x.GetNext()).Returns(1).Returns(2).Returns(3);
        /// // First call returns 1, second returns 2, third returns 3, subsequent calls return last value
        /// </example>
        public static SequenceSetupContext<TMock, TResult> SetupSequence<TMock, TResult>(this TMock mock, Expression<Func<TMock, TResult>> expression)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            if (expression.Body is MethodCallExpression methodCall)
            {
                var args = methodCall.Arguments.Select(GetArgumentValue).ToArray();
                return new SequenceSetupContext<TMock, TResult>(setup.Handler, methodCall.Method.Name, args);
            }
            else if (expression.Body is MemberExpression memberAccess && memberAccess.Member.MemberType == System.Reflection.MemberTypes.Property)
            {
                return new SequenceSetupContext<TMock, TResult>(setup.Handler, "get_" + memberAccess.Member.Name, Array.Empty<object?>());
            }
            
            throw new ArgumentException($"Expression must be a method call or property access, got: {expression.Body.GetType().Name}");
        }

        /// <summary>
        /// Verifies that a specific method was called on the mock.
        /// </summary>
        /// <typeparam name="T">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method or property</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the method call or property access</param>
        /// <param name="times">The expected number of times the method should have been called</param>
        /// <exception cref="MockException">Thrown when the verification fails</exception>
        /// <example>
        /// mock.Verify(x => x.Execute(), Times.Once());
        /// mock.Verify(x => x.GetData(42), Times.AtLeast(2));
        /// </example>
        public static void Verify<T, TResult>(this T mock, Expression<Func<T, TResult>> expression, Times times)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            string signature;
            object?[] args;

            // Handle both method calls and property access
            if (expression.Body is MethodCallExpression methodCall)
            {
                signature = methodCall.Method.Name;
                args = methodCall.Arguments.Select(GetArgumentValue).ToArray();
            }
            else if (expression.Body is MemberExpression memberAccess && memberAccess.Member.MemberType == System.Reflection.MemberTypes.Property)
            {
                signature = "get_" + memberAccess.Member.Name;
                args = Array.Empty<object?>();
            }
            else
            {
                throw new ArgumentException($"Expression must be a method call or property access, got: {expression.Body.GetType().Name}");
            }

            // Count matching invocations
            int count = setup.Handler.Invocations.Count(inv => inv.Matches(signature, args));
            
            if (!times.Validate(count))
            {
                throw new MockException($"Expected {times.Description} call(s) to '{signature}', but was called {count} time(s).");
            }
        }

        /// <summary>
        /// Verifies that a specific void method was called on the mock.
        /// </summary>
        public static void Verify<T>(this T mock, Expression<Action<T>> expression, Times times)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            if (expression.Body is not MethodCallExpression methodCall)
                throw new ArgumentException("Expression must be a method call");

            string signature = methodCall.Method.Name;
            object?[] args = methodCall.Arguments.Select(GetArgumentValue).ToArray();

            int count = setup.Handler.Invocations.Count(inv => inv.Matches(signature, args));
            
            if (!times.Validate(count))
            {
                throw new MockException($"Expected {times.Description} call(s) to '{signature}', but was called {count} time(s).");
            }
        }
        
        /// <summary>
        /// Sets up a property with automatic backing field for get/set tracking.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the property</param>
        /// <example>
        /// mock.SetupProperty(x => x.Name);
        /// mock.Name = "John";
        /// Assert.Equal("John", mock.Name);
        /// </example>
        public static void SetupProperty<TMock, TProperty>(this TMock mock, Expression<Func<TMock, TProperty>> expression)
        {
            SetupProperty(mock, expression, default(TProperty));
        }
        
        /// <summary>
        /// Sets up a property with automatic backing field and a default value.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the property</param>
        /// <param name="defaultValue">The default value for the property</param>
        /// <example>
        /// mock.SetupProperty(x => x.Age, 25);
        /// Assert.Equal(25, mock.Age);
        /// mock.Age = 30;
        /// Assert.Equal(30, mock.Age);
        /// </example>
        public static void SetupProperty<TMock, TProperty>(this TMock mock, Expression<Func<TMock, TProperty>> expression, TProperty? defaultValue)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            if (expression.Body is not MemberExpression memberAccess || 
                memberAccess.Member.MemberType != System.Reflection.MemberTypes.Property)
            {
                throw new ArgumentException("Expression must be a property access");
            }

            var propertyName = memberAccess.Member.Name;
            setup.Handler.SetupPropertyStorage(propertyName, defaultValue);
        }
        
        /// <summary>
        /// Sets up all properties on the interface with automatic backing fields.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <example>
        /// mock.SetupAllProperties();
        /// mock.Name = "John";
        /// mock.Age = 30;
        /// Assert.Equal("John", mock.Name);
        /// Assert.Equal(30, mock.Age);
        /// </example>
        public static void SetupAllProperties<TMock>(this TMock mock)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            // Get all properties from the interface using reflection
            var interfaceType = typeof(TMock);
            var properties = interfaceType.GetProperties();
            
            foreach (var property in properties)
            {
                // Only setup if not already setup (respect individual SetupProperty calls)
                if (!setup.Handler.HasPropertyStorage(property.Name))
                {
                    // Setup with default value for the property type
                    setup.Handler.SetupPropertyStorage(property.Name, GetDefaultValue(property.PropertyType));
                }
            }
        }
        
        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        
        /// <summary>
        /// Verifies that a property getter was accessed on the mock.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the property</param>
        /// <param name="times">The expected number of times the property should be accessed</param>
        /// <example>
        /// mock.VerifyGet(x => x.Name, Times.Once());
        /// mock.VerifyGet(x => x.Age, Times.AtLeast(2));
        /// </example>
        public static void VerifyGet<TMock, TProperty>(this TMock mock, Expression<Func<TMock, TProperty>> expression, Times times)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            if (expression.Body is not MemberExpression memberAccess || 
                memberAccess.Member.MemberType != System.Reflection.MemberTypes.Property)
            {
                throw new ArgumentException("Expression must be a property access");
            }

            // Property getters are tracked as "get_PropertyName" invocations with no arguments
            string signature = "get_" + memberAccess.Member.Name;
            object?[] args = Array.Empty<object?>();

            // Count how many times the getter was invoked
            int count = setup.Handler.Invocations.Count(inv => inv.Matches(signature, args));
            
            if (!times.Validate(count))
            {
                throw new MockException($"Expected {times.Description} call(s) to '{signature}', but was called {count} time(s).");
            }
        }
        
        /// <summary>
        /// Verifies that a property setter was called with a specific value.
        /// Supports argument matchers like It.IsAny&lt;T&gt;() and It.Is&lt;T&gt;(predicate).
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="propertyExpression">Lambda expression specifying the property (x => x.Name)</param>
        /// <param name="value">The expected value that should have been set (can be constant, variable, or It.* matcher)</param>
        /// <param name="times">The expected number of times the setter should be called</param>
        /// <example>
        /// mock.VerifySet(x => x.Name, "John", Times.Once());
        /// mock.VerifySet(x => x.Age, It.IsAny&lt;int&gt;(), Times.AtLeast(1));
        /// mock.VerifySet(x => x.Score, It.Is&lt;int&gt;(s => s > 90), Times.Exactly(2));
        /// </example>
        public static void VerifySet<TMock, TProperty>(this TMock mock, Expression<Func<TMock, TProperty>> propertyExpression, Expression<Func<TProperty>> valueExpression, Times times)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            // Extract property name from the expression
            if (propertyExpression.Body is not MemberExpression memberAccess ||
                memberAccess.Member.MemberType != System.Reflection.MemberTypes.Property)
            {
                throw new ArgumentException("Expression must be a property access");
            }

            // Property setters are tracked as "set_PropertyName" invocations with the value as the argument
            string signature = "set_" + memberAccess.Member.Name;
            
            // Extract the expected value from the value expression
            // This handles constants, variables, and It.* matchers correctly
            object? expectedValue = GetArgumentValue(valueExpression.Body);
            object?[] args = new[] { expectedValue };

            // Count how many times the setter was invoked with the expected value
            // ArgumentMatcher support: if expectedValue is an ArgumentMatcher, Invocation.Matches will use it
            int count = setup.Handler.Invocations.Count(inv => inv.Matches(signature, args));
            
            if (!times.Validate(count))
            {
                throw new MockException($"Expected {times.Description} call(s) to '{signature}', but was called {count} time(s).");
            }
        }

        /// <summary>
        /// Raises the specified event on the mock with the provided event arguments.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="eventName">The name of the event to raise (use nameof for type safety)</param>
        /// <param name="args">The event arguments to pass to subscribers</param>
        /// <example>
        /// mock.Raise(nameof(IServiceWithEvents.Completed), this, EventArgs.Empty);
        /// </example>
        public static void Raise<TMock>(this TMock mock, string eventName, params object?[] args)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            // Invoke the event through the handler
            setup.Handler.RaiseEvent(eventName, args);
        }
        
        /// <summary>
        /// Verifies that an event handler was added (subscribed) to the specified event.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="eventName">The name of the event (use nameof for type safety)</param>
        /// <param name="times">The expected number of times the event was subscribed to</param>
        /// <example>
        /// mock.VerifyAdd(nameof(INotifyPropertyChanged.PropertyChanged), Times.Once());
        /// </example>
        public static void VerifyAdd<TMock>(this TMock mock, string eventName, Times times)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            string signature = "add_" + eventName;
            
            // Count event subscriptions
            int count = setup.Handler.Invocations.Count(inv => inv.Signature == signature);
            
            if (!times.Validate(count))
            {
                throw new VerificationException($"Expected {times.Description} subscription(s) to event '{eventName}', but was subscribed {count} time(s).");
            }
        }
        
        /// <summary>
        /// Verifies that an event handler was removed (unsubscribed) from the specified event.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="eventName">The name of the event (use nameof for type safety)</param>
        /// <param name="times">The expected number of times the event was unsubscribed from</param>
        /// <example>
        /// mock.VerifyRemove(nameof(INotifyPropertyChanged.PropertyChanged), Times.Once());
        /// </example>
        public static void VerifyRemove<TMock>(this TMock mock, string eventName, Times times)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            string signature = "remove_" + eventName;
            
            // Count event unsubscriptions
            int count = setup.Handler.Invocations.Count(inv => inv.Signature == signature);
            
            if (!times.Validate(count))
            {
                throw new VerificationException($"Expected {times.Description} unsubscription(s) from event '{eventName}', but was unsubscribed {count} time(s).");
            }
        }

        public static void Chaos<T>(this T mock, Action<ChaosPolicy> config)
        {
             if (mock is IMockSetup setup) {
                 var policy = new ChaosPolicy();
                 config(policy);
                 setup.Handler.SetChaosPolicy(policy);
             }
        }

        private static object? GetArgumentValue(Expression expr)
        {
            // Handle constant values (e.g., 42, "test", null)
            if (expr is ConstantExpression c) 
                return c.Value;
            
            // Handle member access (variable capture by closure): () => variable
            // The expression tree may wrap variables in a closure class
            if (expr is MemberExpression memberExpr)
            {
                // Try to evaluate the member expression
                var objectMember = Expression.Convert(memberExpr, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                try
                {
                    var getter = getterLambda.Compile();
                    return getter();
                }
                catch
                {
                    // If evaluation fails, treat as unsupported
                }
            }
            
            // Handle unary expressions: !value, -number, etc.
            if (expr is UnaryExpression unaryExpr)
            {
                try
                {
                    var lambda = Expression.Lambda<Func<object>>(Expression.Convert(unaryExpr, typeof(object)));
                    return lambda.Compile()();
                }
                catch { }
            }
            
            // Handle binary expressions: a + b, x * y, etc.
            if (expr is BinaryExpression binaryExpr)
            {
                try
                {
                    var lambda = Expression.Lambda<Func<object>>(Expression.Convert(binaryExpr, typeof(object)));
                    return lambda.Compile()();
                }
                catch { }
            }
            
            // Handle conditional expressions: condition ? a : b
            if (expr is ConditionalExpression conditionalExpr)
            {
                try
                {
                    var lambda = Expression.Lambda<Func<object>>(Expression.Convert(conditionalExpr, typeof(object)));
                    return lambda.Compile()();
                }
                catch { }
            }
            
            // Handle array/indexer access: array[0], dict[key]
            if (expr is System.Linq.Expressions.IndexExpression or System.Linq.Expressions.MethodCallExpression { Method.Name: "get_Item" })
            {
                try
                {
                    var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object)));
                    return lambda.Compile()();
                }
                catch { }
            }
            
            // Detect It.* matcher calls and convert to ArgumentMatcher
            if (expr is MethodCallExpression methodCall && 
                methodCall.Method.DeclaringType?.Name == "It")
            {
                var methodName = methodCall.Method.Name;
                var matcherType = methodCall.Method.ReturnType;
                
                // It.IsAny<T>()
                if (methodName == "IsAny")
                {
                    return new ArgumentMatcher(matcherType, _ => true, $"It.IsAny<{matcherType.Name}>()");
                }
                
                // It.Is<T>(predicate) - NOTE: Predicate evaluation happens at compile-time via generator
                if (methodName == "Is" && methodCall.Arguments.Count == 1)
                {
                    // Extract the lambda/predicate - will be compiled and used by generator
                    var predicateExpr = methodCall.Arguments[0];
                    if (predicateExpr is LambdaExpression lambda)
                    {
                        var compiledPredicate = lambda.Compile();
                        return new ArgumentMatcher(
                            matcherType, 
                            v => v != null && (bool)compiledPredicate.DynamicInvoke(v)!,
                            $"It.Is<{matcherType.Name}>(predicate)");
                    }
                }
                
                // It.IsIn<T>(values)
                if (methodName == "IsIn" && methodCall.Arguments.Count == 1)
                {
                    // Extract the array of values
                    var arrayExpr = methodCall.Arguments[0];
                    if (arrayExpr is NewArrayExpression newArray)
                    {
                        var values = newArray.Expressions
                            .Select(e => e is ConstantExpression ce ? ce.Value : null)
                            .ToArray();
                        return new ArgumentMatcher(
                            matcherType,
                            v => values.Any(val => val?.Equals(v) == true),
                            $"It.IsIn({string.Join(", ", values.Select(v => v?.ToString() ?? "null"))})");
                    }
                }
                
                // It.IsNotNull<T>()
                if (methodName == "IsNotNull")
                {
                    return new ArgumentMatcher(matcherType, v => v != null, $"It.IsNotNull<{matcherType.Name}>()");
                }
                
                // It.IsRegex(pattern)
                if (methodName == "IsRegex" && methodCall.Arguments.Count == 1)
                {
                    if (methodCall.Arguments[0] is ConstantExpression patternExpr && 
                        patternExpr.Value is string pattern)
                    {
                        var regex = new System.Text.RegularExpressions.Regex(pattern);
                        return new ArgumentMatcher(
                            typeof(string),
                            v => v is string s && regex.IsMatch(s),
                            $"It.IsRegex(\"{pattern}\")");
                    }
                }
            }
            
            // Handle method calls that might return matchers (Match.Create) or other values
            // Try to evaluate the method call to see if it's a matcher
            if (expr is MethodCallExpression methodCallExpr)
            {
                // First check if this is Match.Create directly
                if (methodCallExpr.Method.DeclaringType?.Name == "Match" &&
                    methodCallExpr.Method.Name == "Create")
                {
                    var matcherType = methodCallExpr.Method.ReturnType;
                    
                    // Match.Create<T>(predicate) or Match.Create<T>(predicate, description)
                    if (methodCallExpr.Arguments.Count >= 1)
                    {
                        var predicateExpr = methodCallExpr.Arguments[0];
                        string description = methodCallExpr.Arguments.Count == 2 && 
                                           methodCallExpr.Arguments[1] is ConstantExpression descExpr &&
                                           descExpr.Value is string desc
                            ? desc
                            : $"Match.Create<{matcherType.Name}>(predicate)";
                        
                        if (predicateExpr is LambdaExpression lambda)
                        {
                            var compiledPredicate = lambda.Compile();
                            return new ArgumentMatcher(
                                matcherType, 
                                v => v != null && (bool)compiledPredicate.DynamicInvoke(v)!,
                                description);
                        }
                    }
                }
                
                // For other method calls (like helper methods that return Match.Create results),
                // try to evaluate them. This handles cases like IsLargeString() which returns Match.Create<string>(...)
                if (methodCallExpr.Method.DeclaringType?.Name != "It")
                {
                    try
                    {
                        var lambda = Expression.Lambda<Func<object>>(Expression.Convert(methodCallExpr, typeof(object)));
                        var result = lambda.Compile()();
                        
                        // If the result is an ArgumentMatcher (shouldn't be directly), return it
                        // Otherwise, the method call returned a value (like Match.Create's default(T)!)
                        // In that case, we need to walk the method body to extract the Match.Create call
                        
                        // Actually, when Match.Create returns default(T)!, we won't get a matcher here
                        // We need to look inside the method being called to find the Match.Create call
                        if (methodCallExpr.Method.ReturnType != typeof(void) && 
                            methodCallExpr.Object == null) // Static or has no instance
                        {
                            // Try to get the method body and extract Match.Create from it
                            var method = methodCallExpr.Method;
                            if (method.IsStatic || methodCallExpr.Object == null)
                            {
                                // Invoke the method to get its expression body
                                // For methods like "public static string IsLarge() => Match.Create<string>(...)"
                                // We can't easily introspect the method body, so we need a different approach
                                
                                // Actually, the generator should handle this. Let's check if generator
                                // can inline the helper method calls
                            }
                        }
                    }
                    catch
                    {
                        // If evaluation fails, continue to throw error below
                    }
                }
            }
            
            // COMPILE-TIME ONLY: Cannot extract non-constant arguments at runtime without reflection
            // Source generator MUST intercept Setup/Verify and emit AddSetup calls directly
            // If you see this error, use constants or It.* matchers
            throw new NotSupportedException(
                $"Skugga cannot extract argument values from expression: {expr.GetType().Name}\n" +
                "Skugga is compile-time only with zero runtime reflection.\n" +
                "Solutions:\n" +
                "1. Use constant values: mock.Setup(x => x.Method(42))\n" +
                "2. Use argument matchers: mock.Setup(x => x.Method(It.IsAny<int>()))\n" +
                "3. Ensure source generator is intercepting (see project setup)\n" +
                "Note: Variables, properties, and calculations require generator interception.");
        }
        
        /// <summary>
        /// Adds an interface implementation to the mock, allowing the mock to be cast to the specified interface.
        /// This provides Moq API compatibility, though the actual implementation is tracked for future use.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to add</typeparam>
        /// <param name="mock">The mock object</param>
        /// <returns>The mock cast to the specified interface type</returns>
        /// <exception cref="ArgumentException">Thrown if the object is not a Skugga mock or if TInterface is not an interface</exception>
        /// <example>
        /// var mock = Mock.Create&lt;IFoo&gt;();
        /// mock.As&lt;IDisposable&gt;(); // Track that mock also implements IDisposable
        /// </example>
        public static TInterface As<TInterface>(this object mock) where TInterface : class
        {
            if (mock is not IMockSetup mockSetup)
                throw new ArgumentException("Object is not a Skugga mock", nameof(mock));
            
            if (!typeof(TInterface).IsInterface)
                throw new ArgumentException($"Type {typeof(TInterface).Name} is not an interface", nameof(TInterface));
            
            mockSetup.Handler.AddInterface(typeof(TInterface));
            return (TInterface)mock;
        }
        
        /// <summary>
        /// Returns a setup context for configuring protected members.
        /// Use string-based method/property names to set up protected members.
        /// </summary>
        /// <typeparam name="T">The mock type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <returns>Protected setup context</returns>
        /// <example>
        /// var mock = Mock.Create&lt;AbstractClass&gt;();\n        /// mock.Protected()\n        ///     .Setup&lt;int&gt;(\"ExecuteCore\")\n        ///     .Returns(42);\n        /// </example>
        public static IProtectedMockSetup Protected<T>(this T mock) where T : class
        {
            if (mock is not IMockSetup mockSetup)
                throw new ArgumentException("Object is not a Skugga mock", nameof(mock));
            
            return new ProtectedMockSetup(mockSetup.Handler);
        }
    }
    
    /// <summary>
    /// Provides argument matching for Setup and Verify expressions.
    /// </summary>
    public static class It
    {
        /// <summary>
        /// Matches any value of type T.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <returns>A marker value (default(T)) that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// mock.Setup(x => x.Process(It.IsAny&lt;int&gt;())).Returns(42);
        /// mock.Verify(x => x.Process(It.IsAny&lt;string&gt;()), Times.Once());
        /// </example>
        public static T IsAny<T>()
        {
            // This method is never actually executed - it's intercepted in GetArgumentValue
            // Return default to make the expression compile
            return default(T)!;
        }

        /// <summary>
        /// Matches values that satisfy a custom predicate.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <param name="predicate">Function that returns true for matching values</param>
        /// <returns>A marker value that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// mock.Setup(x => x.Process(It.Is&lt;int&gt;(n => n > 0))).Returns("positive");
        /// mock.Verify(x => x.Process(It.Is&lt;string&gt;(s => s.StartsWith("test"))), Times.Once());
        /// </example>
        public static T Is<T>(Func<T, bool> predicate)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches values that are in the specified set.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <param name="values">Set of acceptable values</param>
        /// <returns>A marker value that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// mock.Setup(x => x.Process(It.IsIn(1, 2, 3))).Returns("matched");
        /// mock.Verify(x => x.Process(It.IsIn("a", "b", "c")), Times.Once());
        /// </example>
        public static T IsIn<T>(params T[] values)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches non-null values.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <returns>A marker value that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// mock.Setup(x => x.Process(It.IsNotNull&lt;string&gt;())).Returns("not null");
        /// mock.Verify(x => x.Process(It.IsNotNull&lt;object&gt;()), Times.Once());
        /// </example>
        public static T IsNotNull<T>()
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches strings that match a regular expression pattern.
        /// </summary>
        /// <param name="pattern">Regular expression pattern</param>
        /// <returns>A marker value that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// mock.Setup(x => x.Process(It.IsRegex(@"^\d{3}-\d{4}$"))).Returns("phone matched");
        /// mock.Verify(x => x.Process(It.IsRegex(@"^test")), Times.Once());
        /// </example>
        public static string IsRegex(string pattern)
        {
            return string.Empty;
        }
        
        /// <summary>
        /// Provides matchers for ref/out parameters. Note: Due to C# limitations,
        /// you cannot use It.Ref in actual Setup/Verify expressions with ref/out modifiers.
        /// Instead, pass normal values and configure out/ref values using .OutValue() or .RefValue().
        /// </summary>
        /// <typeparam name="T">The type of the ref/out parameter</typeparam>
        public static class Ref<T>
        {
            /// <summary>
            /// Matches any value for a ref or out parameter.
            /// NOTE: This is a placeholder - you cannot actually use this in C# out/ref expressions.
            /// Pass a normal value in Setup and use .OutValue()/.RefValue() to configure the result.
            /// </summary>
            public static T IsAny => default(T)!;
        }
    }
    
    /// <summary>
    /// Provides factory methods for creating custom matchers.
    /// </summary>
    public static class Match
    {
        /// <summary>
        /// Creates a custom matcher based on a predicate function.
        /// This is useful for creating reusable matcher methods.
        /// </summary>
        /// <typeparam name="T">The type of value to match</typeparam>
        /// <param name="predicate">Function that returns true if the value matches</param>
        /// <returns>A marker value that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// // Create a custom matcher method
        /// public static string IsLargeString() => Match.Create&lt;string&gt;(s => s != null && s.Length > 100);
        /// 
        /// // Use in setup
        /// mock.Setup(x => x.Process(IsLargeString())).Returns("large");
        /// </example>
        public static T Create<T>(Func<T, bool> predicate)
        {
            return default(T)!;
        }
        
        /// <summary>
        /// Creates a custom matcher based on a predicate function with a description.
        /// The description is used in error messages.
        /// </summary>
        /// <typeparam name="T">The type of value to match</typeparam>
        /// <param name="predicate">Function that returns true if the value matches</param>
        /// <param name="description">Description of what the matcher matches (for error messages)</param>
        /// <returns>A marker value that will be replaced with a matcher during setup/verify</returns>
        /// <example>
        /// // Create a custom matcher with description
        /// public static int IsPositive() => Match.Create&lt;int&gt;(i => i > 0, "positive number");
        /// 
        /// // Use in setup
        /// mock.Setup(x => x.Add(IsPositive())).Returns(true);
        /// </example>
        public static T Create<T>(Func<T, bool> predicate, string description)
        {
            return default(T)!;
        }
    }
    
    /// <summary>
    /// Represents a matcher for verifying or matching method arguments.
    /// </summary>
    internal class ArgumentMatcher
    {
        public Type MatchType { get; }
        public Func<object?, bool> Predicate { get; }
        public string Description { get; }
        
        public ArgumentMatcher(Type matchType, Func<object?, bool> predicate, string description)
        {
            MatchType = matchType;
            Predicate = predicate;
            Description = description;
        }
        
        public bool Matches(object? value)
        {
            // Check type compatibility first
            if (value != null && !MatchType.IsAssignableFrom(value.GetType()))
                return false;
            
            // Always run the predicate - it decides whether to accept null
            return Predicate(value);
        }
    }

    public class SetupContext<T, TResult>
    {
        public MockHandler Handler { get; }
        public string Signature { get; }
        public object?[] Args { get; }
        internal MockSetup? Setup { get; set; }
        
        public SetupContext(MockHandler handler, string signature, object?[] args) 
        { 
            Handler = handler; 
            Signature = signature; 
            Args = args; 
        }
    }

    /// <summary>
    /// Represents the context for configuring a void method setup.
    /// </summary>
    public class VoidSetupContext<T>
    {
        public MockHandler Handler { get; }
        public string Signature { get; }
        public object?[] Args { get; }
        internal MockSetup? Setup { get; set; }
        
        public VoidSetupContext(MockHandler handler, string signature, object?[] args) 
        { 
            Handler = handler; 
            Signature = signature; 
            Args = args; 
        }
    }

    /// <summary>
    /// Represents the context for configuring a sequence of return values.
    /// </summary>
    public class SequenceSetupContext<TMock, TResult>
    {
        public MockHandler Handler { get; }
        public string Signature { get; }
        public object?[] Args { get; }
        internal MockSetup? Setup { get; set; }
        private readonly List<object?> _values = new();
        
        public SequenceSetupContext(MockHandler handler, string signature, object?[] args) 
        { 
            Handler = handler; 
            Signature = signature; 
            Args = args; 
        }
        
        /// <summary>
        /// Adds the next return value in the sequence.
        /// </summary>
        /// <param name="value">The value to return on the next invocation</param>
        /// <returns>The context to continue configuring the sequence</returns>
        /// <example>
        /// mock.SetupSequence(x => x.GetNext()).Returns(1).Returns(2).Returns(3);
        /// </example>
        public SequenceSetupContext<TMock, TResult> Returns(TResult value)
        {
            _values.Add(value);
            
            // Create or update the setup with the sequential values array
            if (Setup == null)
            {
                Setup = Handler.AddSetup(Signature, Args, default(TResult), null);
                Setup.SequentialValues = _values.ToArray();
            }
            else
            {
                Setup.SequentialValues = _values.ToArray();
            }
            
            return this;
        }
        
        /// <summary>
        /// Specifies that the sequence should throw an exception on the next invocation.
        /// </summary>
        /// <param name="exception">The exception to throw</param>
        /// <returns>The context to continue configuring the sequence</returns>
        /// <example>
        /// mock.SetupSequence(x => x.GetNext()).Returns(1).Throws(new InvalidOperationException());
        /// </example>
        public SequenceSetupContext<TMock, TResult> Throws(Exception exception)
        {
            _values.Add(new SequenceException(exception));
            
            if (Setup == null)
            {
                Setup = Handler.AddSetup(Signature, Args, default(TResult), null);
                Setup.SequentialValues = _values.ToArray();
            }
            else
            {
                Setup.SequentialValues = _values.ToArray();
            }
            
            return this;
        }
    }
    
    /// <summary>
    /// Marker class to indicate an exception should be thrown in a sequence.
    /// </summary>
    internal class SequenceException
    {
        public Exception Exception { get; }
        public SequenceException(Exception exception) => Exception = exception;
    }

    /// <summary>
    /// Extension methods for configuring setup contexts.
    /// Note: Additional overloads for 4+ arguments are generated at compile-time.
    /// </summary>
    public static partial class SetupContextExtensions
    {
        /// <summary>
        /// Configures the setup to return the specified value when invoked.
        /// </summary>
        public static SetupContext<TMock, TResult> Returns<TMock, TResult>(this SetupContext<TMock, TResult> context, TResult value)
        {
            if (context.Setup == null)
            {
                // Create new setup
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, value, null);
            }
            else
            {
                // Update existing setup with static value and clear factory
                context.Setup.Value = value;
                context.Setup.ValueFactory = null;
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a value computed by a function when invoked.
        /// </summary>
        /// <param name="valueFunction">Function that computes the return value</param>
        /// <example>
        /// mock.Setup(x => x.GetValue()).Returns(() => DateTime.Now.Ticks);
        /// </example>
        public static SetupContext<TMock, TResult> Returns<TMock, TResult>(this SetupContext<TMock, TResult> context, Func<TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                // Create new setup with value factory
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
                context.Setup.ValueFactory = _ => valueFunction();
            }
            else
            {
                // Update existing setup
                context.Setup.ValueFactory = _ => valueFunction();
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a value computed from the method argument.
        /// </summary>
        /// <param name="valueFunction">Function that takes the method argument and computes the return value</param>
        /// <example>
        /// mock.Setup(x => x.Double(42)).Returns((int x) => x * 2);
        /// </example>
        public static SetupContext<TMock, TResult> Returns<TMock, TResult, TArg>(this SetupContext<TMock, TResult> context, Func<TArg, TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                // Create new setup with value factory
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
                context.Setup.ValueFactory = args => valueFunction((TArg)args[0]!);
            }
            else
            {
                // Update existing setup
                context.Setup.ValueFactory = args => valueFunction((TArg)args[0]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a value computed from two method arguments.
        /// </summary>
        public static SetupContext<TMock, TResult> Returns<TMock, TResult, TArg1, TArg2>(this SetupContext<TMock, TResult> context, Func<TArg1, TArg2, TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
                context.Setup.ValueFactory = args => valueFunction((TArg1)args[0]!, (TArg2)args[1]!);
            }
            else
            {
                context.Setup.ValueFactory = args => valueFunction((TArg1)args[0]!, (TArg2)args[1]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a value computed from three method arguments.
        /// </summary>
        public static SetupContext<TMock, TResult> Returns<TMock, TResult, TArg1, TArg2, TArg3>(this SetupContext<TMock, TResult> context, Func<TArg1, TArg2, TArg3, TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
                context.Setup.ValueFactory = args => valueFunction((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!);
            }
            else
            {
                context.Setup.ValueFactory = args => valueFunction((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!);
            }
            return context;
        }
        
        // Note: Returns<T1..T4> through Returns<T1..T8> are generated at compile-time
        // See SetupExtensionsGenerator.cs
        
        /// <summary>
        /// Configures the setup to return a Task<TResult> with the specified value (async shorthand).
        /// </summary>
        /// <param name="value">The value to wrap in a completed Task</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(x => x.GetDataAsync()).ReturnsAsync("result");
        /// // Equivalent to: mock.Setup(x => x.GetDataAsync()).Returns(Task.FromResult("result"));
        /// </example>
        public static SetupContext<TMock, System.Threading.Tasks.Task<TResult>> ReturnsAsync<TMock, TResult>(
            this SetupContext<TMock, System.Threading.Tasks.Task<TResult>> context, TResult value)
        {
            var task = System.Threading.Tasks.Task.FromResult(value);
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, task, null);
            }
            else
            {
                context.Setup.Value = task;
                context.Setup.ValueFactory = null;
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a Task<TResult> computed by a function (async shorthand).
        /// </summary>
        /// <param name="valueFunction">Function that computes the result value</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(x => x.GetTimestampAsync()).ReturnsAsync(() => DateTime.Now.Ticks);
        /// </example>
        public static SetupContext<TMock, System.Threading.Tasks.Task<TResult>> ReturnsAsync<TMock, TResult>(
            this SetupContext<TMock, System.Threading.Tasks.Task<TResult>> context, Func<TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(System.Threading.Tasks.Task<TResult>), null);
                context.Setup.ValueFactory = _ => System.Threading.Tasks.Task.FromResult(valueFunction());
            }
            else
            {
                context.Setup.ValueFactory = _ => System.Threading.Tasks.Task.FromResult(valueFunction());
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a Task<TResult> computed from a method argument (async shorthand).
        /// </summary>
        /// <param name="valueFunction">Function that takes the method argument and computes the result</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(x => x.ProcessAsync(42)).ReturnsAsync((int x) => x * 2);
        /// </example>
        public static SetupContext<TMock, System.Threading.Tasks.Task<TResult>> ReturnsAsync<TMock, TResult, TArg>(
            this SetupContext<TMock, System.Threading.Tasks.Task<TResult>> context, Func<TArg, TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(System.Threading.Tasks.Task<TResult>), null);
                context.Setup.ValueFactory = args => System.Threading.Tasks.Task.FromResult(valueFunction((TArg)args[0]!));
            }
            else
            {
                context.Setup.ValueFactory = args => System.Threading.Tasks.Task.FromResult(valueFunction((TArg)args[0]!));
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return a Task<TResult> computed from two method arguments (async shorthand).
        /// </summary>
        /// <param name="valueFunction">Function that takes two method arguments and computes the result</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(x => x.AddAsync(10, 20)).ReturnsAsync((int a, int b) => a + b);
        /// </example>
        public static SetupContext<TMock, System.Threading.Tasks.Task<TResult>> ReturnsAsync<TMock, TResult, TArg1, TArg2>(
            this SetupContext<TMock, System.Threading.Tasks.Task<TResult>> context, Func<TArg1, TArg2, TResult> valueFunction)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(System.Threading.Tasks.Task<TResult>), null);
                context.Setup.ValueFactory = args => System.Threading.Tasks.Task.FromResult(valueFunction((TArg1)args[0]!, (TArg2)args[1]!));
            }
            else
            {
                context.Setup.ValueFactory = args => System.Threading.Tasks.Task.FromResult(valueFunction((TArg1)args[0]!, (TArg2)args[1]!));
            }
            return context;
        }
        
        // Note: ReturnsAsync<T1..T3> through ReturnsAsync<T1..T8> are generated at compile-time
        // See SetupExtensionsGenerator.cs
        
        /// <summary>
        /// Configures the setup to return values in sequence on successive calls.
        /// </summary>
        /// <param name="values">Values to return in order on successive calls</param>
        /// <example>
        /// mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second", "third");
        /// </example>
        public static SetupContext<TMock, TResult> ReturnsInOrder<TMock, TResult>(this SetupContext<TMock, TResult> context, params TResult[] values)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
                context.Setup.SequentialValues = values.Cast<object?>().ToArray();
            }
            else
            {
                // Clear other return configurations
                context.Setup.Value = default(TResult);
                context.Setup.ValueFactory = null;
                context.Setup.SequentialValues = values.Cast<object?>().ToArray();
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to return values in sequence on successive calls.
        /// </summary>
        /// <param name="values">Enumerable of values to return in order on successive calls</param>
        /// <example>
        /// mock.Setup(x => x.GetNext()).ReturnsInOrder(new[] { "first", "second", "third" });
        /// </example>
        public static SetupContext<TMock, TResult> ReturnsInOrder<TMock, TResult>(this SetupContext<TMock, TResult> context, IEnumerable<TResult> values)
        {
            return ReturnsInOrder(context, values.ToArray());
        }
        
        /// <summary>
        /// Configures a callback to execute when the method is invoked.
        /// </summary>
        /// <param name="callback">Action to execute on invocation</param>
        /// <example>
        /// mock.Setup(x => x.Execute()).Callback(() => Console.WriteLine("Called"));
        /// </example>
        public static SetupContext<TMock, TResult> Callback<TMock, TResult>(this SetupContext<TMock, TResult> context, Action callback)
        {
            if (context.Setup == null)
            {
                // Create new setup with callback
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult)!, _ => callback());
            }
            else
            {
                // Update existing setup
                context.Setup.Callback = _ => callback();
            }
            return context;
        }
        
        /// <summary>
        /// Configures a callback with access to method arguments when invoked.
        /// </summary>
        /// <param name="callback">Action to execute with arguments on invocation</param>
        /// <example>
        /// mock.Setup(x => x.Process(It.IsAny&lt;int&gt;())).Callback&lt;int&gt;(value => Console.WriteLine($"Processing {value}"));
        /// </example>
        public static SetupContext<TMock, TResult> Callback<TMock, TResult, TArg>(this SetupContext<TMock, TResult> context, Action<TArg> callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult)!, args => callback((TArg)args[0]!));
            }
            else
            {
                context.Setup.Callback = args => callback((TArg)args[0]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures a callback with access to two method arguments when invoked.
        /// </summary>
        public static SetupContext<TMock, TResult> Callback<TMock, TResult, TArg1, TArg2>(this SetupContext<TMock, TResult> context, Action<TArg1, TArg2> callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult)!, args => callback((TArg1)args[0]!, (TArg2)args[1]!));
            }
            else
            {
                context.Setup.Callback = args => callback((TArg1)args[0]!, (TArg2)args[1]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures a callback with access to three method arguments when invoked.
        /// </summary>
        public static SetupContext<TMock, TResult> Callback<TMock, TResult, TArg1, TArg2, TArg3>(this SetupContext<TMock, TResult> context, Action<TArg1, TArg2, TArg3> callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult)!, args => callback((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!));
            }
            else
            {
                context.Setup.Callback = args => callback((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!);
            }
            return context;
        }
        
        // Callback extensions for void methods
        
        /// <summary>
        /// Configures a callback to execute when a void method is invoked.
        /// </summary>
        /// <param name="callback">Action to execute on invocation</param>
        /// <example>
        /// mock.Setup(x => x.Execute()).Callback(() => Console.WriteLine("Called"));
        /// </example>
        public static VoidSetupContext<TMock> Callback<TMock>(this VoidSetupContext<TMock> context, Action callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, _ => callback());
            }
            else
            {
                context.Setup.Callback = _ => callback();
            }
            return context;
        }
        
        /// <summary>
        /// Configures a callback with access to method arguments when a void method is invoked.
        /// </summary>
        /// <param name="callback">Action to execute with arguments on invocation</param>
        /// <example>
        /// mock.Setup(x => x.Process(It.IsAny&lt;int&gt;())).Callback&lt;int&gt;(value => Console.WriteLine($"Processing {value}"));
        /// </example>
        public static VoidSetupContext<TMock> Callback<TMock, TArg>(this VoidSetupContext<TMock> context, Action<TArg> callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, args => callback((TArg)args[0]!));
            }
            else
            {
                context.Setup.Callback = args => callback((TArg)args[0]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures a callback with access to two method arguments when a void method is invoked.
        /// </summary>
        public static VoidSetupContext<TMock> Callback<TMock, TArg1, TArg2>(this VoidSetupContext<TMock> context, Action<TArg1, TArg2> callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, args => callback((TArg1)args[0]!, (TArg2)args[1]!));
            }
            else
            {
                context.Setup.Callback = args => callback((TArg1)args[0]!, (TArg2)args[1]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures a callback with access to three method arguments when a void method is invoked.
        /// </summary>
        public static VoidSetupContext<TMock> Callback<TMock, TArg1, TArg2, TArg3>(this VoidSetupContext<TMock> context, Action<TArg1, TArg2, TArg3> callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, args => callback((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!));
            }
            else
            {
                context.Setup.Callback = args => callback((TArg1)args[0]!, (TArg2)args[1]!, (TArg3)args[2]!);
            }
            return context;
        }
        
        /// <summary>
        /// Configures the setup to automatically raise the specified event when the method is invoked.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="eventName">The name of the event to raise (use nameof for type safety)</param>
        /// <param name="args">The event arguments to pass when raising the event</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(m => m.Start())
        ///     .Raises(nameof(IServiceWithEvents.Completed), EventArgs.Empty);
        /// </example>
        public static SetupContext<TMock, TResult> Raises<TMock, TResult>(
            this SetupContext<TMock, TResult> context, 
            string eventName, 
            params object?[] args)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
            }
            
            // Store event info in setup
            context.Setup.EventToRaise = eventName;
            context.Setup.EventArgs = args;
            
            return context;
        }
        
        /// <summary>
        /// Configures the setup to automatically raise the specified event when a void method is invoked.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="eventName">The name of the event to raise (use nameof for type safety)</param>
        /// <param name="args">The event arguments to pass when raising the event</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(m => m.Process())
        ///     .Raises(nameof(IServiceWithEvents.StatusChanged), new StatusEventArgs { Status = "Done" });
        /// </example>
        public static VoidSetupContext<TMock> Raises<TMock>(
            this VoidSetupContext<TMock> context, 
            string eventName, 
            params object?[] args)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            // Store event info in setup
            context.Setup.EventToRaise = eventName;
            context.Setup.EventArgs = args;
            
            return context;
        }
        
        /// <summary>
        /// Configures this setup to be part of a sequence, ensuring it's called in order.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="sequence">The MockSequence tracking the order</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// var sequence = new MockSequence();
        /// mock.Setup(m => m.First()).InSequence(sequence);
        /// mock.Setup(m => m.Second()).InSequence(sequence);
        /// </example>
        public static SetupContext<TMock, TResult> InSequence<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            MockSequence sequence)
        {
            if (context.Setup == null)
            {
                throw new InvalidOperationException("InSequence must be called after Returns/Callback");
            }
            
            context.Setup.Sequence = sequence;
            context.Setup.SequenceStep = sequence.RegisterStep();
            
            return context;
        }
        
        /// <summary>
        /// Configures this setup to be part of a sequence, ensuring it's called in order.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="sequence">The MockSequence tracking the order</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// var sequence = new MockSequence();
        /// mock.Setup(m => m.DoWork()).InSequence(sequence);
        /// mock.Setup(m => m.Finish()).InSequence(sequence);
        /// </example>
        public static VoidSetupContext<TMock> InSequence<TMock>(
            this VoidSetupContext<TMock> context,
            MockSequence sequence)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.Sequence = sequence;
            context.Setup.SequenceStep = sequence.RegisterStep();
            
            return context;
        }
        
        /// <summary>
        /// Configures a callback with support for ref/out parameters.
        /// The callback delegate can have ref/out parameters matching the mocked method signature.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="callback">Delegate with ref/out parameters matching the method signature</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(m => m.TryParse(It.IsAny&lt;string&gt;(), out dummy))
        ///     .Returns(true)
        ///     .CallbackRefOut((TryParseCallback)((string input, out int result) => result = int.Parse(input)));
        /// </example>
        public static SetupContext<TMock, TResult> CallbackRefOut<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            Delegate callback)
        {
            if (context.Setup == null)
            {
                throw new InvalidOperationException("CallbackRefOut must be called after Returns");
            }
            
            context.Setup.RefOutCallback = callback;
            
            // Mark parameters as ref/out for matching (analyze delegate signature)
            var method = callback.GetType().GetMethod("Invoke");
            if (method != null)
            {
                context.Setup.RefOutParameterIndices ??= new HashSet<int>();
                var parameters = method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef)
                    {
                        context.Setup.RefOutParameterIndices.Add(i);
                    }
                }
            }
            
            return context;
        }
        
        /// <summary>
        /// Configures a callback with support for ref/out parameters for void methods.
        /// The callback delegate can have ref/out parameters matching the mocked method signature.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="callback">Delegate with ref/out parameters matching the method signature</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(m => m.ProcessValue(It.IsAny&lt;string&gt;(), out dummy))
        ///     .CallbackRefOut((ProcessCallback)((string input, out int result) => result = input.Length));
        /// </example>
        public static VoidSetupContext<TMock> CallbackRefOut<TMock>(
            this VoidSetupContext<TMock> context,
            Delegate callback)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.RefOutCallback = callback;
            
            // Mark parameters as ref/out for matching (analyze delegate signature)
            var method = callback.GetType().GetMethod("Invoke");
            if (method != null)
            {
                context.Setup.RefOutParameterIndices ??= new HashSet<int>();
                var parameters = method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef)
                    {
                        context.Setup.RefOutParameterIndices.Add(i);
                    }
                }
            }
            
            return context;
        }
        
        /// <summary>
        /// Configures an out parameter value for this setup.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the out parameter</param>
        /// <param name="value">The value to assign to the out parameter</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// int dummy = 0;
        /// mock.Setup(m => m.TryParse("5", out dummy))
        ///     .Returns(true)
        ///     .OutValue(1, 5);
        /// </example>
        public static SetupContext<TMock, TResult> OutValue<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            int parameterIndex,
            object? value)
        {
            if (context.Setup == null)
            {
                throw new InvalidOperationException("OutValue must be called after Returns/Callback");
            }
            
            context.Setup.OutValues ??= new Dictionary<int, object?>();
            context.Setup.OutValues[parameterIndex] = value;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures a ref parameter value for this setup.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the ref parameter</param>
        /// <param name="value">The value to assign to the ref parameter</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// int dummy = 0;
        /// mock.Setup(m => m.ModifyValue(ref dummy))
        ///     .RefValue(0, 10);
        /// </example>
        public static SetupContext<TMock, TResult> RefValue<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            int parameterIndex,
            object? value)
        {
            if (context.Setup == null)
            {
                throw new InvalidOperationException("RefValue must be called after Returns/Callback");
            }
            
            context.Setup.RefValues ??= new Dictionary<int, object?>();
            context.Setup.RefValues[parameterIndex] = value;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures an out parameter value for this void setup.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the out parameter</param>
        /// <param name="value">The value to assign to the out parameter</param>
        /// <returns>The setup context for further configuration</returns>
        public static VoidSetupContext<TMock> OutValue<TMock>(
            this VoidSetupContext<TMock> context,
            int parameterIndex,
            object? value)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.OutValues ??= new Dictionary<int, object?>();
            context.Setup.OutValues[parameterIndex] = value;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures a ref parameter value for this void setup.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the ref parameter</param>
        /// <param name="value">The value to assign to the ref parameter</param>
        /// <returns>The setup context for further configuration</returns>
        public static VoidSetupContext<TMock> RefValue<TMock>(
            this VoidSetupContext<TMock> context,
            int parameterIndex,
            object? value)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.RefValues ??= new Dictionary<int, object?>();
            context.Setup.RefValues[parameterIndex] = value;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures an out parameter with a dynamic value computed from the method arguments.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the out parameter</param>
        /// <param name="valueFactory">Function that computes the out value from the method arguments</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// int dummy = 0;
        /// mock.Setup(m => m.TryParse(It.IsAny&lt;string&gt;(), out dummy))
        ///     .Returns(true)
        ///     .OutValueFunc(1, args => int.Parse((string)args[0]!));
        /// </example>
        public static SetupContext<TMock, TResult> OutValueFunc<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            int parameterIndex,
            Func<object?[], object?> valueFactory)
        {
            if (context.Setup == null)
            {
                throw new InvalidOperationException("OutValueFunc must be called after Returns/Callback");
            }
            
            context.Setup.OutValueFactories ??= new Dictionary<int, Func<object?[], object?>>();
            context.Setup.OutValueFactories[parameterIndex] = valueFactory;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures a ref parameter with a dynamic value computed from the method arguments.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the ref parameter</param>
        /// <param name="valueFactory">Function that computes the ref value from the method arguments</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// int dummy = 0;
        /// mock.Setup(m => m.ModifyValue(ref dummy))
        ///     .RefValueFunc(0, args => (int)args[0]! * 2);
        /// </example>
        public static SetupContext<TMock, TResult> RefValueFunc<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            int parameterIndex,
            Func<object?[], object?> valueFactory)
        {
            if (context.Setup == null)
            {
                throw new InvalidOperationException("RefValueFunc must be called after Returns/Callback");
            }
            
            context.Setup.RefValueFactories ??= new Dictionary<int, Func<object?[], object?>>();
            context.Setup.RefValueFactories[parameterIndex] = valueFactory;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures an out parameter with a dynamic value for void setups.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the out parameter</param>
        /// <param name="valueFactory">Function that computes the out value from the method arguments</param>
        /// <returns>The setup context for further configuration</returns>
        public static VoidSetupContext<TMock> OutValueFunc<TMock>(
            this VoidSetupContext<TMock> context,
            int parameterIndex,
            Func<object?[], object?> valueFactory)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.OutValueFactories ??= new Dictionary<int, Func<object?[], object?>>();
            context.Setup.OutValueFactories[parameterIndex] = valueFactory;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
        
        /// <summary>
        /// Configures a ref parameter with a dynamic value for void setups.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="parameterIndex">The zero-based index of the ref parameter</param>
        /// <param name="valueFactory">Function that computes the ref value from the method arguments</param>
        /// <returns>The setup context for further configuration</returns>
        public static VoidSetupContext<TMock> RefValueFunc<TMock>(
            this VoidSetupContext<TMock> context,
            int parameterIndex,
            Func<object?[], object?> valueFactory)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.RefValueFactories ??= new Dictionary<int, Func<object?[], object?>>();
            context.Setup.RefValueFactories[parameterIndex] = valueFactory;
            
            // Mark this parameter as ref/out so matching ignores its value
            context.Setup.RefOutParameterIndices ??= new HashSet<int>();
            context.Setup.RefOutParameterIndices.Add(parameterIndex);
            
            return context;
        }
    }

    public interface IMockSetup { MockHandler Handler { get; } }
    
    /// <summary>
    /// Interface for setting up protected members on mocks.
    /// </summary>
    public interface IProtectedMockSetup
    {
        MockHandler Handler { get; }
        ProtectedSetupContext<TResult> Setup<TResult>(string methodName, params object?[] args);
        ProtectedVoidSetupContext Setup(string methodName, params object?[] args);
        ProtectedSetupContext<TResult> SetupGet<TResult>(string propertyName);
    }

    public class MockHandler
    {
        private readonly List<MockSetup> _setups = new();
        private readonly List<Invocation> _invocations = new();
        private ChaosPolicy? _chaosPolicy;
        private Random _rng = new();
        private readonly ChaosStatistics _chaosStats = new();
        
        // Property backing store for SetupProperty
        private readonly Dictionary<string, object?> _propertyStorage = new();
        
        // Event handler storage for Raise
        private readonly Dictionary<string, List<Delegate>> _eventHandlers = new();
        
        // Additional interfaces added via As<T>()
        private readonly HashSet<Type> _additionalInterfaces = new();
        
        // Default value provider for un-setup members
        private DefaultValueProvider? _defaultValueProvider;
        private DefaultValue? _explicitDefaultValueStrategy = null; // Track if user explicitly set strategy
        
        public MockBehavior Behavior { get; set; } = MockBehavior.Loose;
        
        /// <summary>
        /// Gets or sets the default value strategy. When set, creates appropriate provider.
        /// </summary>
        public DefaultValue DefaultValueStrategy 
        { 
            get => _explicitDefaultValueStrategy ?? DefaultValue.Empty; 
            set => _explicitDefaultValueStrategy = value; 
        }
        
        /// <summary>
        /// Gets or sets a custom default value provider. Takes precedence over DefaultValueStrategy.
        /// </summary>
        public DefaultValueProvider? DefaultValueProvider 
        { 
            get => _defaultValueProvider; 
            set => _defaultValueProvider = value;
        }
        
        /// <summary>
        /// Gets statistics about chaos mode behavior during test execution.
        /// </summary>
        public ChaosStatistics ChaosStatistics => _chaosStats;
        
        /// <summary>
        /// Adds an additional interface that the mock should implement.
        /// Called by the As<T>() extension method.
        /// </summary>
        /// <param name="interfaceType">The interface type to add</param>
        public void AddInterface(Type interfaceType)
        {
            lock (_additionalInterfaces)
            {
                _additionalInterfaces.Add(interfaceType);
            }
        }
        
        /// <summary>
        /// Gets all additional interfaces that have been added to the mock via As<T>().
        /// Used by the generator to create composite mock classes.
        /// </summary>
        /// <returns>Read-only set of additional interface types</returns>
        public IReadOnlySet<Type> GetAdditionalInterfaces()
        {
            lock (_additionalInterfaces)
            {
                return new HashSet<Type>(_additionalInterfaces);
            }
        }

        public MockSetup AddSetup(string signature, object?[] args, object? value, Action<object?[]>? callback = null)
        {
            var setup = new MockSetup(signature, args, value, callback);
            _setups.Add(setup);
            return setup;
        }
        
        public void AddCallbackToLastSetup(string signature, object?[] args, Action<object?[]> callback)
        {
            var setup = _setups.LastOrDefault(s => s.Matches(signature, args));
            if (setup != null)
            {
                setup.Callback = callback;
            }
            else
            {
                // If no matching setup exists, create one with just the callback
                _setups.Add(new MockSetup(signature, args, null, callback));
            }
        }
        
        public void AddCallbackToLastSetup(string signature, object?[] args, Action callback)
        {
            AddCallbackToLastSetup(signature, args, _ => callback());
        }
        
        public void SetChaosPolicy(ChaosPolicy policy) 
        { 
            _chaosPolicy = policy;
            // Initialize RNG with seed if provided
            if (policy.Seed.HasValue)
                _rng = new Random(policy.Seed.Value);
        }
        
        /// <summary>
        /// Sets up a property with a backing field for automatic get/set tracking.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="defaultValue">The default value for the property</param>
        public void SetupPropertyStorage(string propertyName, object? defaultValue)
        {
            _propertyStorage[propertyName] = defaultValue;
        }
        
        /// <summary>
        /// Gets a property value from the backing store.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>The stored value, or null if not setup</returns>
        public object? GetPropertyValue(string propertyName)
        {
            return _propertyStorage.TryGetValue(propertyName, out var value) ? value : null;
        }
        
        /// <summary>
        /// Sets a property value in the backing store.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="value">The value to store</param>
        public void SetPropertyValue(string propertyName, object? value)
        {
            _propertyStorage[propertyName] = value;
        }
        
        /// <summary>
        /// Checks if a property has been setup with backing storage.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>True if property has backing storage</returns>
        public bool HasPropertyStorage(string propertyName)
        {
            return _propertyStorage.ContainsKey(propertyName);
        }
        
        /// <summary>
        /// Adds an event handler to the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">The delegate handler to add</param>
        public void AddEventHandler(string eventName, Delegate handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);
            
            // Track event subscription as an invocation
            _invocations.Add(new Invocation("add_" + eventName, new object?[] { handler }));
        }
        
        /// <summary>
        /// Removes an event handler from the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">The delegate handler to remove</param>
        public void RemoveEventHandler(string eventName, Delegate handler)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }
            
            // Track event unsubscription as an invocation
            _invocations.Add(new Invocation("remove_" + eventName, new object?[] { handler }));
        }
        
        /// <summary>
        /// Raises the specified event with the given arguments.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments to pass to subscribers</param>
        public void RaiseEvent(string eventName, params object?[] args)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                // Invoke all subscribed handlers
                foreach (var handler in handlers.ToList()) // ToList() to avoid modification during enumeration
                {
                    try
                    {
                        handler.DynamicInvoke(args);
                    }
                    catch (Exception ex)
                    {
                        // Re-throw the inner exception if available (unwrap TargetInvocationException)
                        if (ex.InnerException != null)
                            throw ex.InnerException;
                        throw;
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets all invocations for verification.
        /// </summary>
        public IReadOnlyList<Invocation> Invocations => _invocations;

        public object? Invoke(string signature, object?[] args)
        {
            // Track the invocation
            _invocations.Add(new Invocation(signature, args));
            
            // Apply chaos if configured
            if (_chaosPolicy != null)
            {
                _chaosStats.TotalInvocations++;
                
                // Simulate timeout/delay if configured
                if (_chaosPolicy.TimeoutMilliseconds > 0)
                {
                    _chaosStats.TimeoutTriggeredCount++;
                    System.Threading.Thread.Sleep(_chaosPolicy.TimeoutMilliseconds);
                }
                
                // Trigger failure based on failure rate
                if (_rng.NextDouble() < _chaosPolicy.FailureRate)
                {
                    _chaosStats.ChaosTriggeredCount++;
                    
                    // Only throw if exceptions are configured
                    if (_chaosPolicy.PossibleExceptions?.Length > 0)
                        throw _chaosPolicy.PossibleExceptions[_rng.Next(_chaosPolicy.PossibleExceptions.Length)];
                }
            }

            foreach (var setup in _setups)
            {
                if (setup.Matches(signature, args))
                {
                    // Check sequence order if this setup is part of a sequence
                    if (setup.Sequence != null)
                    {
                        setup.Sequence.RecordInvocation(setup.SequenceStep, signature);
                    }
                    
                    // Execute callback if present
                    setup.ExecuteCallback(args);
                    
                    // Raise event if configured
                    if (setup.EventToRaise != null && setup.EventArgs != null)
                    {
                        RaiseEvent(setup.EventToRaise, setup.EventArgs);
                    }
                    
                    // If this setup has out/ref parameters (static or dynamic) or callback, return the setup itself
                    // so the generated code can extract and apply those values or invoke the callback
                    if (setup.OutValues != null || setup.RefValues != null || 
                        setup.OutValueFactories != null || setup.RefValueFactories != null ||
                        setup.RefOutCallback != null)
                    {
                        return setup;
                    }
                    
                    // Return sequential value if present, otherwise factory, otherwise static value
                    if (setup.SequentialValues != null)
                        return setup.GetNextSequentialValue();
                    
                    return setup.ValueFactory != null ? setup.ValueFactory(args) : setup.Value;
                }
            }

            if (Behavior == MockBehavior.Strict)
                throw new MockException($"[Strict Mode] Call to '{signature}' was not setup.");

            // Return null in Loose mode - generator will use GetDefaultValueFor<T> for typed defaults
            return null; 
        }
        
        /// <summary>
        /// Gets the default value for a specific type using the configured default value provider.
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="mock">The mock instance (for recursive mocking)</param>
        /// <returns>Default value for the type</returns>
        public T? GetDefaultValueFor<T>(object mock)
        {
            // Special handling for Task and Task<T> - always return completed tasks
            var type = typeof(T);
            if (type == typeof(System.Threading.Tasks.Task))
            {
                return (T)(object)System.Threading.Tasks.Task.CompletedTask;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>))
            {
                var resultType = type.GetGenericArguments()[0];
                var fromResultMethod = typeof(System.Threading.Tasks.Task).GetMethod("FromResult")!.MakeGenericMethod(resultType);
                var result = fromResultMethod.Invoke(null, new object?[] { GetDefaultForType(resultType, mock) });
                return (T)result!;
            }
            
            // If custom provider is set, use it
            if (_defaultValueProvider != null)
            {
                var value = _defaultValueProvider.GetDefaultValue(typeof(T), mock);
                return value == null ? default(T) : (T)value;
            }
            
            // If no explicit strategy was set, return CLR defaults for backwards compatibility
            if (_explicitDefaultValueStrategy == null)
            {
                return default(T);
            }
            
            // Otherwise use strategy-based provider
            DefaultValueProvider provider = _explicitDefaultValueStrategy == DefaultValue.Mock 
                ? new MockDefaultValueProvider() 
                : new EmptyDefaultValueProvider();
            
            var providerResult = provider.GetDefaultValue(typeof(T), mock);
            return providerResult == null ? default(T) : (T)providerResult;
        }
        
        // Helper method to get default value for any type, used by Task<T> handling
        // DynamicallyAccessedMembers ensures AOT compatibility when creating value types
        private object? GetDefaultForType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] 
            Type type, object mock)
        {
            if (_defaultValueProvider != null)
            {
                return _defaultValueProvider.GetDefaultValue(type, mock);
            }
            
            if (_explicitDefaultValueStrategy == null)
            {
                // AOT-safe: DynamicallyAccessedMembers attribute ensures constructor is preserved
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
            
            DefaultValueProvider provider = _explicitDefaultValueStrategy == DefaultValue.Mock 
                ? new MockDefaultValueProvider() 
                : new EmptyDefaultValueProvider();
            
            return provider.GetDefaultValue(type, mock);
        }
    }
    
    /// <summary>
    /// Represents a recorded method invocation on a mock.
    /// </summary>
    public class Invocation
    {
        public string Signature { get; }
        public object?[] Args { get; }
        
        public Invocation(string signature, object?[] args)
        {
            Signature = signature;
            Args = args;
        }
        
        public bool Matches(string signature, object?[] args)
        {
            if (Signature != signature || Args.Length != args.Length) return false;
            
            for (int i = 0; i < Args.Length; i++)
            {
                // Check if the expected arg is a matcher
                if (args[i] is ArgumentMatcher matcher)
                {
                    if (!matcher.Matches(Args[i]))
                        return false;
                }
                else
                {
                    // Standard equality check
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
    public class Times
    {
        private readonly Func<int, bool> _validator;
        
        /// <summary>
        /// Gets a human-readable description of the expected call count.
        /// </summary>
        public string Description { get; }
        
        private Times(Func<int, bool> validator, string description)
        {
            _validator = validator;
            Description = description;
        }
        
        /// <summary>
        /// Validates if the actual call count matches the expectation.
        /// </summary>
        public bool Validate(int actualCalls) => _validator(actualCalls);
        
        /// <summary>
        /// Expects exactly one call.
        /// </summary>
        public static Times Once() => new Times(c => c == 1, "exactly 1");
        
        /// <summary>
        /// Expects no calls.
        /// </summary>
        public static Times Never() => new Times(c => c == 0, "exactly 0");
        
        /// <summary>
        /// Expects exactly n calls.
        /// </summary>
        public static Times Exactly(int callCount) => new Times(c => c == callCount, $"exactly {callCount}");
        
        /// <summary>
        /// Expects at least n calls.
        /// </summary>
        public static Times AtLeast(int callCount) => new Times(c => c >= callCount, $"at least {callCount}");
        
        /// <summary>
        /// Expects at most n calls.
        /// </summary>
        public static Times AtMost(int callCount) => new Times(c => c <= callCount, $"at most {callCount}");
        
        /// <summary>
        /// Expects between min and max calls (inclusive).
        /// </summary>
        public static Times Between(int callCountFrom, int callCountTo) => 
            new Times(c => c >= callCountFrom && c <= callCountTo, $"between {callCountFrom} and {callCountTo}");
    }

    public class MockSetup 
    {
        public string Signature { get; }
        public object?[] Args { get; }
        public object? Value { get; set; }
        public Func<object?[], object?>? ValueFactory { get; set; }
        public Action<object?[]>? Callback { get; set; }
        public Delegate? RefOutCallback { get; set; }  // Callback with ref/out parameters
        public object?[]? SequentialValues { get; set; }
        private int _sequentialIndex = 0;
        
        // Event raising support
        public string? EventToRaise { get; set; }
        public object?[]? EventArgs { get; set; }
        
        // Sequence support
        public MockSequence? Sequence { get; set; }
        public int SequenceStep { get; set; }
        
        // Out/Ref parameter support
        public Dictionary<int, object?>? OutValues { get; set; }
        public Dictionary<int, object?>? RefValues { get; set; }
        public Dictionary<int, Func<object?[], object?>>? OutValueFactories { get; set; }  // Dynamic out values
        public Dictionary<int, Func<object?[], object?>>? RefValueFactories { get; set; }  // Dynamic ref values
        public HashSet<int>? RefOutParameterIndices { get; set; }  // Tracks which params are ref/out for matching
        
        public MockSetup(string sig, object?[] args, object? val, Action<object?[]>? callback = null) 
        { 
            Signature = sig; 
            Args = args; 
            Value = val; 
            Callback = callback;
        }

        public bool Matches(string sig, object?[] args)
        {
            if (Signature != sig || Args.Length != args.Length) return false;
            
            for(int i = 0; i < Args.Length; i++)
            {
                // Skip matching for ref/out parameters - they always match regardless of value
                if (RefOutParameterIndices != null && RefOutParameterIndices.Contains(i))
                    continue;
                
                // Check if the setup arg is a matcher
                if (Args[i] is ArgumentMatcher matcher)
                {
                    if (!matcher.Matches(args[i]))
                        return false;
                }
                else
                {
                    // Standard equality check
                    if (Args[i] != null && !Args[i]!.Equals(args[i]))
                        return false;
                }
            }
            
            return true;
        }
        
        public void ExecuteCallback(object?[] args)
        {
            Callback?.Invoke(args);
            // RefOutCallback is invoked by generated code with proper ref/out modifiers
        }
        
        public object? GetNextSequentialValue()
        {
            if (SequentialValues == null || SequentialValues.Length == 0)
                return null;
                
            var value = SequentialValues[_sequentialIndex];
            if (_sequentialIndex < SequentialValues.Length - 1)
                _sequentialIndex++;
            
            // Check if this value is an exception marker
            if (value is SequenceException seqEx)
                throw seqEx.Exception;
                
            return value;
        }
    }

    /// <summary>
    /// Configuration for chaos engineering mode in mocks.
    /// </summary>
    public class ChaosPolicy 
    { 
        /// <summary>
        /// Probability (0.0 to 1.0) that a mocked method will fail.
        /// </summary>
        public double FailureRate { get; set; }
        
        /// <summary>
        /// Array of exceptions to randomly throw when chaos triggers. If null, a generic exception is thrown.
        /// </summary>
        public Exception[]? PossibleExceptions { get; set; }
        
        /// <summary>
        /// Delay in milliseconds to simulate slow responses or timeouts. Default is 0 (no delay).
        /// </summary>
        public int TimeoutMilliseconds { get; set; }
        
        /// <summary>
        /// Seed for the random number generator. Set this for reproducible chaos scenarios in tests.
        /// If null, a random seed is used.
        /// </summary>
        public int? Seed { get; set; }
    }
    
    /// <summary>
    /// Statistics about chaos mode behavior during test execution.
    /// </summary>
    public class ChaosStatistics
    {
        /// <summary>
        /// Total number of method invocations on the mock.
        /// </summary>
        public int TotalInvocations { get; set; }
        
        /// <summary>
        /// Number of times chaos mode triggered a failure.
        /// </summary>
        public int ChaosTriggeredCount { get; set; }
        
        /// <summary>
        /// Number of times a timeout/delay was applied.
        /// </summary>
        public int TimeoutTriggeredCount { get; set; }
        
        /// <summary>
        /// Actual failure rate observed during execution.
        /// </summary>
        public double ActualFailureRate => TotalInvocations > 0 ? (double)ChaosTriggeredCount / TotalInvocations : 0;
        
        /// <summary>
        /// Resets all statistics to zero.
        /// </summary>
        public void Reset()
        {
            TotalInvocations = 0;
            ChaosTriggeredCount = 0;
            TimeoutTriggeredCount = 0;
        }
    }

    /// <summary>
    /// Utilities for asserting and monitoring memory allocations during tests.
    /// </summary>
    public static class AssertAllocations
    {
        /// <summary>
        /// Asserts that an action allocates zero bytes on the heap.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <exception cref="Exception">Thrown if any heap allocations are detected</exception>
        public static void Zero(Action action)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            long after = GC.GetAllocatedBytesForCurrentThread();
            if (after - before > 0) 
                throw new Exception($"Allocated {after - before} bytes (Expected 0).");
        }
        
        /// <summary>
        /// Asserts that an action allocates at most the specified number of bytes.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <param name="maxBytes">Maximum allowed allocation in bytes</param>
        /// <exception cref="Exception">Thrown if allocations exceed the threshold</exception>
        public static void AtMost(Action action, long maxBytes)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            long after = GC.GetAllocatedBytesForCurrentThread();
            long allocated = after - before;
            
            if (allocated > maxBytes)
                throw new Exception($"Allocated {allocated} bytes (Expected at most {maxBytes}).");
        }
        
        /// <summary>
        /// Measures allocation of an action and returns a detailed report.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <param name="actionName">Optional name for the action being measured</param>
        /// <returns>Allocation report with detailed statistics</returns>
        public static AllocationReport Measure(Action action, string actionName = "Action")
        {
            // Force GC to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long gen0Before = GC.CollectionCount(0);
            long gen1Before = GC.CollectionCount(1);
            long gen2Before = GC.CollectionCount(2);
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            action();
            sw.Stop();
            
            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            long gen0After = GC.CollectionCount(0);
            long gen1After = GC.CollectionCount(1);
            long gen2After = GC.CollectionCount(2);
            
            return new AllocationReport
            {
                ActionName = actionName,
                BytesAllocated = bytesAfter - bytesBefore,
                DurationMilliseconds = sw.ElapsedMilliseconds,
                Gen0Collections = (int)(gen0After - gen0Before),
                Gen1Collections = (int)(gen1After - gen1Before),
                Gen2Collections = (int)(gen2After - gen2Before)
            };
        }
        
        /// <summary>
        /// Configures a performance threshold for a specific action.
        /// </summary>
        /// <param name="actionName">Name of the action to monitor</param>
        /// <param name="maxBytes">Maximum allowed allocation in bytes</param>
        /// <param name="maxMilliseconds">Maximum allowed duration in milliseconds</param>
        /// <returns>Performance threshold configuration</returns>
        public static PerformanceThreshold Threshold(string actionName, long maxBytes, long maxMilliseconds)
        {
            return new PerformanceThreshold
            {
                ActionName = actionName,
                MaxBytes = maxBytes,
                MaxMilliseconds = maxMilliseconds
            };
        }
        
        /// <summary>
        /// Validates that an action meets a performance threshold.
        /// </summary>
        /// <param name="action">Action to execute and measure</param>
        /// <param name="threshold">Performance threshold to validate against</param>
        /// <exception cref="Exception">Thrown if the action exceeds the threshold</exception>
        public static void MeetsThreshold(Action action, PerformanceThreshold threshold)
        {
            var report = Measure(action, threshold.ActionName);
            
            if (report.BytesAllocated > threshold.MaxBytes)
                throw new Exception($"[{threshold.ActionName}] Allocated {report.BytesAllocated} bytes (Threshold: {threshold.MaxBytes}).");
            
            if (report.DurationMilliseconds > threshold.MaxMilliseconds)
                throw new Exception($"[{threshold.ActionName}] Took {report.DurationMilliseconds}ms (Threshold: {threshold.MaxMilliseconds}ms).");
        }
    }
    
    /// <summary>
    /// Detailed report of memory allocations and performance metrics.
    /// </summary>
    public class AllocationReport
    {
        /// <summary>
        /// Name of the action that was measured.
        /// </summary>
        public string ActionName { get; set; } = string.Empty;
        
        /// <summary>
        /// Total bytes allocated on the heap during execution.
        /// </summary>
        public long BytesAllocated { get; set; }
        
        /// <summary>
        /// Time taken to execute the action in milliseconds.
        /// </summary>
        public long DurationMilliseconds { get; set; }
        
        /// <summary>
        /// Number of generation 0 garbage collections during execution.
        /// </summary>
        public int Gen0Collections { get; set; }
        
        /// <summary>
        /// Number of generation 1 garbage collections during execution.
        /// </summary>
        public int Gen1Collections { get; set; }
        
        /// <summary>
        /// Number of generation 2 garbage collections during execution.
        /// </summary>
        public int Gen2Collections { get; set; }
        
        /// <summary>
        /// Formats the report as a human-readable string.
        /// </summary>
        public override string ToString()
        {
            return $"[{ActionName}] {BytesAllocated} bytes allocated, {DurationMilliseconds}ms duration, " +
                   $"GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections}";
        }
    }
    
    /// <summary>
    /// Performance threshold configuration for monitoring action performance.
    /// </summary>
    public class PerformanceThreshold
    {
        /// <summary>
        /// Name of the action being monitored.
        /// </summary>
        public string ActionName { get; set; } = string.Empty;
        
        /// <summary>
        /// Maximum allowed allocation in bytes.
        /// </summary>
        public long MaxBytes { get; set; }
        
        /// <summary>
        /// Maximum allowed duration in milliseconds.
        /// </summary>
        public long MaxMilliseconds { get; set; }
    }

    public static class Harness { public static TestHarness<T> Create<T>() => new TestHarness<T>(); }
    public class TestHarness<T> { public T SUT { get; protected set; } = default!; protected Dictionary<Type, object> _mocks = new(); }

    /// <summary>
    /// AutoScribe captures real method calls and generates test setup code automatically.
    /// This is a compile-time feature - the generator creates a recording proxy.
    /// </summary>
    public static class AutoScribe
    {
        /// <summary>
        /// Wraps a real implementation with a recording proxy that logs all method calls.
        /// The generator intercepts this call and generates a compile-time recording proxy.
        /// </summary>
        /// <typeparam name="T">Interface type to record</typeparam>
        /// <param name="realImplementation">The real object to wrap</param>
        /// <returns>A recording proxy that logs calls and delegates to the real implementation</returns>
        public static T Capture<T>(T realImplementation) where T : class
        {
            // NO FALLBACK: AutoScribe is compile-time only - source generator MUST intercept this call
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept AutoScribe.Capture<{typeof(T).Name}>().\n" +
                "AutoScribe is a COMPILE-TIME feature that generates recording proxies.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.\n" +
                "See: https://github.com/Digvijay/Skugga/blob/main/README.md#autoscribe");
        }
        
        /// <summary>
        /// Exports recorded method calls to JSON format for analysis or replay.
        /// </summary>
        /// <param name="recordings">List of recorded method calls</param>
        /// <returns>JSON string representation of the recordings</returns>
        public static string ExportToJson(IEnumerable<RecordedCall> recordings)
        {
            var items = recordings.Select(r => $"{{\"Method\":\"{r.MethodName}\",\"Args\":[{string.Join(",", r.Arguments.Select(a => $"\"{a}\""))}],\"Result\":\"{r.Result}\",\"Duration\":{r.DurationMilliseconds}}}");
            return $"[{string.Join(",", items)}]";
        }
        
        /// <summary>
        /// Exports recorded method calls to CSV format for analysis in spreadsheets.
        /// </summary>
        /// <param name="recordings">List of recorded method calls</param>
        /// <returns>CSV string representation of the recordings</returns>
        public static string ExportToCsv(IEnumerable<RecordedCall> recordings)
        {
            var lines = new List<string> { "Method,Arguments,Result,Duration(ms)" };
            foreach (var r in recordings)
            {
                var args = string.Join(";", r.Arguments);
                lines.Add($"{r.MethodName},\"{args}\",{r.Result},{r.DurationMilliseconds}");
            }
            return string.Join(Environment.NewLine, lines);
        }
        
        /// <summary>
        /// Creates a replay context that can be used to replay recorded method calls.
        /// </summary>
        /// <param name="recordings">List of recorded method calls to replay</param>
        /// <returns>A replay context for verifying behavior matches recordings</returns>
        public static ReplayContext CreateReplayContext(IEnumerable<RecordedCall> recordings)
        {
            return new ReplayContext(recordings.ToList());
        }
    }
    
    /// <summary>
    /// Represents a recorded method call with timing information.
    /// </summary>
    public class RecordedCall
    {
        /// <summary>
        /// Name of the method that was called.
        /// </summary>
        public string MethodName { get; set; } = string.Empty;
        
        /// <summary>
        /// Arguments passed to the method.
        /// </summary>
        public object?[] Arguments { get; set; } = Array.Empty<object?>();
        
        /// <summary>
        /// Result returned by the method.
        /// </summary>
        public object? Result { get; set; }
        
        /// <summary>
        /// Time taken to execute the method in milliseconds.
        /// </summary>
        public long DurationMilliseconds { get; set; }
        
        /// <summary>
        /// Timestamp when the method was called.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Context for replaying recorded method calls and verifying behavior matches.
    /// </summary>
    public class ReplayContext
    {
        private readonly List<RecordedCall> _recordings;
        private int _currentIndex = 0;
        
        public ReplayContext(List<RecordedCall> recordings)
        {
            _recordings = recordings;
        }
        
        /// <summary>
        /// Gets the next expected call in the replay sequence.
        /// </summary>
        public RecordedCall? GetNextExpectedCall()
        {
            if (_currentIndex < _recordings.Count)
                return _recordings[_currentIndex++];
            return null;
        }
        
        /// <summary>
        /// Verifies that a method call matches the next expected recording.
        /// </summary>
        /// <param name="methodName">Name of the method being called</param>
        /// <param name="args">Arguments passed to the method</param>
        /// <returns>True if the call matches the recording, false otherwise</returns>
        public bool VerifyNextCall(string methodName, object?[] args)
        {
            var expected = GetNextExpectedCall();
            if (expected == null) return false;
            
            if (expected.MethodName != methodName) return false;
            if (expected.Arguments.Length != args.Length) return false;
            
            for (int i = 0; i < args.Length; i++)
            {
                if (!Equals(expected.Arguments[i], args[i]))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Resets the replay context to the beginning of the recording sequence.
        /// </summary>
        public void Reset()
        {
            _currentIndex = 0;
        }
        
        /// <summary>
        /// Gets all recordings in this replay context.
        /// </summary>
        public IReadOnlyList<RecordedCall> Recordings => _recordings;
    }
}
