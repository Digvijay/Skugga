#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Skugga.Core
{
    public enum MockBehavior { Loose, Strict }
    public class MockException : Exception { public MockException(string message) : base(message) { } }

    public static class Mock
    {
        public static T Create<T>(MockBehavior behavior = MockBehavior.Loose)
        {
            Console.WriteLine($"[Skugga Warning] Interceptor failed for {typeof(T).Name}. Using runtime fallback.");
            object proxyObj = DispatchProxy.Create<T, DynamicMockProxy<T>>();
            // Suppress null warning, DispatchProxy always returns a valid object that inherits the proxy class
            ((DynamicMockProxy<T>)proxyObj!).Handler.Behavior = behavior;
            return (T)proxyObj;
        }
    }

    public static class MockExtensions
    {
        public static SetupContext<T, TResult> Setup<T, TResult>(this T mock, Expression<Func<T, TResult>> expression)
        {
            if (mock is IMockSetup setup)
            {
                var methodCall = (MethodCallExpression)expression.Body;
                var args = methodCall.Arguments.Select(GetArgumentValue).ToArray();
                return new SetupContext<T, TResult>(setup.Handler, methodCall.Method.Name, args);
            }
            throw new ArgumentException("Object is not a Skugga Mock");
        }

        public static SetupContext<T, TResult> Setup<T, TResult>(this T mock, Expression<Func<T>> expression)
        {
            if (mock is IMockSetup setup)
            {
                var memberAccess = (MemberExpression)expression.Body;
                return new SetupContext<T, TResult>(setup.Handler, "get_" + memberAccess.Member.Name, Array.Empty<object?>());
            }
            throw new ArgumentException("Object is not a Skugga Mock");
        }

        public static void Returns<T, TResult>(this SetupContext<T, TResult> context, TResult value)
            => context.Handler.AddSetup(context.Signature, context.Args, value);

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
            if (expr is ConstantExpression c) return c.Value;
            return Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object))).Compile()();
        }
    }

    public class SetupContext<T, TResult>
    {
        public MockHandler Handler { get; }
        public string Signature { get; }
        public object?[] Args { get; }
        public SetupContext(MockHandler handler, string signature, object?[] args) { Handler = handler; Signature = signature; Args = args; }
    }

    public interface IMockSetup { MockHandler Handler { get; } }

    public class MockHandler
    {
        private readonly List<MockSetup> _setups = new();
        private ChaosPolicy? _chaosPolicy;
        private readonly Random _rng = new();
        public MockBehavior Behavior { get; set; } = MockBehavior.Loose;

        public void AddSetup(string signature, object?[] args, object? value) => _setups.Add(new MockSetup(signature, args, value));
        public void SetChaosPolicy(ChaosPolicy policy) => _chaosPolicy = policy;

        public object? Invoke(string signature, object?[] args)
        {
            if (_chaosPolicy != null && _rng.NextDouble() < _chaosPolicy.FailureRate)
                if (_chaosPolicy.PossibleExceptions?.Length > 0)
                    throw _chaosPolicy.PossibleExceptions[_rng.Next(_chaosPolicy.PossibleExceptions.Length)];

            foreach (var setup in _setups)
                if (setup.Matches(signature, args)) return setup.Value;

            if (Behavior == MockBehavior.Strict)
                throw new MockException($"[Strict Mode] Call to '{signature}' was not setup.");

            return null; 
        }
    }

    public class MockSetup 
    {
        public string Signature { get; }
        public object?[] Args { get; }
        public object? Value { get; }
        public MockSetup(string sig, object?[] args, object? val) { Signature=sig; Args=args; Value=val; }

        public bool Matches(string sig, object?[] args)
        {
            if (Signature != sig || Args.Length != args.Length) return false;
            for(int i=0; i<Args.Length; i++)
                if (Args[i] != null && !Args[i]!.Equals(args[i])) return false;
            return true;
        }
    }

    public class ChaosPolicy { public double FailureRate { get; set; } public Exception[]? PossibleExceptions { get; set; } }

    public static class AssertAllocations
    {
        public static void Zero(Action action)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            long after = GC.GetAllocatedBytesForCurrentThread();
            if (after - before > 0) throw new Exception($"Allocated {after - before} bytes (Expected 0).");
        }
    }

    public static class Harness { public static TestHarness<T> Create<T>() => new TestHarness<T>(); }
    public class TestHarness<T> { public T SUT { get; protected set; } = default!; protected Dictionary<Type, object> _mocks = new(); }

    public class DynamicMockProxy<T> : DispatchProxy, IMockSetup
    {
        public MockHandler Handler { get; } = new MockHandler();
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Handler") return Handler;
            var res = Handler.Invoke(targetMethod!.Name, args ?? Array.Empty<object?>());
            return res ?? (targetMethod.ReturnType.IsValueType && targetMethod.ReturnType != typeof(void) 
                ? Activator.CreateInstance(targetMethod.ReturnType) : null);
        }
    }

    public static class AutoScribe
    {
        public static T Capture<T>(T target)
        {
            object proxyObj = DispatchProxy.Create<T, RecordingProxy<T>>();
            ((RecordingProxy<T>)proxyObj!).Initialize(target);
            return (T)proxyObj;
        }
    }

    public class RecordingProxy<T> : DispatchProxy
    {
        private T _target = default!;
        public void Initialize(T target) => _target = target;
        protected override object? Invoke(MethodInfo? m, object?[]? args)
        {
            if (m == null) return null;
            var argsDisplay = args == null ? "" : string.Join(", ", args.Select(a => a == null ? "null" : (a is string s ? $"\"{s}\"" : a.ToString())));
            Console.WriteLine($"[AutoScribe] mock.Setup(x => x.{m.Name}({argsDisplay})).Returns( ... );");
            return m.Invoke(_target, args);
        }
    }
}