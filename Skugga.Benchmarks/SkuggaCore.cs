using System.Linq.Expressions;

namespace Skugga.Core
{
    public interface IMockSetup
    {
        void SetupMethod(string signature, object? value);
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
            if (expression.Body is MethodCallExpression methodCall)
            {
                string signature = methodCall.Method.Name; 
                return new SetupContext<TReturn>(mock as IMockSetup, signature);
            }
            throw new NotSupportedException("Only method calls are supported.");
        }
    }

    public class SetupContext<T>
    {
        private readonly IMockSetup? _mock;
        private readonly string _signature;

        public SetupContext(IMockSetup? mock, string signature)
        {
            _mock = mock;
            _signature = signature;
        }

        public void Returns(T value)
        {
            if (_mock == null) throw new InvalidOperationException("Mock was not generated correctly.");
            _mock.SetupMethod(_signature, value);
        }
    }
}
