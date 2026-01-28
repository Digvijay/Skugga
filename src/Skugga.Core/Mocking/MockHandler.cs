#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Skugga.Core
{
    /// <summary>
    /// Core engine that handles mock setup, invocation recording, and verification.
    /// Each mock instance has exactly one MockHandler.
    /// </summary>
    public class MockHandler
    {
        private readonly List<MockSetup> _setups = new();
        private readonly List<Invocation> _invocations = new();
        private readonly ConcurrentDictionary<string, object?> _propertyStorage = new();
        private readonly ConcurrentDictionary<string, List<Delegate>> _eventHandlers = new();
        private readonly List<Type> _additionalInterfaces = new();
        private readonly Dictionary<Type, object?> _defaultReturnValues = new();

        private ChaosPolicy? _chaosPolicy;
        private ChaosStatistics? _chaosStats;
        private Random _rng = new();
        private DefaultValueProvider? _defaultValueProvider;
        private DefaultValue? _explicitDefaultValueStrategy;

        /// <summary>
        /// Gets or sets the mock behavior (Loose or Strict).
        /// </summary>
        public MockBehavior Behavior { get; set; } = MockBehavior.Loose;

        /// <summary>
        /// Gets or sets the default value strategy (Mock, Empty, or Default).
        /// </summary>
        public DefaultValue DefaultValueStrategy
        {
            get => _explicitDefaultValueStrategy ?? DefaultValue.Empty;
            set
            {
                _explicitDefaultValueStrategy = value;
                _defaultValueProvider = value switch
                {
                    DefaultValue.Empty => new EmptyDefaultValueProvider(),
                    DefaultValue.Mock => new MockDefaultValueProvider(),
                    _ => null
                };
            }
        }

        /// <summary>
        /// Gets or sets a custom default value provider.
        /// </summary>
        public DefaultValueProvider? DefaultValueProvider
        {
            get => _defaultValueProvider;
            set => _defaultValueProvider = value;
        }

        /// <summary>
        /// Gets or sets whether to call the base implementation if no setup matches.
        /// </summary>
        public bool CallBase { get; set; }

        /// <summary>
        /// Gets the list of setups configured for this mock.
        /// </summary>
        public IReadOnlyList<MockSetup> Setups => _setups;

        /// <summary>
        /// Gets the list of all invocations made to this mock.
        /// </summary>
        public IReadOnlyList<Invocation> Invocations => _invocations;

        /// <summary>
        /// Gets the chaos statistics for this mock.
        /// </summary>
        public ChaosStatistics ChaosStatistics => _chaosStats ??= new ChaosStatistics();

        #region Setup Management

        /// <summary>
        /// Adds a new setup for a method with an optional callback.
        /// </summary>
        public MockSetup AddSetup(string signature, object?[] args, object? value, Action<object?[]>? callback = null)
        {
            var setup = new MockSetup(signature, args, value, callback);
            _setups.Add(setup); // First setup added takes precedence because of loop order
            return setup;
        }

        /// <summary>
        /// Adds a new setup for a method with a sequential return value.
        /// </summary>
        public MockSetup AddSequentialSetup(string signature, object?[] args, IEnumerable<object?> values)
        {
            var setup = new MockSetup(signature, args, null)
            {
                SequentialValues = values.ToArray()
            };
            _setups.Add(setup);
            return setup;
        }

        /// <summary>
        /// Adds or updates a callback for the most recent matching setup.
        /// </summary>
        public void AddCallbackToLastSetup(string signature, object?[] args, Action<object?[]> callback)
        {
            var setup = _setups.LastOrDefault(s => s.Matches(signature, args));
            if (setup != null)
            {
                setup.Callback = callback;
            }
            else
            {
                AddSetup(signature, args, null, callback);
            }
        }

        #endregion

        #region Property Storage

        /// <summary>
        /// Configures a property to use backing storage.
        /// </summary>
        public void SetupPropertyStorage(string propertyName, object? defaultValue)
        {
            _propertyStorage[propertyName] = defaultValue;
        }

        /// <summary>
        /// Gets a property value from the backing store.
        /// </summary>
        public object? GetPropertyValue(string propertyName)
        {
            return _propertyStorage.TryGetValue(propertyName, out var value) ? value : null;
        }

        /// <summary>
        /// Sets a property value in the backing store.
        /// </summary>
        public void SetPropertyValue(string propertyName, object? value)
        {
            _propertyStorage[propertyName] = value;
        }

        /// <summary>
        /// Checks if a property has been setup with backing storage.
        /// </summary>
        public bool HasPropertyStorage(string propertyName)
        {
            return _propertyStorage.ContainsKey(propertyName);
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Adds an event handler to the specified event.
        /// </summary>
        public void AddEventHandler(string eventName, Delegate handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);
        }

        /// <summary>
        /// Removes an event handler from the specified event.
        /// </summary>
        public void RemoveEventHandler(string eventName, Delegate handler)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Raises the specified event with the given arguments.
        /// </summary>
        public void RaiseEvent(string eventName, params object?[] args)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                foreach (var handler in handlers.ToList())
                {
                    try
                    {
                        handler.DynamicInvoke(args);
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (ex.InnerException != null)
                            throw ex.InnerException;
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Chaos Mode

        /// <summary>
        /// Configures chaos mode with the specified policy.
        /// </summary>
        public void SetChaosPolicy(ChaosPolicy policy)
        {
            _chaosPolicy = policy;
            if (_chaosStats == null) _chaosStats = new ChaosStatistics();

            if (policy.Seed.HasValue)
                _rng = new Random(policy.Seed.Value);
        }

        #endregion

        #region Additional Interfaces

        /// <summary>
        /// Adds an additional interface that this mock is claimed to implement.
        /// </summary>
        public void AddInterface(Type interfaceType)
        {
            if (!_additionalInterfaces.Contains(interfaceType))
                _additionalInterfaces.Add(interfaceType);
        }

        /// <summary>
        /// Gets the list of additional interfaces implemented by this mock.
        /// </summary>
        public IReadOnlyCollection<Type> AdditionalInterfaces => _additionalInterfaces;

        /// <summary>
        /// Gets all additional interfaces that have been added to the mock via As<T>().
        /// </summary>
        public IReadOnlySet<Type> GetAdditionalInterfaces()
        {
            return new HashSet<Type>(_additionalInterfaces);
        }

        #endregion

        #region Default Values

        /// <summary>
        /// Sets a default return value for a specific type.
        /// </summary>
        public void SetReturnsDefault(Type type, object? value)
        {
            _defaultReturnValues[type] = value;
        }

        /// <summary>
        /// Sets a default return value for a specific type.
        /// </summary>
        public void SetReturnsDefault<T>(T value)
        {
            _defaultReturnValues[typeof(T)] = value;
        }

        #endregion

        #region Invocation Engine

        /// <summary>
        /// Core method called by generated mocks for every member invocation.
        /// Matches the invocation against setups, records history, and handles return values.
        /// </summary>
        public object? Invoke(string signature, object?[] args, Type? returnType = null, bool canCallBase = false)
        {
            // Record invocation
            _invocations.Add(new Invocation(signature, args));

            #region Chaos Mode Application

            if (_chaosPolicy != null)
            {
                var stats = ChaosStatistics;
                stats.TotalInvocations++;

                if (_chaosPolicy.TimeoutMilliseconds > 0)
                {
                    stats.TimeoutTriggeredCount++;
                    System.Threading.Thread.Sleep(_chaosPolicy.TimeoutMilliseconds);
                }

                if (_rng.NextDouble() < _chaosPolicy.FailureRate)
                {
                    stats.ChaosTriggeredCount++;
                    if (_chaosPolicy.PossibleExceptions?.Length > 0)
                        throw _chaosPolicy.PossibleExceptions[_rng.Next(_chaosPolicy.PossibleExceptions.Length)];
                }
            }

            #endregion

            #region Setup Matching and Execution

            foreach (var setup in _setups)
            {
                if (setup.Matches(signature, args))
                {
                    if (setup.Sequence != null)
                        setup.Sequence.RecordInvocation(setup.SequenceStep, signature);

                    setup.CallCount++;
                    setup.ExecuteCallback(args);

                    if (setup.EventToRaise != null && setup.EventArgs != null)
                        RaiseEvent(setup.EventToRaise, setup.EventArgs);

                    if (setup.Exception != null)
                        throw setup.Exception;

                    // If setup has out/ref parameters or CallbackRefOut, return setup object
                    if ((setup.OutValues?.Count > 0) ||
                        (setup.RefValues?.Count > 0) ||
                        (setup.OutValueFactories?.Count > 0) ||
                        (setup.RefValueFactories?.Count > 0) ||
                        setup.RefOutCallback != null)
                    {
                        return setup;
                    }

                    if (returnType == typeof(void))
                        return null;

                    if (setup.SequentialValues != null)
                        return setup.GetNextSequentialValue();

                    return setup.ValueFactory != null ? setup.ValueFactory(args) : setup.Value;
                }
            }

            #endregion

            #region Property and Event Shortcut Handling

            if (signature.StartsWith("get_") && args.Length == 0)
            {
                var propertyName = signature.Substring(4);
                if (HasPropertyStorage(propertyName))
                    return GetPropertyValue(propertyName);
            }
            else if (signature.StartsWith("set_") && args.Length == 1)
            {
                var propertyName = signature.Substring(4);
                if (HasPropertyStorage(propertyName))
                {
                    SetPropertyValue(propertyName, args[0]);
                    return null;
                }
            }

            if (signature.StartsWith("add_") && args.Length == 1 && args[0] is Delegate addHandler)
            {
                var eventName = signature.Substring(4);
                AddEventHandler(eventName, addHandler);
                return null;
            }
            if (signature.StartsWith("remove_") && args.Length == 1 && args[0] is Delegate removeHandler)
            {
                var eventName = signature.Substring(7);
                RemoveEventHandler(eventName, removeHandler);
                return null;
            }

            #endregion

            #region Unmatched Invocation Handling

            if (CallBase && canCallBase)
                return MockSetup.CallBaseMarker;

            if (Behavior == MockBehavior.Strict)
                throw new MockException($"[Strict Mode] Call to '{signature}' was not setup.");

            if (returnType == null || returnType == typeof(void))
                return null;

            return GetDefaultValue(returnType);

            #endregion
        }

        public void Verify(string signature, object?[] args, Times times, HashSet<int>? refOutParameterIndices = null)
        {
            var matchingInvocations = _invocations.Where(i => i.Matches(signature, args, refOutParameterIndices)).ToList();
            if (!times.Validate(matchingInvocations.Count))
            {
                var currentCalls = matchingInvocations.Count;
                var errorMsg = $"Verification failed: Expected {times.Description} call(s) to '{signature}', but was called {currentCalls} time(s).";
                throw new MockException(errorMsg);
            }

            foreach (var invocation in matchingInvocations)
                invocation.IsVerified = true;
        }

        public void Verify()
        {
            var unverifiableSetups = _setups.Where(s => s.IsVerifiable && s.CallCount == 0).ToList();
            if (unverifiableSetups.Any())
            {
                var first = unverifiableSetups.First();
                throw new MockException($"Verification failed: Expected setup for '{first.Signature}' to be called, but it was not.");
            }
        }

        public void VerifyAll()
        {
            var uncalledSetups = _setups.Where(s => s.CallCount == 0).ToList();
            if (uncalledSetups.Any())
            {
                var first = uncalledSetups.First();
                throw new MockException($"Verification failed: Expected all setups to be called, but '{first.Signature}' was not.");
            }
        }

        public void Reset()
        {
            _setups.Clear();
            _invocations.Clear();
            _propertyStorage.Clear();
            _eventHandlers.Clear();
        }

        public void ResetCalls()
        {
            _invocations.Clear();
            foreach (var setup in _setups)
                setup.CallCount = 0;
        }

        public void VerifyNoOtherCalls()
        {
            var unverified = _invocations.Where(i => !i.IsVerified).ToList();
            if (unverified.Any())
            {
                var first = unverified.First();
                throw new MockException($"Verification failed: Expected no other calls, but found {unverified.Count} unverified call(s). First unverified call: '{first.Signature}'.");
            }
        }

        #endregion

        #region Helpers

        private object? GetDefaultValue(Type type)
        {
            if (_defaultReturnValues.TryGetValue(type, out var value))
                return value;

            if (_defaultValueProvider != null)
                return _defaultValueProvider.GetDefaultValue(type, this);

            // Handle Task and Task<T> for async support in Loose mode
            if (type == typeof(Task))
                return Task.CompletedTask;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = type.GetGenericArguments()[0];
                var defaultValue = GetDefaultValue(resultType);
                return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null, new[] { defaultValue });
            }

            // If no explicit strategy was set, return CLR default (null for ref types, 0 for value types)
            if (_explicitDefaultValueStrategy == null)
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                return null;
            }

            DefaultValueProvider provider = DefaultValueStrategy == DefaultValue.Mock
                ? new MockDefaultValueProvider()
                : new EmptyDefaultValueProvider();

            return provider.GetDefaultValue(type, this);
        }

        #endregion
    }
}
