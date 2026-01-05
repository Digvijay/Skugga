#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skugga.Core
{
    /// <summary>
    /// Extension methods for configuring setup contexts with Returns, Callback, and advanced options.
    /// These methods provide the fluent API for setting up mock behavior.
    /// </summary>
    /// <remarks>
    /// This partial class contains extension methods that operate on SetupContext, VoidSetupContext,
    /// and SequenceSetupContext. Additional overloads for 4+ arguments are generated at compile-time
    /// by SetupExtensionsGenerator.cs in the generator project.
    /// 
    /// Key methods:
    /// - Returns/ReturnsAsync: Configure return values (static, dynamic, or computed from arguments)
    /// - Callback: Execute side effects when methods are invoked
    /// - Raises: Auto-raise events when methods are called
    /// - InSequence: Enforce call ordering with MockSequence
    /// - OutValue/RefValue: Configure out/ref parameter values
    /// </remarks>
    public static partial class SetupContextExtensions
    {
        #region Returns Methods
        
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
        
        #endregion

        #region ReturnsAsync Methods
        
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
        
        #endregion

        #region ReturnsInOrder Methods
        
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
        
        #endregion

        #region Callback Methods (for SetupContext<T, TResult>)
        
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
        
        #endregion

        #region Callback Methods (for VoidSetupContext<T>)
        
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
        
        #endregion

        #region Raises Methods
        
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
        
        #endregion

        #region InSequence Methods
        
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
        
        #endregion

        #region CallbackRefOut Methods
        
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
        
        #endregion

        #region OutValue/RefValue Methods (for SetupContext<T, TResult>)
        
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
        
        #endregion

        #region OutValue/RefValue Methods (for VoidSetupContext<T>)
        
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
        
        #endregion

        #region Throws Methods

        /// <summary>
        /// Configures the setup to throw an exception when the method is invoked.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <typeparam name="TResult">The return type of the method</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="exception">The exception to throw</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(x => x.GetData()).Throws(new TimeoutException("Request timed out"));
        /// </example>
        public static SetupContext<TMock, TResult> Throws<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            Exception exception)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, default(TResult), null);
            }
            
            context.Setup.Exception = exception;
            
            return context;
        }

        /// <summary>
        /// Configures the void setup to throw an exception when the method is invoked.
        /// </summary>
        /// <typeparam name="TMock">The type being mocked</typeparam>
        /// <param name="context">The setup context</param>
        /// <param name="exception">The exception to throw</param>
        /// <returns>The setup context for further configuration</returns>
        /// <example>
        /// mock.Setup(x => x.Execute()).Throws(new InvalidOperationException("Operation failed"));
        /// </example>
        public static VoidSetupContext<TMock> Throws<TMock>(
            this VoidSetupContext<TMock> context,
            Exception exception)
        {
            if (context.Setup == null)
            {
                context.Setup = context.Handler.AddSetup(context.Signature, context.Args, null, null);
            }
            
            context.Setup.Exception = exception;
            
            return context;
        }

        #endregion

        #region Error Scenario Methods

        /// <summary>
        /// Configures the setup to throw an HTTP error with the specified status code.
        /// Useful for testing error handling in API integrations.
        /// </summary>
        /// <param name="statusCode">The HTTP status code (e.g., 401, 404, 500)</param>
        /// <param name="message">The error message</param>
        /// <example>
        /// mock.Setup(x => x.ProcessPayment(It.IsAny&lt;Payment&gt;()))
        ///     .ReturnsError(401, "Invalid API key");
        /// </example>
        public static SetupContext<TMock, TResult> ReturnsError<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            int statusCode,
            string message)
        {
            var exception = statusCode switch
            {
                400 => new Exceptions.BadRequestException(message),
                401 => new Exceptions.UnauthorizedException(message),
                403 => new Exceptions.ForbiddenException(message),
                404 => new Exceptions.NotFoundException(message),
                429 => new Exceptions.TooManyRequestsException(message),
                500 => new Exceptions.InternalServerErrorException(message),
                503 => new Exceptions.ServiceUnavailableException(message),
                _ => new Exceptions.HttpStatusException(statusCode, message)
            };

            return context.Throws(exception);
        }

        /// <summary>
        /// Configures the setup to throw an HTTP error with the specified status code and error body.
        /// </summary>
        /// <param name="statusCode">The HTTP status code</param>
        /// <param name="message">The error message</param>
        /// <param name="errorBody">The structured error response body</param>
        public static SetupContext<TMock, TResult> ReturnsError<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            int statusCode,
            string message,
            object errorBody)
        {
            var exception = new Exceptions.HttpStatusException(statusCode, message, errorBody);
            return context.Throws(exception);
        }

        /// <summary>
        /// Configures the setup to throw a 400 Bad Request error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsBadRequest<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.BadRequestException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 400 Bad Request error with validation errors.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="validationErrors">The validation errors</param>
        /// <example>
        /// mock.Setup(x => x.CreateOrder(It.IsAny&lt;Order&gt;()))
        ///     .ReturnsValidationError("Validation failed", new[] {
        ///         new ValidationError("amount", "Must be positive"),
        ///         new ValidationError("currency", "Invalid currency code")
        ///     });
        /// </example>
        public static SetupContext<TMock, TResult> ReturnsValidationError<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message,
            params Exceptions.ValidationError[] validationErrors)
        {
            return context.Throws(new Exceptions.BadRequestException(message, validationErrors));
        }

        /// <summary>
        /// Configures the setup to throw a 401 Unauthorized error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsUnauthorized<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.UnauthorizedException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 403 Forbidden error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsForbidden<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.ForbiddenException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 404 Not Found error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsNotFound<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.NotFoundException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 404 Not Found error with resource details.
        /// </summary>
        /// <param name="resourceType">The type of resource (e.g., "User", "Order")</param>
        /// <param name="resourceId">The resource identifier</param>
        /// <example>
        /// mock.Setup(x => x.GetUser(99)).ReturnsNotFound("User", "99");
        /// </example>
        public static SetupContext<TMock, TResult> ReturnsNotFound<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string resourceType,
            string resourceId)
        {
            return context.Throws(new Exceptions.NotFoundException(resourceType, resourceId));
        }

        /// <summary>
        /// Configures the setup to throw a 429 Too Many Requests error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsTooManyRequests<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.TooManyRequestsException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 429 Too Many Requests error with retry-after.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="retryAfter">How long to wait before retrying</param>
        /// <example>
        /// mock.Setup(x => x.ProcessPayment(It.IsAny&lt;Payment&gt;()))
        ///     .ReturnsTooManyRequests("Rate limit exceeded", TimeSpan.FromSeconds(60));
        /// </example>
        public static SetupContext<TMock, TResult> ReturnsTooManyRequests<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message,
            TimeSpan retryAfter)
        {
            return context.Throws(new Exceptions.TooManyRequestsException(message, retryAfter));
        }

        /// <summary>
        /// Configures the setup to throw a 500 Internal Server Error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsInternalServerError<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.InternalServerErrorException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 503 Service Unavailable error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static SetupContext<TMock, TResult> ReturnsServiceUnavailable<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message)
        {
            return context.Throws(new Exceptions.ServiceUnavailableException(message));
        }

        /// <summary>
        /// Configures the setup to throw a 503 Service Unavailable error with retry-after.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="retryAfter">How long to wait before retrying</param>
        public static SetupContext<TMock, TResult> ReturnsServiceUnavailable<TMock, TResult>(
            this SetupContext<TMock, TResult> context,
            string message,
            TimeSpan retryAfter)
        {
            return context.Throws(new Exceptions.ServiceUnavailableException(message, retryAfter));
        }

        // Void setup context overloads

        /// <summary>
        /// Configures the void setup to throw an HTTP error with the specified status code.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsError<TMock>(
            this VoidSetupContext<TMock> context,
            int statusCode,
            string message)
        {
            var exception = statusCode switch
            {
                400 => new Exceptions.BadRequestException(message),
                401 => new Exceptions.UnauthorizedException(message),
                403 => new Exceptions.ForbiddenException(message),
                404 => new Exceptions.NotFoundException(message),
                429 => new Exceptions.TooManyRequestsException(message),
                500 => new Exceptions.InternalServerErrorException(message),
                503 => new Exceptions.ServiceUnavailableException(message),
                _ => new Exceptions.HttpStatusException(statusCode, message)
            };

            return context.Throws(exception);
        }

        /// <summary>
        /// Configures the void setup to throw a 400 Bad Request error with validation errors.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsValidationError<TMock>(
            this VoidSetupContext<TMock> context,
            string message,
            params Exceptions.ValidationError[] validationErrors)
        {
            return context.Throws(new Exceptions.BadRequestException(message, validationErrors));
        }

        /// <summary>
        /// Configures the void setup to throw a 401 Unauthorized error.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsUnauthorized<TMock>(
            this VoidSetupContext<TMock> context,
            string message)
        {
            return context.Throws(new Exceptions.UnauthorizedException(message));
        }

        /// <summary>
        /// Configures the void setup to throw a 403 Forbidden error.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsForbidden<TMock>(
            this VoidSetupContext<TMock> context,
            string message)
        {
            return context.Throws(new Exceptions.ForbiddenException(message));
        }

        /// <summary>
        /// Configures the void setup to throw a 404 Not Found error.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsNotFound<TMock>(
            this VoidSetupContext<TMock> context,
            string message)
        {
            return context.Throws(new Exceptions.NotFoundException(message));
        }

        /// <summary>
        /// Configures the void setup to throw a 429 Too Many Requests error.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsTooManyRequests<TMock>(
            this VoidSetupContext<TMock> context,
            string message)
        {
            return context.Throws(new Exceptions.TooManyRequestsException(message));
        }

        /// <summary>
        /// Configures the void setup to throw a 500 Internal Server Error.
        /// </summary>
        public static VoidSetupContext<TMock> ReturnsInternalServerError<TMock>(
            this VoidSetupContext<TMock> context,
            string message)
        {
            return context.Throws(new Exceptions.InternalServerErrorException(message));
        }

        #endregion
    }
}
