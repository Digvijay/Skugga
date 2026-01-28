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

            MockHandler handler = setup.Handler;
            Expression? targetExpression = null;
            string memberName;
            object?[] args;

            // Handle both method calls and property access
            if (expression.Body is MethodCallExpression methodCall)
            {
                // Method call: mock.Setup(x => x.GetData(42))
                targetExpression = methodCall.Object;
                memberName = methodCall.Method.Name;
                args = methodCall.Arguments.Select(GetArgumentValue).ToArray();
            }
            else if (expression.Body is MemberExpression memberAccess && memberAccess.Member.MemberType == System.Reflection.MemberTypes.Property)
            {
                // Property access: mock.Setup(x => x.Name)
                targetExpression = memberAccess.Expression;
                memberName = "get_" + memberAccess.Member.Name;
                args = Array.Empty<object?>();
            }
            else
            {
                throw new ArgumentException($"Expression must be a method call or property access, got: {expression.Body.GetType().Name}");
            }

            // Handle recursive setups (e.g. x => x.Prop.Method())
            if (targetExpression != null && targetExpression != expression.Parameters[0])
            {
                try
                {
                    // Evaluate the target expression on the mock
                    // This calls the property getters/methods to get the intermediate mock
                    var lambda = Expression.Lambda(targetExpression, expression.Parameters);
                    var func = lambda.Compile();
                    var targetObject = func.DynamicInvoke(mock);

                    if (targetObject is IMockSetup innerSetup)
                    {
                        handler = innerSetup.Handler;
                    }
                    else if (targetObject != null)
                    {
                        throw new ArgumentException($"Recursive setup target '{targetExpression}' returned a non-mock object of type {targetObject.GetType().Name}. Only Skugga mocks can be setup.");
                    }
                    else
                    {
                        throw new ArgumentException($"Recursive setup target '{targetExpression}' returned null. Ensure recursive mocking is enabled (DefaultValue.Mock).");
                    }
                }
                catch (Exception ex) when (ex is not ArgumentException)
                {
                    throw new ArgumentException($"Failed to resolve recursive setup target: {ex.Message}", ex);
                }
            }

            return new SetupContext<TMock, TResult>(handler, memberName, args);
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
        /// Sets up a property setter on the mock.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="expression">Lambda expression specifying the property assignment (x => x.Property = value)</param>
        /// <returns>A void setup context that can be configured with Callback()</returns>
        /// <remarks>
        /// This method is intercepted at compile time by the source generator.
        /// Use it to configure behavior for property setters, such as callbacks or specific value matching.
        /// </remarks>
        /// <example>
        /// mock.SetupSet(x => x.Name = "John").Callback(() => Console.WriteLine("Name set"));
        /// </example>
        public static VoidSetupContext<TMock> SetupSet<TMock>(this TMock mock, Action<TMock> expression)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            // This method MUST be intercepted by the source generator.
            // There is no runtime-only fallback because extracting property/value from Action<T>
            // without reflection or expression trees is not possible in this zero-reflection library.
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept SetupSet.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.");
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

            // Delegate to handler which tracks verification status
            setup.Handler.Verify(signature, args, times, null);
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

            // Delegate to handler
            setup.Handler.Verify(signature, args, times, null);
        }

        #endregion

        #region Management Methods

        /// <summary>
        /// Clears all setups and invocation history for this mock.
        /// </summary>
        public static void Reset<TMock>(this TMock mock) where TMock : class
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            setup.Handler.Reset();
        }

        /// <summary>
        /// Clears only the invocation history for this mock, keeping all setups.
        /// </summary>
        public static void ResetCalls<TMock>(this TMock mock) where TMock : class
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            setup.Handler.ResetCalls();
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

        public static void SetupAllProperties<TMock>(this TMock mock) where TMock : class
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            var properties = typeof(TMock).GetProperties();
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

            // Delegate to handler
            setup.Handler.Verify(signature, args, times, null);
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

            // Delegate to handler
            setup.Handler.Verify(signature, args, times, null);
        }

        /// <summary>
        /// Verifies that a property setter was called with a specific value using an assignment lambda.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="mock">The mock instance</param>
        /// <param name="setterExpression">Lambda expression specifying the property assignment (x => x.Property = value)</param>
        /// <param name="times">The expected number of times the setter should be called (default: at least once)</param>
        /// <example>
        /// mock.VerifySet(x => x.Name = "John", Times.Once());
        /// </example>
        public static void VerifySet<TMock>(this TMock mock, Action<TMock> setterExpression, Times? times = null)
        {
            if (mock is not IMockSetup setup)
                throw new ArgumentException("Object is not a Skugga Mock");

            // This method MUST be intercepted by the source generator.
            throw new InvalidOperationException(
                $"[Skugga] Source generator failed to intercept VerifySet.\n" +
                "Ensure your project references Skugga.Generator and enables interceptors.");
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

            // Use a matcher to match any delegate since we don't know the exact instance used in the test
            var matcher = new ArgumentMatcher<Delegate>(d => true, "It.IsAny<Delegate>()");

            // Delegate to handler
            setup.Handler.Verify(signature, new object?[] { matcher }, times, null);
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

            // Use a matcher to match any delegate since we don't know the exact instance used in the test
            var matcher = new ArgumentMatcher<Delegate>(d => true, "It.IsAny<Delegate>()");

            // Delegate to handler
            setup.Handler.Verify(signature, new object?[] { matcher }, times, null);
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
            if (mock is IMockSetup setup)
            {
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
        public static IProtectedMockSetup<T> Protected<T>(this T mock) where T : class
        {
            if (mock is not IMockSetup mockSetup)
                throw new ArgumentException("Object is not a Skugga mock", nameof(mock));

            return new ProtectedMockSetup<T>(mockSetup.Handler);
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
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static object? GetArgumentValue(Expression expr)
        {
            if (expr is ConstantExpression c)
                return c.Value;

            if (expr is MemberExpression memberExpr)
                return EvaluateMemberExpression(memberExpr);

            if (expr is ConditionalExpression cond)
            {
                var test = GetArgumentValue(cond.Test);
                if (test is bool b)
                {
                    return b ? GetArgumentValue(cond.IfTrue) : GetArgumentValue(cond.IfFalse);
                }
                return Expression.Lambda(cond).Compile().DynamicInvoke();
            }

            if (expr is IndexExpression indexExpr)
            {
                var target = indexExpr.Object != null ? GetArgumentValue(indexExpr.Object) : null;
                var indexArgs = indexExpr.Arguments.Select(GetArgumentValue).ToArray();
                return indexExpr.Indexer?.GetValue(target, indexArgs);
            }

            if (expr is UnaryExpression unaryExpr)
            {
                if (unaryExpr.NodeType == ExpressionType.Quote)
                    return GetArgumentValue(unaryExpr.Operand);

                var operand = GetArgumentValue(unaryExpr.Operand);
                return EvaluateUnaryExpression(unaryExpr.NodeType, operand, unaryExpr.Type);
            }

            if (expr is BinaryExpression binaryExpr)
            {
                var left = GetArgumentValue(binaryExpr.Left);
                var right = GetArgumentValue(binaryExpr.Right);
                return EvaluateBinaryExpression(binaryExpr.NodeType, left, right, binaryExpr.Type);
            }

            if (expr is MethodCallExpression methodCall)
            {
                if (methodCall.Method.DeclaringType?.Name == "It")
                    return HandleItMatchers(methodCall, methodCall.Method.ReturnType);

                if (methodCall.Method.DeclaringType?.Name == "Match" && methodCall.Method.Name == "Create")
                    return HandleMatchCreate(methodCall);

                // Fallback for custom logic (like helper methods returning Match.Create)
                try
                {
                    var lambda = Expression.Lambda<Func<object>>(Expression.Convert(methodCall, typeof(object)));
                    return lambda.Compile()();
                }
                catch { /* fallback to exception below */ }
            }

            if (expr is NewArrayExpression newArray)
            {
                var elements = newArray.Expressions.Select(GetArgumentValue).ToArray();

                // If any element is a matcher, we must use object[] because
                // matchers cannot be stored in specialized arrays (e.g., string[], int[]).
                bool containsMatcher = elements.Any(e => e is ArgumentMatcher);

                var elementType = containsMatcher ? typeof(object) : newArray.Type.GetElementType()!;
                var array = Array.CreateInstance(elementType, elements.Length);

                for (int i = 0; i < elements.Length; i++)
                {
                    array.SetValue(elements[i], i);
                }

                return array;
            }

            throw new NotSupportedException(
                $"Skugga cannot extract argument values from expression: {expr.GetType().Name}\n" +
                "Skugga is compile-time only with zero runtime reflection.\n" +
                "Solutions:\n" +
                "1. Use constant values: mock.Setup(x => x.Method(42))\n" +
                "2. Use argument matchers: mock.Setup(x => x.Method(It.IsAny<int>()))\n" +
                "3. Ensure source generator is intercepting (see project setup)\n" +
                "Note: Variables, properties, and calculations require generator interception.");
        }

        private static object? EvaluateMemberExpression(MemberExpression memberExpr)
        {
            object? container = null;
            if (memberExpr.Expression != null)
                container = GetArgumentValue(memberExpr.Expression);

            if (memberExpr.Member is System.Reflection.FieldInfo field)
                return field.GetValue(container);

            if (memberExpr.Member is System.Reflection.PropertyInfo property)
                return property.GetValue(container);

            return null;
        }

        private static object? EvaluateUnaryExpression(ExpressionType nodeType, object? operand, Type resultType)
        {
            if (operand == null) return null;
            return nodeType switch
            {
                ExpressionType.Not when operand is bool b => !b,
                ExpressionType.Negate when operand is int i => -i,
                ExpressionType.Negate when operand is long l => -l,
                ExpressionType.Convert => Convert.ChangeType(operand, resultType),
                _ => null
            };
        }

        private static object? EvaluateBinaryExpression(ExpressionType nodeType, object? left, object? right, Type resultType)
        {
            if (left == null || right == null) return null;
            return nodeType switch
            {
                ExpressionType.Add when left is int l && right is int r => l + r,
                ExpressionType.Subtract when left is int l && right is int r => l - r,
                ExpressionType.Multiply when left is int l && right is int r => l * r,
                ExpressionType.Divide when left is int l && right is int r => l / r,
                ExpressionType.Add when left is string l && right is string r => l + r,
                ExpressionType.Equal => Equals(left, right),
                ExpressionType.NotEqual => !Equals(left, right),
                ExpressionType.ArrayIndex when left is Array arr && right is int idx => arr.GetValue(idx),
                _ => null
            };
        }

        private static ArgumentMatcher? HandleItMatchers(MethodCallExpression methodCall, Type matcherType)
        {
            var methodName = methodCall.Method.Name;

            // Helper to create the matcher via reflection
            ArgumentMatcher Create(object predicate, string description)
            {
                var method = typeof(MockExtensions).GetMethod(nameof(CreateMatcherGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(matcherType);
                return (ArgumentMatcher)method.Invoke(null, new object[] { predicate, description })!;
            }

            // Helper to create Func<T, bool> returning true
            object CreateTruePredicate()
            {
                var param = Expression.Parameter(matcherType, "x");
                return Expression.Lambda(Expression.Constant(true), param).Compile();
            }

            if (methodName == "IsAny")
            {
                return Create(CreateTruePredicate(), $"It.IsAny<{matcherType.Name}>()");
            }

            if (methodName == "Is" && methodCall.Arguments.Count == 1)
            {
                if (methodCall.Arguments[0] is LambdaExpression lambda)
                    return Create(lambda.Compile(), $"It.Is<{matcherType.Name}>(...)");

                // Fallback if not lambda?
                return Create(CreateTruePredicate(), $"It.Is<{matcherType.Name}>(...) [Fallback]");
            }

            if (methodName == "IsIn" && methodCall.Arguments.Count == 1)
            {
                var values = GetArgumentValue(methodCall.Arguments[0]) as System.Collections.IEnumerable;
                if (values != null)
                {
                    var valuesList = values.Cast<object?>().ToArray();
                    // Create predicate v => valuesList.Contains(v)
                    // Hard to make generic predicate dynamically without expression tree construction involved
                    // Simplified: check equality via object.Equals inside a generic wrapper

                    var param = Expression.Parameter(matcherType, "v");
                    // We need to capture valuesList.
                    // Implementation detail: constructing expression to call Enumerable.Contains is heavy.
                    // Hack: use a static helper that takes IEnumerable and returns Func<T, bool>

                    var helper = typeof(MockExtensions).GetMethod(nameof(CreateIsInPredicate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                        .MakeGenericMethod(matcherType);
                    var predicate = helper.Invoke(null, new object[] { valuesList });

                    return Create(predicate!, $"It.IsIn({string.Join(", ", valuesList.Select(v => v?.ToString() ?? "null"))})");
                }
            }

            if (methodName == "IsNotIn" && methodCall.Arguments.Count == 1)
            {
                var values = GetArgumentValue(methodCall.Arguments[0]) as System.Collections.IEnumerable;
                if (values != null)
                {
                    var valuesList = values.Cast<object?>().ToArray();
                    var helper = typeof(MockExtensions).GetMethod(nameof(CreateIsNotInPredicate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                        .MakeGenericMethod(matcherType);
                    var predicate = helper.Invoke(null, new object[] { valuesList });

                    return Create(predicate!, $"It.IsNotIn({string.Join(", ", valuesList.Select(v => v?.ToString() ?? "null"))})");
                }
            }

            if (methodName == "IsInRange" && methodCall.Arguments.Count == 3)
            {
                var from = GetArgumentValue(methodCall.Arguments[0]);
                var to = GetArgumentValue(methodCall.Arguments[1]);
                var rangeKind = (Range)GetArgumentValue(methodCall.Arguments[2])!;

                var helper = typeof(MockExtensions).GetMethod(nameof(CreateIsInRangePredicate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(matcherType);
                var predicate = helper.Invoke(null, new object[] { from!, to!, rangeKind });

                return Create(predicate!, $"It.IsInRange({from}, {to}, Range.{rangeKind})");
            }

            if (methodName == "IsNotNull")
            {
                // Predicate: v => v != null
                var param = Expression.Parameter(matcherType, "v");
                var check = Expression.NotEqual(param, Expression.Constant(null, matcherType));
                var predicate = Expression.Lambda(check, param).Compile();
                return Create(predicate, $"It.IsNotNull<{matcherType.Name}>()");
            }

            if (methodName == "IsRegex" && (methodCall.Arguments.Count == 1 || methodCall.Arguments.Count == 2))
            {
                if (GetArgumentValue(methodCall.Arguments[0]) is string pattern)
                {
                    var options = methodCall.Arguments.Count == 2
                        ? (System.Text.RegularExpressions.RegexOptions)GetArgumentValue(methodCall.Arguments[1])!
                        : System.Text.RegularExpressions.RegexOptions.None;

                    var regex = new System.Text.RegularExpressions.Regex(pattern, options);

                    // Predicate: v => v is string s && regex.IsMatch(s)
                    if (matcherType == typeof(string))
                    {
                        Func<string, bool> pred = s => s != null && regex.IsMatch(s);
                        return Create(pred, $"It.IsRegex(\"{pattern}\")");
                    }
                }
            }
            return null;
        }

        private static ArgumentMatcher? HandleMatchCreate(MethodCallExpression methodCallExpr)
        {
            var matcherType = methodCallExpr.Method.ReturnType;
            if (methodCallExpr.Arguments.Count >= 1)
            {
                var predicateExpr = methodCallExpr.Arguments[0];
                string description = methodCallExpr.Arguments.Count == 2 && GetArgumentValue(methodCallExpr.Arguments[1]) is string desc
                    ? desc : $"Match.Create<{matcherType.Name}>(predicate)";

                if (predicateExpr is LambdaExpression lambda)
                {
                    var compiledPredicate = lambda.Compile();
                    // compiledPredicate is Func<T, bool>.
                    // Verify logic creates ArgumentMatcher<T>.

                    var method = typeof(MockExtensions).GetMethod(nameof(CreateMatcherGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                        .MakeGenericMethod(matcherType);
                    return (ArgumentMatcher)method.Invoke(null, new object[] { compiledPredicate, description })!;
                }
            }
            return null;
        }

        private static ArgumentMatcher<T> CreateMatcherGeneric<T>(Func<T, bool> predicate, string description)
        {
            return new ArgumentMatcher<T>(predicate, description);
        }

        private static Func<T, bool> CreateIsInPredicate<T>(IEnumerable<object?> values)
        {
            var set = values.ToHashSet();
            return v => set.Contains(v);
        }

        private static Func<T, bool> CreateIsNotInPredicate<T>(IEnumerable<object?> values)
        {
            var set = values.ToHashSet();
            return v => !set.Contains(v);
        }

        private static Func<T, bool> CreateIsInRangePredicate<T>(object from, object to, Range rangeKind) where T : IComparable
        {
            var fromTyped = (T)from;
            var toTyped = (T)to;

            if (rangeKind == Range.Inclusive)
            {
                return v => v != null && v.CompareTo(fromTyped) >= 0 && v.CompareTo(toTyped) <= 0;
            }
            else
            {
                return v => v != null && v.CompareTo(fromTyped) > 0 && v.CompareTo(toTyped) < 0;
            }
        }

        /// <summary>
        /// Gets the default CLR value for a type.
        /// Used when setting up all properties on a mock.
        /// </summary>
        private static object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        #endregion
    }
}
