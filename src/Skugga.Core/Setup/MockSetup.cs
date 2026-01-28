#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// Represents a configured setup for a method or property on a mock.
    /// Stores return values, callbacks, out/ref parameters, and other configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each Setup() call creates a MockSetup instance that defines how a method should behave.
    /// The handler matches invocations against setups to determine return values and execute callbacks.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Static or computed return values (Value vs ValueFactory)</description></item>
    /// <item><description>Sequential return values (SetupSequence)</description></item>
    /// <item><description>Callbacks for side effects</description></item>
    /// <item><description>Out/ref parameter support (static and dynamic)</description></item>
    /// <item><description>Event raising on invocation</description></item>
    /// <item><description>Sequential ordering (InSequence)</description></item>
    /// <item><description>Verifiability for MockRepository/VerifyAll (IsVerifiable)</description></item>
    /// </list>
    /// </remarks>
    public class MockSetup
    {
        /// <summary>
        /// Gets the method signature (e.g., "GetData" or "get_Name" for properties).
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Gets the expected arguments (may include ArgumentMatcher instances).
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// Gets or sets the static return value for this setup.
        /// </summary>
        /// <remarks>
        /// Used when Returns(value) is called. For computed values, use ValueFactory instead.
        /// </remarks>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets a function that computes the return value from method arguments.
        /// </summary>
        /// <remarks>
        /// Used when Returns(func) is called. Takes precedence over Value if both are set.
        /// </remarks>
        public Func<object?[], object?>? ValueFactory { get; set; }

        /// <summary>
        /// Gets or sets the callback to execute when this setup is matched.
        /// </summary>
        /// <remarks>
        /// Executes before the return value is provided. Useful for side effects or verification.
        /// </remarks>
        public Action<object?[]>? Callback { get; set; }

        /// <summary>
        /// Gets or sets an exception to throw when this setup is matched.
        /// </summary>
        /// <remarks>
        /// Used by Throws() extension method. When set, this exception is thrown instead of returning a value.
        /// </remarks>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets a delegate with ref/out parameters for complex parameter scenarios.
        /// </summary>
        /// <remarks>
        /// Invoked by generated code with proper ref/out modifiers to assign parameter values.
        /// </remarks>
        public Delegate? RefOutCallback { get; set; }

        /// <summary>
        /// Gets or sets an array of values to return sequentially on successive calls.
        /// </summary>
        /// <remarks>
        /// Used by SetupSequence(). Each call returns the next value in the array.
        /// Last value is repeated for subsequent calls.
        /// </remarks>
        public object?[]? SequentialValues { get; set; }

        /// <summary>
        /// Tracks the current index in SequentialValues for sequential returns.
        /// </summary>
        private int _sequentialIndex = 0;

        /// <summary>
        /// Sentinel value returned by MockHandler.Invoke to indicate that the base implementation should be called.
        /// </summary>
        public static readonly object CallBaseMarker = new object();

        #region Event Support

        /// <summary>
        /// Gets or sets the event name to raise when this setup is invoked.
        /// </summary>
        /// <remarks>
        /// Used by Raises() extension method to trigger events on method invocation.
        /// </remarks>
        public string? EventToRaise { get; set; }

        /// <summary>
        /// Gets or sets the event arguments to pass when raising the event.
        /// </summary>
        public object?[]? EventArgs { get; set; }

        #endregion

        #region Sequence Support

        /// <summary>
        /// Gets or sets the MockSequence this setup is part of (if any).
        /// </summary>
        /// <remarks>
        /// Used by InSequence() to enforce ordered method calls.
        /// </remarks>
        public MockSequence? Sequence { get; set; }

        /// <summary>
        /// Gets or sets the step number in the sequence.
        /// </summary>
        public int SequenceStep { get; set; }

        #endregion

        #region Out/Ref Parameter Support

        /// <summary>
        /// Gets or sets static out parameter values (parameter index -> value).
        /// </summary>
        /// <remarks>
        /// Used when OutValue() is called to configure static out parameter values.
        /// </remarks>
        public Dictionary<int, object?>? OutValues { get; set; }

        /// <summary>
        /// Gets or sets static ref parameter values (parameter index -> value).
        /// </summary>
        /// <remarks>
        /// Used when RefValue() is called to configure static ref parameter values.
        /// </remarks>
        public Dictionary<int, object?>? RefValues { get; set; }

        /// <summary>
        /// Gets or sets dynamic out parameter value factories (parameter index -> factory).
        /// </summary>
        /// <remarks>
        /// Used when OutValueFunc() is called to compute out values from method arguments.
        /// </remarks>
        public Dictionary<int, Func<object?[], object?>>? OutValueFactories { get; set; }

        /// <summary>
        /// Gets or sets dynamic ref parameter value factories (parameter index -> factory).
        /// </summary>
        /// <remarks>
        /// Used when RefValueFunc() is called to compute ref values from method arguments.
        /// </remarks>
        public Dictionary<int, Func<object?[], object?>>? RefValueFactories { get; set; }

        /// <summary>
        /// Gets or sets the set of parameter indices that are ref/out parameters.
        /// </summary>
        /// <remarks>
        /// These parameters are ignored during matching since their values are outputs, not inputs.
        /// </remarks>
        public HashSet<int>? RefOutParameterIndices { get; set; }
        #endregion
        /// <summary>
        /// Gets or sets whether this setup is required to be called for Verify().
        /// </summary>
        public bool IsVerifiable { get; set; }

        /// <summary>
        /// Gets the number of times this setup has been matched by an invocation.
        /// </summary>
        public int CallCount { get; internal set; }



        /// <summary>
        /// Initializes a new MockSetup with the specified configuration.
        /// </summary>
        /// <param name="sig">The method signature</param>
        /// <param name="args">The expected arguments</param>
        /// <param name="val">The return value</param>
        /// <param name="callback">Optional callback to execute on invocation</param>
        public MockSetup(string sig, object?[] args, object? val, Action<object?[]>? callback = null)
        {
            Signature = sig;
            Args = args;
            Value = val;
            Callback = callback;
        }

        /// <summary>
        /// Determines if this setup matches the specified method signature and arguments.
        /// </summary>
        /// <param name="sig">The method signature being invoked</param>
        /// <param name="args">The arguments passed to the method</param>
        /// <returns>True if this setup matches; otherwise false</returns>
        /// <remarks>
        /// <para>
        /// Matching logic:
        /// </para>
        /// <list type="number">
        /// <item><description>Signature must match exactly</description></item>
        /// <item><description>Argument count must match</description></item>
        /// <item><description>Ref/out parameters are always matched (ignored during comparison)</description></item>
        /// <item><description>ArgumentMatcher instances use their Matches() method</description></item>
        /// <item><description>Regular values use Equals() comparison</description></item>
        /// </list>
        /// </remarks>
        public bool Matches(string sig, object?[] args)
        {
            // Quick checks: signature and argument count must match
            if (Signature != sig || Args.Length != args.Length)
            {
                return false;
            }

            // Check each argument
            for (int i = 0; i < Args.Length; i++)
            {
                // Skip matching for ref/out parameters - they always match regardless of value
                // since they are outputs, not inputs
                if (RefOutParameterIndices != null && RefOutParameterIndices.Contains(i))
                    continue;

                if (!AreArgumentsEquivalent(Args[i], args[i]))
                    return false;
            }

            return true;
        }

        private static bool AreArgumentsEquivalent(object? expected, object? actual)
        {
            // If the expected arg (from setup) is a matcher, use its Matches() method
            if (expected is ArgumentMatcher matcher)
            {
                return matcher.Matches(actual);
            }

            // Handle arrays (including params)
            if (expected is Array expectedArray && actual is Array actualArray)
            {
                if (expectedArray.Length != actualArray.Length)
                {
                    return false;
                }

                for (int i = 0; i < expectedArray.Length; i++)
                {
                    if (!AreArgumentsEquivalent(expectedArray.GetValue(i), actualArray.GetValue(i)))
                        return false;
                }
                return true;
            }

            // Standard equality check for non-matcher arguments
            if (expected == null)
                return actual == null;

            return expected.Equals(actual);
        }

        /// <summary>
        /// Executes the callback if configured.
        /// </summary>
        /// <param name="args">The method arguments to pass to the callback</param>
        /// <remarks>
        /// RefOutCallback is NOT invoked here - it's invoked by generated code
        /// with proper ref/out modifiers to assign parameter values.
        /// </remarks>
        public void ExecuteCallback(object?[] args)
        {
            Callback?.Invoke(args);
            // RefOutCallback is invoked by generated code with proper ref/out modifiers
        }

        /// <summary>
        /// Gets the next value in the sequential values array.
        /// </summary>
        /// <returns>The next sequential value</returns>
        /// <remarks>
        /// <para>
        /// Used by SetupSequence(). Each call advances the index and returns the next value.
        /// When the end is reached, the last value is repeated for subsequent calls.
        /// </para>
        /// <para>
        /// If the value is a SequenceException marker, the wrapped exception is thrown instead.
        /// This enables SetupSequence().Throws() patterns.
        /// </para>
        /// </remarks>
        /// <exception cref="Exception">Throws the exception if the current value is a SequenceException</exception>
        public object? GetNextSequentialValue()
        {
            if (SequentialValues == null || SequentialValues.Length == 0)
                return null;

            var value = SequentialValues[_sequentialIndex];

            // Advance index, but don't go past the end (repeat last value)
            if (_sequentialIndex < SequentialValues.Length - 1)
                _sequentialIndex++;

            // Check if this value is an exception marker
            if (value is SequenceException seqEx)
                throw seqEx.Exception;

            return value;
        }
    }
}
