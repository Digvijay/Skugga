#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Skugga.Core
{
    public interface IMockSetup
    {
        // Now accepts arguments for precise matching
        void AddSetup(string signature, object?[] args, object? value);
        object? Invoke(string signature, object?[] args);
    }

    public static class Mock
    {
        public static T Create<T>() => throw new NotImplementedException("Skugga generator failed to intercept!");
    }

    public static class MockExtensions
    {
        public static SetupContext<TReturn> Setup<T, TReturn>(this T mock, Expression<Func<T, TReturn>> expression) 
            where T : class
        {
            var mockSetup = mock as IMockSetup ?? throw new InvalidOperationException("Mock not generated.");
            
            // 1. Handle Method Calls: mock.Setup(x => x.Method(1))
            if (expression.Body is MethodCallExpression methodCall)
            {
                string signature = methodCall.Method.Name;
                object?[] args = new object?[methodCall.Arguments.Count];
                
                // AOT-Safe Argument Evaluation (No Expression.Compile)
                for (int i = 0; i < methodCall.Arguments.Count; i++)
                {
                    args[i] = Evaluate(methodCall.Arguments[i]);
                }

                return new SetupContext<TReturn>(mockSetup, signature, args);
            }
            // 2. Handle Properties: mock.Setup(x => x.Name)
            else if (expression.Body is MemberExpression memberEx && memberEx.Member is PropertyInfo prop)
            {
                // We treat properties as methods named "get_PropName" with 0 args
                string signature = "get_" + prop.Name;
                return new SetupContext<TReturn>(mockSetup, signature, Array.Empty<object?>());
            }

            throw new NotSupportedException("Only methods and properties are supported.");
        }

        // AOT-Safe Evaluator (Basic support for Constants and Captured Variables)
        private static object? Evaluate(Expression expr)
        {
            if (expr is ConstantExpression c) return c.Value;
            
            // Handle captured variables (closures)
            if (expr is MemberExpression m && m.Expression is ConstantExpression container)
            {
                if (m.Member is FieldInfo f) return f.GetValue(container.Value);
                if (m.Member is PropertyInfo p) return p.GetValue(container.Value);
            }
            
            return null; // Fallback (For complex expressions, we'd need a fuller interpreter)
        }
    }

    public class SetupContext<T>
    {
        private readonly IMockSetup _mock;
        private readonly string _signature;
        private readonly object?[] _args;

        public SetupContext(IMockSetup mock, string signature, object?[] args)
        {
            _mock = mock;
            _signature = signature;
            _args = args;
        }

        public void Returns(T value)
        {
            _mock.AddSetup(_signature, _args, value);
        }
    }

    // The Logic Engine that stores and retrieves return values
    public class MockHandler
    {
        // List of (Signature, Args, ReturnValue)
        private readonly List<(string Sig, object?[] Args, object? Val)> _setups = new();

        public void AddSetup(string signature, object?[] args, object? value)
        {
            // Remove existing setup if exact match (override behavior)
            _setups.RemoveAll(s => s.Sig == signature && ArraysEqual(s.Args, args));
            _setups.Add((signature, args, value));
        }

        public object? Invoke(string signature, object?[] args)
        {
            // Find last matching setup (LIFO behavior is standard for mocks)
            var match = _setups.LastOrDefault(s => s.Sig == signature && ArraysEqual(s.Args, args));
            
            // Return value or default
            return match.Val; 
        }

        private bool ArraysEqual(object?[] a, object?[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!Equals(a[i], b[i])) return false;
            }
            return true;
        }
    }
}