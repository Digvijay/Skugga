#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// Represents the context for configuring a method or property setup that returns a value.
    /// Provides fluent API for Returns(), Callback(), and other configuration methods.
    /// </summary>
    /// <typeparam name="T">The type being mocked</typeparam>
    /// <typeparam name="TResult">The return type of the method or property</typeparam>
    /// <remarks>
    /// <para>
    /// Created by Setup() extension methods. Use the fluent API to configure:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Return values (static, computed, or sequential)</description></item>
    /// <item><description>Callbacks for side effects</description></item>
    /// <item><description>Out/ref parameter values</description></item>
    /// <item><description>Event raising on invocation</description></item>
    /// <item><description>Sequential ordering with InSequence()</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var context = mock.Setup(x => x.GetData(42));
    /// context.Returns("result")
    ///        .Callback(() => Console.WriteLine("Called"))
    ///        .Raises(nameof(IService.DataRetrieved), EventArgs.Empty);
    /// </code>
    /// </example>
    public class SetupContext<T, TResult>
    {
        /// <summary>
        /// Gets the mock handler for this setup.
        /// </summary>
        public MockHandler Handler { get; }

        /// <summary>
        /// Gets the method signature being setup.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Gets the expected arguments for this setup.
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// Gets or sets the underlying MockSetup instance (created when Returns/Callback is called).
        /// </summary>
        internal MockSetup? Setup { get; set; }

        /// <summary>
        /// Initializes a new setup context.
        /// </summary>
        /// <param name="handler">The mock handler</param>
        /// <param name="signature">The method signature</param>
        /// <param name="args">The expected arguments</param>
        public SetupContext(MockHandler handler, string signature, object?[] args)
        {
            Handler = handler;
            Signature = signature;
            Args = args;
        }
    }

    /// <summary>
    /// Represents the context for configuring a void method setup.
    /// Provides fluent API for Callback() and other void method configuration.
    /// </summary>
    /// <typeparam name="T">The type being mocked</typeparam>
    /// <remarks>
    /// <para>
    /// Created by Setup() extension methods for void methods.
    /// Since void methods have no return value, only callbacks and side effects can be configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// mock.Setup(x => x.Execute())
    ///     .Callback(() => Console.WriteLine("Executed"))
    ///     .Raises(nameof(IService.Completed), EventArgs.Empty);
    /// </code>
    /// </example>
    public class VoidSetupContext<T>
    {
        /// <summary>
        /// Gets the mock handler for this setup.
        /// </summary>
        public MockHandler Handler { get; }

        /// <summary>
        /// Gets the method signature being setup.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Gets the expected arguments for this setup.
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// Gets or sets the underlying MockSetup instance (created when Callback is called).
        /// </summary>
        internal MockSetup? Setup { get; set; }

        /// <summary>
        /// Initializes a new void setup context.
        /// </summary>
        /// <param name="handler">The mock handler</param>
        /// <param name="signature">The method signature</param>
        /// <param name="args">The expected arguments</param>
        public VoidSetupContext(MockHandler handler, string signature, object?[] args)
        {
            Handler = handler;
            Signature = signature;
            Args = args;
        }
    }

    /// <summary>
    /// Represents the context for configuring a sequence of return values.
    /// Each call to Returns() or Throws() adds to the sequence.
    /// </summary>
    /// <typeparam name="TMock">The type being mocked</typeparam>
    /// <typeparam name="TResult">The return type of the method</typeparam>
    /// <remarks>
    /// <para>
    /// Created by SetupSequence() extension methods. Use to configure methods
    /// that return different values on successive calls.
    /// </para>
    /// <para>
    /// The last value in the sequence is repeated for subsequent calls after
    /// the sequence is exhausted.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// mock.SetupSequence(x => x.GetNext())
    ///     .Returns(1)
    ///     .Returns(2)
    ///     .Throws(new InvalidOperationException())
    ///     .Returns(3);
    /// 
    /// mock.GetNext(); // 1
    /// mock.GetNext(); // 2
    /// mock.GetNext(); // throws InvalidOperationException
    /// mock.GetNext(); // 3
    /// mock.GetNext(); // 3 (repeats)
    /// </code>
    /// </example>
    public class SequenceSetupContext<TMock, TResult>
    {
        /// <summary>
        /// Gets the mock handler for this setup.
        /// </summary>
        public MockHandler Handler { get; }

        /// <summary>
        /// Gets the method signature being setup.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Gets the expected arguments for this setup.
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// Gets or sets the underlying MockSetup instance.
        /// </summary>
        internal MockSetup? Setup { get; set; }

        /// <summary>
        /// Accumulates the sequential values to return.
        /// </summary>
        private readonly List<object?> _values = new();

        /// <summary>
        /// Initializes a new sequence setup context.
        /// </summary>
        /// <param name="handler">The mock handler</param>
        /// <param name="signature">The method signature</param>
        /// <param name="args">The expected arguments</param>
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
        /// <remarks>
        /// Each call adds a value to the sequence. Values are returned in order on successive calls.
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.SetupSequence(x => x.GetNext())
        ///     .Returns("first")
        ///     .Returns("second")
        ///     .Returns("third");
        /// </code>
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
        /// <remarks>
        /// <para>
        /// Use to simulate errors or test exception handling logic.
        /// The exception is thrown when this step in the sequence is reached.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// mock.SetupSequence(x => x.TryConnect())
        ///     .Returns(false)  // First attempt fails
        ///     .Returns(false)  // Second attempt fails
        ///     .Returns(true);  // Third attempt succeeds
        /// 
        /// mock.SetupSequence(x => x.GetData())
        ///     .Returns("data1")
        ///     .Throws(new TimeoutException())  // Simulates timeout
        ///     .Returns("data2");
        /// </code>
        /// </example>
        public SequenceSetupContext<TMock, TResult> Throws(Exception exception)
        {
            // Use SequenceException as a marker to indicate an exception should be thrown
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
    /// Internal marker class to indicate an exception should be thrown in a sequence.
    /// </summary>
    /// <remarks>
    /// This class wraps an exception and is stored in the sequential values array.
    /// When MockSetup.GetNextSequentialValue() encounters this, it throws the wrapped exception.
    /// </remarks>
    internal class SequenceException
    {
        /// <summary>
        /// Gets the exception to throw.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new SequenceException marker.
        /// </summary>
        /// <param name="exception">The exception to throw when this step is reached</param>
        public SequenceException(Exception exception) => Exception = exception;
    }
}
