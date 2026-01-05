#nullable enable
using System;
using System.Collections.Generic;

namespace Skugga.Core
{
    /// <summary>
    /// Tracks the expected order of mock invocations for sequential verification.
    /// Used with InSequence() to ensure methods are called in a specific order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class enables verification that methods are called in a specific sequence.
    /// Each setup registers a step number, and invocations are validated to occur in order.
    /// </para>
    /// <para>
    /// This is thread-safe and uses locking to ensure consistent behavior in concurrent scenarios.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sequence = new MockSequence();
    /// var mock = Mock.Create&lt;IService&gt;();
    /// 
    /// mock.InSequence(sequence).Setup(x => x.Initialize()).Returns(true);
    /// mock.InSequence(sequence).Setup(x => x.Execute()).Returns("done");
    /// 
    /// // Must be called in order
    /// mock.Initialize(); // OK
    /// mock.Execute();    // OK
    /// mock.Initialize(); // Throws - out of sequence
    /// </code>
    /// </example>
    public class MockSequence
    {
        /// <summary>
        /// The current step number for setup registration.
        /// </summary>
        private int _setupStep = 0;

        /// <summary>
        /// The current step number for invocation verification.
        /// </summary>
        private int _invocationStep = 0;

        /// <summary>
        /// Lock object for thread-safe access to step counters.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the current invocation step in the sequence.
        /// </summary>
        internal int CurrentInvocationStep => _invocationStep;

        /// <summary>
        /// Registers that a setup is part of this sequence and returns the step number.
        /// </summary>
        /// <returns>The step number assigned to this setup</returns>
        /// <remarks>
        /// This is called internally when a setup is configured with InSequence().
        /// Each call increments the setup counter.
        /// </remarks>
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
        /// <param name="expectedStep">The step number this method was registered at</param>
        /// <param name="methodSignature">The method signature for error messages</param>
        /// <exception cref="MockException">Thrown when the method is invoked out of sequence</exception>
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
    /// <remarks>
    /// Use this class when configuring mock setups that need to assign values to out parameters.
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IParser&gt;();
    /// var outValue = new OutValue&lt;int&gt;(42);
    /// 
    /// mock.Setup(x => x.TryParse("test", out outValue)).Returns(true);
    /// 
    /// int result;
    /// bool success = mock.TryParse("test", out result);
    /// // result is now 42, success is true
    /// </code>
    /// </example>
    public class OutValue<T>
    {
        /// <summary>
        /// Gets or sets the value to assign to the out parameter.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        /// <param name="value">The value to assign to the out parameter</param>
        public OutValue(T value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Represents a value to be assigned to a ref parameter, or a validator for ref parameters.
    /// </summary>
    /// <typeparam name="T">The type of the ref parameter</typeparam>
    /// <remarks>
    /// <para>
    /// This class supports three modes:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Exact value</b>: Match ref parameters with a specific value</description></item>
    /// <item><description><b>Validator</b>: Match ref parameters using a predicate</description></item>
    /// <item><description><b>Any value</b>: Match any ref parameter value</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = Mock.Create&lt;IProcessor&gt;();
    /// 
    /// // Exact value matching
    /// var refValue = new RefValue&lt;int&gt;(42);
    /// mock.Setup(x => x.Process(ref refValue)).Returns(true);
    /// 
    /// // Validator matching
    /// var refValidator = new RefValue&lt;int&gt;(x => x > 0);
    /// mock.Setup(x => x.Process(ref refValidator)).Returns(true);
    /// 
    /// // Any value matching
    /// mock.Setup(x => x.Process(ref RefValue&lt;int&gt;.IsAny)).Returns(true);
    /// </code>
    /// </example>
    public class RefValue<T>
    {
        /// <summary>
        /// Gets or sets the exact value to match (when using value mode).
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// Gets or sets the validator function (when using validator mode).
        /// </summary>
        public Func<T, bool>? Validator { get; set; }

        /// <summary>
        /// Gets or sets whether this matches any value.
        /// </summary>
        public bool IsAnyValue { get; set; }

        /// <summary>
        /// Initializes a new instance that matches an exact value.
        /// </summary>
        /// <param name="value">The value to match</param>
        public RefValue(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance that uses a validator function.
        /// </summary>
        /// <param name="validator">The validator function</param>
        public RefValue(Func<T, bool> validator)
        {
            Validator = validator;
        }

        /// <summary>
        /// Private constructor for IsAny mode.
        /// </summary>
        private RefValue(bool isAny)
        {
            IsAnyValue = isAny;
        }

        /// <summary>
        /// Gets a RefValue that matches any value for the ref parameter.
        /// </summary>
        public static RefValue<T> IsAny => new RefValue<T>(true);

        /// <summary>
        /// Determines if the specified value matches this RefValue configuration.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value matches; otherwise false</returns>
        public bool Matches(T value)
        {
            // IsAny matches everything
            if (IsAnyValue) return true;

            // Validator mode - use the predicate
            if (Validator != null) return Validator(value);

            // Value mode - use equality comparison
            return EqualityComparer<T>.Default.Equals(Value, value);
        }
    }
}
