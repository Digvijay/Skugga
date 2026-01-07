#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// The core handler that manages mock behavior, setups, invocations, and verification.
    /// Each mock instance has its own handler to track configuration and calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is the heart of Skugga's mocking functionality. It:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Stores method setups configured via Setup()</description></item>
    /// <item><description>Records all invocations for verification</description></item>
    /// <item><description>Applies chaos policies for resilience testing</description></item>
    /// <item><description>Manages property backing storage for SetupProperty()</description></item>
    /// <item><description>Handles event subscriptions and raising</description></item>
    /// <item><description>Provides default values via configurable providers</description></item>
    /// </list>
    /// <para>
    /// The handler is thread-safe for invocation recording and chaos application,
    /// but setup operations should be performed before concurrent access.
    /// </para>
    /// </remarks>
    public class MockHandler
    {
        // Core storage for setups and invocations
        private readonly List<MockSetup> _setups = new();
        private readonly List<Invocation> _invocations = new();

        // Chaos mode support for resilience testing
        private ChaosPolicy? _chaosPolicy;
        private Random _rng = new();
        private readonly ChaosStatistics _chaosStats = new();

        // Property backing store for automatic get/set tracking via SetupProperty()
        private readonly Dictionary<string, object?> _propertyStorage = new();

        // Event handler storage for subscription tracking and event raising
        private readonly Dictionary<string, List<Delegate>> _eventHandlers = new();

        // Additional interfaces added via As<T>() for multi-interface mocks
        private readonly HashSet<Type> _additionalInterfaces = new();

        // Default value provider for un-setup members
        private DefaultValueProvider? _defaultValueProvider;

        // Track if user explicitly set strategy (null = not set, use backwards-compatible behavior)
        private DefaultValue? _explicitDefaultValueStrategy = null;

        /// <summary>
        /// Gets or sets the mock behavior (Loose or Strict).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Loose:</b> Un-setup members return default values (no exceptions).
        /// </para>
        /// <para>
        /// <b>Strict:</b> Un-setup members throw MockException.
        /// </para>
        /// <para>
        /// Default is Loose for backwards compatibility and ease of use.
        /// </para>
        /// </remarks>
        public MockBehavior Behavior { get; set; } = MockBehavior.Loose;

        /// <summary>
        /// Gets or sets the default value strategy for un-setup members.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Empty:</b> Returns CLR defaults and empty collections.
        /// </para>
        /// <para>
        /// <b>Mock:</b> Returns mock instances for interfaces/abstract classes (recursive mocking).
        /// </para>
        /// <para>
        /// When set, this creates the appropriate default value provider automatically.
        /// For custom behavior, set DefaultValueProvider directly instead.
        /// </para>
        /// </remarks>
        public DefaultValue DefaultValueStrategy
        {
            get => _explicitDefaultValueStrategy ?? DefaultValue.Empty;
            set => _explicitDefaultValueStrategy = value;
        }

        /// <summary>
        /// Gets or sets a custom default value provider.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This takes precedence over DefaultValueStrategy. Use this for complete control
        /// over default value generation for un-setup members.
        /// </para>
        /// <para>
        /// If null, the handler uses the strategy specified by DefaultValueStrategy
        /// (or CLR defaults if neither is set, for backwards compatibility).
        /// </para>
        /// </remarks>
        public DefaultValueProvider? DefaultValueProvider
        {
            get => _defaultValueProvider;
            set => _defaultValueProvider = value;
        }

        /// <summary>
        /// Gets statistics about chaos mode behavior during test execution.
        /// </summary>
        /// <remarks>
        /// Provides counters for total invocations, chaos-triggered failures, and timeouts.
        /// Reset when a new chaos policy is set.
        /// </remarks>
        public ChaosStatistics ChaosStatistics => _chaosStats;

        /// <summary>
        /// Gets all recorded invocations for verification.
        /// </summary>
        /// <remarks>
        /// Each method call on the mock adds an Invocation to this list.
        /// Used by Verify() methods to check if methods were called with expected arguments.
        /// </remarks>
        public IReadOnlyList<Invocation> Invocations => _invocations;

        #region Additional Interfaces (As<T> support)

        /// <summary>
        /// Adds an additional interface that the mock should implement.
        /// </summary>
        /// <param name="interfaceType">The interface type to add</param>
        /// <remarks>
        /// Called internally by the As&lt;T&gt;() extension method.
        /// The source generator uses this to create composite mock classes
        /// that implement multiple interfaces.
        /// </remarks>
        public void AddInterface(Type interfaceType)
        {
            lock (_additionalInterfaces)
            {
                _additionalInterfaces.Add(interfaceType);
            }
        }

        /// <summary>
        /// Gets all additional interfaces that have been added to the mock via As&lt;T&gt;().
        /// </summary>
        /// <returns>Read-only set of additional interface types</returns>
        /// <remarks>
        /// Used by the source generator to determine which interfaces the generated
        /// mock class should implement.
        /// </remarks>
        public IReadOnlySet<Type> GetAdditionalInterfaces()
        {
            lock (_additionalInterfaces)
            {
                return new HashSet<Type>(_additionalInterfaces);
            }
        }

        #endregion

        #region Setup Management

        /// <summary>
        /// Adds a method setup with return value and optional callback.
        /// </summary>
        /// <param name="signature">The method signature (e.g., "GetData" or "get_Name")</param>
        /// <param name="args">The method arguments (may include matchers like It.IsAny)</param>
        /// <param name="value">The return value for this setup</param>
        /// <param name="callback">Optional callback to execute when method is invoked</param>
        /// <returns>The created setup for further configuration</returns>
        /// <remarks>
        /// This is called internally by Setup() extension methods. The signature
        /// is the method name (or "get_/set_PropertyName" for properties).
        /// </remarks>
        public MockSetup AddSetup(string signature, object?[] args, object? value, Action<object?[]>? callback = null)
        {
            var setup = new MockSetup(signature, args, value, callback);
            _setups.Add(setup);
            return setup;
        }

        /// <summary>
        /// Adds or updates a callback for the most recent matching setup.
        /// </summary>
        /// <param name="signature">The method signature</param>
        /// <param name="args">The method arguments</param>
        /// <param name="callback">The callback to execute</param>
        /// <remarks>
        /// If no matching setup exists, creates a new setup with only the callback.
        /// This supports the .Callback() fluent API.
        /// </remarks>
        public void AddCallbackToLastSetup(string signature, object?[] args, Action<object?[]> callback)
        {
            var setup = _setups.LastOrDefault(s => s.Matches(signature, args));
            if (setup != null)
            {
                setup.Callback = callback;
            }
            else
            {
                // Create setup with just callback if none exists
                _setups.Add(new MockSetup(signature, args, null, callback));
            }
        }

        /// <summary>
        /// Adds or updates a callback (no parameters) for the most recent matching setup.
        /// </summary>
        /// <param name="signature">The method signature</param>
        /// <param name="args">The method arguments</param>
        /// <param name="callback">The callback action</param>
        public void AddCallbackToLastSetup(string signature, object?[] args, Action callback)
        {
            AddCallbackToLastSetup(signature, args, _ => callback());
        }

        #endregion

        #region Chaos Mode

        /// <summary>
        /// Configures chaos mode for resilience testing.
        /// </summary>
        /// <param name="policy">The chaos policy specifying failure rate, timeouts, and exceptions</param>
        /// <remarks>
        /// <para>
        /// Chaos mode randomly triggers failures and delays to test application resilience.
        /// Useful for ensuring code handles exceptions and timeouts gracefully.
        /// </para>
        /// <para>
        /// If the policy specifies a seed, the random number generator is initialized
        /// for deterministic chaos behavior across test runs.
        /// </para>
        /// </remarks>
        public void SetChaosPolicy(ChaosPolicy policy)
        {
            _chaosPolicy = policy;
            // Initialize RNG with seed if provided for reproducible chaos
            if (policy.Seed.HasValue)
                _rng = new Random(policy.Seed.Value);
        }

        #endregion

        #region Property Storage (SetupProperty support)

        /// <summary>
        /// Sets up a property with automatic backing field for get/set tracking.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="defaultValue">The initial value for the property</param>
        /// <remarks>
        /// After calling this, the property behaves like a normal property with storage.
        /// Gets return the stored value, sets update the stored value.
        /// </remarks>
        public void SetupPropertyStorage(string propertyName, object? defaultValue)
        {
            _propertyStorage[propertyName] = defaultValue;
        }

        /// <summary>
        /// Gets a property value from the backing store.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>The stored value, or null if property hasn't been setup</returns>
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

        #endregion

        #region Event Handling

        /// <summary>
        /// Adds an event handler to the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">The delegate handler to add</param>
        /// <remarks>
        /// This is called when subscribing to events on the mock (mock.SomeEvent += handler).
        /// The subscription is tracked as an invocation.
        /// </remarks>
        public void AddEventHandler(string eventName, Delegate handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);

            // Track event subscription as an invocation for verification
            _invocations.Add(new Invocation("add_" + eventName, new object?[] { handler }));
        }

        /// <summary>
        /// Removes an event handler from the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">The delegate handler to remove</param>
        /// <remarks>
        /// This is called when unsubscribing from events (mock.SomeEvent -= handler).
        /// The unsubscription is tracked as an invocation.
        /// </remarks>
        public void RemoveEventHandler(string eventName, Delegate handler)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }

            // Track event unsubscription as an invocation for verification
            _invocations.Add(new Invocation("remove_" + eventName, new object?[] { handler }));
        }

        /// <summary>
        /// Raises the specified event with the given arguments.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments to pass to subscribers</param>
        /// <remarks>
        /// <para>
        /// Invokes all subscribed handlers in order. If a handler throws an exception,
        /// it's unwrapped (for TargetInvocationException) and re-thrown.
        /// </para>
        /// <para>
        /// Uses ToList() before enumeration to avoid modification-during-enumeration issues
        /// if a handler unsubscribes during event handling.
        /// </para>
        /// </remarks>
        public void RaiseEvent(string eventName, params object?[] args)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                // ToList() creates copy to avoid modification-during-enumeration
                foreach (var handler in handlers.ToList())
                {
                    try
                    {
                        handler.DynamicInvoke(args);
                    }
                    catch (Exception ex)
                    {
                        // Unwrap TargetInvocationException to get actual exception
                        if (ex.InnerException != null)
                            throw ex.InnerException;
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Invocation and Matching

        /// <summary>
        /// Invokes a method on the mock, applies setups, chaos, and records the invocation.
        /// </summary>
        /// <param name="signature">The method signature being invoked</param>
        /// <param name="args">The arguments passed to the method</param>
        /// <returns>The return value from matching setup, or null in Loose mode</returns>
        /// <exception cref="MockException">Thrown in Strict mode when no matching setup exists</exception>
        /// <exception cref="ChaosException">Thrown randomly when chaos mode is enabled</exception>
        /// <remarks>
        /// <para>
        /// This is the core method invoked by generated mock implementations.
        /// The process:
        /// </para>
        /// <list type="number">
        /// <item><description>Record invocation for verification</description></item>
        /// <item><description>Apply chaos policy (delays, random failures)</description></item>
        /// <item><description>Find matching setup and execute callback/event/return value</description></item>
        /// <item><description>If no setup matches, return null (Loose) or throw (Strict)</description></item>
        /// </list>
        /// <para>
        /// Special handling: If setup has out/ref parameters or needs dynamic values,
        /// returns the setup itself so generated code can extract those values.
        /// </para>
        /// </remarks>
        public object? Invoke(string signature, object?[] args)
        {
            // Always record invocation for verification, even if it fails
            _invocations.Add(new Invocation(signature, args));

            #region Chaos Mode Application

            // Apply chaos policy if configured
            if (_chaosPolicy != null)
            {
                _chaosStats.TotalInvocations++;

                // Simulate timeout/delay if configured
                // Useful for testing timeout handling in application code
                if (_chaosPolicy.TimeoutMilliseconds > 0)
                {
                    _chaosStats.TimeoutTriggeredCount++;
                    System.Threading.Thread.Sleep(_chaosPolicy.TimeoutMilliseconds);
                }

                // Trigger failure based on failure rate (0.0 to 1.0)
                // RNG.NextDouble() returns [0.0, 1.0), so failure rate of 0.5 = 50% chance
                if (_rng.NextDouble() < _chaosPolicy.FailureRate)
                {
                    _chaosStats.ChaosTriggeredCount++;

                    // Only throw if exceptions are configured
                    // Throws one of the configured exceptions randomly
                    if (_chaosPolicy.PossibleExceptions?.Length > 0)
                        throw _chaosPolicy.PossibleExceptions[_rng.Next(_chaosPolicy.PossibleExceptions.Length)];
                }
            }

            #endregion

            #region Setup Matching and Execution

            // Find the first matching setup (setups are checked in order)
            foreach (var setup in _setups)
            {
                if (setup.Matches(signature, args))
                {
                    // Check sequence order if this setup is part of a sequence
                    if (setup.Sequence != null)
                    {
                        setup.Sequence.RecordInvocation(setup.SequenceStep, signature);
                    }

                    // Execute callback if present (before returning value or throwing exception)
                    setup.ExecuteCallback(args);

                    // Raise event if configured via .Raises()
                    if (setup.EventToRaise != null && setup.EventArgs != null)
                    {
                        RaiseEvent(setup.EventToRaise, setup.EventArgs);
                    }

                    // Throw exception if configured via .Throws() or error scenario methods
                    if (setup.Exception != null)
                    {
                        throw setup.Exception;
                    }

                    // If setup has out/ref parameters (static or dynamic) or callback,
                    // return the setup itself so generated code can extract those values.
                    // Generated code pattern:
                    //   var result = handler.Invoke(...);
                    //   if (result is MockSetup setup) { /* apply out/ref values */ }
                    if (setup.OutValues != null || setup.RefValues != null ||
                        setup.OutValueFactories != null || setup.RefValueFactories != null ||
                        setup.RefOutCallback != null)
                    {
                        return setup;
                    }

                    // Return sequential value if using SetupSequence()
                    if (setup.SequentialValues != null)
                        return setup.GetNextSequentialValue();

                    // Return factory-generated value if ValueFactory is set, otherwise static Value
                    return setup.ValueFactory != null ? setup.ValueFactory(args) : setup.Value;
                }
            }

            #endregion

            #region Unmatched Invocation Handling

            // No matching setup found
            if (Behavior == MockBehavior.Strict)
                throw new MockException($"[Strict Mode] Call to '{signature}' was not setup.");

            // In Loose mode, return null here
            // Generated code will call GetDefaultValueFor<T> to convert null to appropriate default
            return null;

            #endregion
        }

        #endregion

        #region Default Value Providers

        /// <summary>
        /// Gets the default value for a specific type using the configured default value provider.
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="mock">The mock instance (used for recursive mocking in Mock mode)</param>
        /// <returns>Default value for the type</returns>
        /// <remarks>
        /// <para>
        /// Called by generated mock implementations when Invoke() returns null (un-setup member in Loose mode).
        /// </para>
        /// <para>
        /// <b>Special handling:</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description>Task: Returns Task.CompletedTask</description></item>
        /// <item><description>Task&lt;T&gt;: Returns Task.FromResult(default(T))</description></item>
        /// <item><description>Other types: Uses configured provider or CLR defaults</description></item>
        /// </list>
        /// <para>
        /// If no explicit strategy was set (_explicitDefaultValueStrategy == null),
        /// returns CLR defaults for backwards compatibility with existing tests.
        /// </para>
        /// </remarks>
        public T? GetDefaultValueFor<T>(object mock)
        {
            var type = typeof(T);

            // Special handling for Task and Task<T> - always return completed tasks
            // This prevents tests from hanging when un-setup async methods are called
            if (type == typeof(System.Threading.Tasks.Task))
            {
                return (T)(object)System.Threading.Tasks.Task.CompletedTask;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>))
            {
                // Use Task.FromResult<TResult>(default(TResult))
                var resultType = type.GetGenericArguments()[0];
                var fromResultMethod = typeof(System.Threading.Tasks.Task)
                    .GetMethod("FromResult")!
                    .MakeGenericMethod(resultType);
                var result = fromResultMethod.Invoke(null, new object?[] { GetDefaultForType(resultType, mock) });
                return (T)result!;
            }

            // If custom provider is set, use it (takes precedence over strategy)
            if (_defaultValueProvider != null)
            {
                var value = _defaultValueProvider.GetDefaultValue(typeof(T), mock);
                return value == null ? default(T) : (T)value;
            }

            // If no explicit strategy was set, return CLR defaults for backwards compatibility
            // This ensures existing tests don't break
            if (_explicitDefaultValueStrategy == null)
            {
                return default(T);
            }

            // Otherwise use strategy-based provider (Empty or Mock)
            DefaultValueProvider provider = _explicitDefaultValueStrategy == DefaultValue.Mock
                ? new MockDefaultValueProvider()
                : new EmptyDefaultValueProvider();

            var providerResult = provider.GetDefaultValue(typeof(T), mock);
            return providerResult == null ? default(T) : (T)providerResult;
        }

        /// <summary>
        /// Helper method to get default value for any type (used by Task&lt;T&gt; handling).
        /// </summary>
        /// <param name="type">The type to get default value for</param>
        /// <param name="mock">The mock instance for recursive mocking</param>
        /// <returns>Default value for the specified type</returns>
        /// <remarks>
        /// DynamicallyAccessedMembers attribute ensures AOT compatibility when creating value types.
        /// The attribute tells the AOT compiler to preserve the parameterless constructor.
        /// </remarks>
        private object? GetDefaultForType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type type,
            object mock)
        {
            // If custom provider is set, use it
            if (_defaultValueProvider != null)
            {
                return _defaultValueProvider.GetDefaultValue(type, mock);
            }

            // If no explicit strategy, use AOT-safe default construction
            if (_explicitDefaultValueStrategy == null)
            {
                // AOT-safe: DynamicallyAccessedMembers attribute ensures constructor is preserved
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            // Use strategy-based provider
            DefaultValueProvider provider = _explicitDefaultValueStrategy == DefaultValue.Mock
                ? new MockDefaultValueProvider()
                : new EmptyDefaultValueProvider();

            return provider.GetDefaultValue(type, mock);
        }

        #endregion
    }
}
