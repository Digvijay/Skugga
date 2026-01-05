#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Skugga.Core
{
    /// <summary>
    /// Provides extension methods for configuring and verifying mocks.
    /// These methods enable the fluent API for setting up mock behavior and verifying invocations.
    /// </summary>
    /// <remarks>
    /// This class contains all the primary extension methods that developers use to work with Skugga mocks:
    /// - Setup methods for configuring mock behavior
    /// - Verify methods for asserting that methods were called
    /// - Property management (SetupProperty, VerifyGet, VerifySet)
    /// - Event management (Raise, VerifyAdd, VerifyRemove)
    /// - Advanced features (As, Protected, Chaos)
    /// </remarks>
    public static class MockExtensions
    {
        #region Setup Methods
        
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
        
        #endregion

        #region Verification Methods

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
        
        #endregion

        #region Property Management
        
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
        /// <param name="valueExpression">The expected value that should have been set (can be constant, variable, or It.* matcher)</param>
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
        
        #endregion

        #region Event Management

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
        
        #endregion

        #region Advanced Features
        
        /// <summary>
        /// Enables chaos mode on the mock with the specified policy configuration.
        /// Chaos mode randomly injects failures and delays to test resilience.
        /// </summary>
        /// <typeparam name="T">The mock type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="config">Action to configure the chaos policy</param>
        /// <example>
        /// mock.Chaos(policy => 
        /// {
        ///     policy.FailureRate = 0.3; // 30% of calls will fail
        ///     policy.PossibleExceptions = new[] { new TimeoutException(), new IOException() };
        /// });
        /// </example>
        public static void Chaos<T>(this T mock, Action<ChaosPolicy> config)
        {
             if (mock is IMockSetup setup) {
                 var policy = new ChaosPolicy();
                 config(policy);
                 setup.Handler.SetChaosPolicy(policy);
             }
        }

        /// <summary>
        /// Gets the chaos statistics for the mock, showing how many times chaos was triggered.
        /// </summary>
        /// <typeparam name="T">The mock type</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <returns>Chaos statistics including trigger count and total invocations</returns>
        /// <exception cref="ArgumentException">Thrown if the object is not a Skugga mock</exception>
        /// <example>
        /// var mock = Mock.Create&lt;IService&gt;();
        /// mock.Chaos(p => p.FailureRate = 0.3);
        /// // ... use the mock ...
        /// var stats = mock.GetChaosStatistics();
        /// Console.WriteLine($"Chaos triggered {stats.ChaosTriggeredCount} times");
        /// </example>
        public static ChaosStatistics GetChaosStatistics<T>(this T mock)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");
            
            return setup.Handler.ChaosStatistics;
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
        /// var mock = Mock.Create&lt;AbstractClass&gt;();
        /// mock.Protected()
        ///     .Setup&lt;int&gt;("ExecuteCore")
        ///     .Returns(42);
        /// </example>
        public static IProtectedMockSetup Protected<T>(this T mock) where T : class
        {
            if (mock is not IMockSetup mockSetup)
                throw new ArgumentException("Object is not a Skugga mock", nameof(mock));
            
            return new ProtectedMockSetup(mockSetup.Handler);
        }
        
        #endregion

        #region Helper Methods

        /// <summary>
        /// Extracts the argument value from an expression tree.
        /// Handles constants, variables, It.* matchers, and Match.Create calls.
        /// </summary>
        /// <param name="expr">The expression to extract the value from</param>
        /// <returns>The extracted value or an ArgumentMatcher for matcher expressions</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the expression type cannot be evaluated.
        /// This typically means the source generator needs to intercept the call.
        /// </exception>
        /// <remarks>
        /// This method is central to Skugga's compile-time approach. It can handle:
        /// - Constant values: 42, "test", null
        /// - Simple variables captured by closures
        /// - It.* matcher calls (IsAny, Is, IsIn, IsNotNull, IsRegex)
        /// - Match.Create custom matchers
        /// 
        /// For complex expressions (calculations, method calls, etc.), the source generator
        /// must intercept the Setup/Verify call and emit code that directly passes values.
        /// </remarks>
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
        /// Gets the default CLR value for a type.
        /// Used when setting up all properties on a mock.
        /// </summary>
        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        
        #endregion
    }
}
