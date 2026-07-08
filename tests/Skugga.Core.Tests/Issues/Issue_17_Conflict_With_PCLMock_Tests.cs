using PCLMock;

namespace Skugga.Core.Tests.Issues
{
    public class Issue_17_Conflict_With_PCLMock_Tests
    {
        public interface IThing
        {
            void DoSomething();
        }

        public sealed class ThingMock : MockBase<IThing>, IThing
        {
            public ThingMock(PCLMock.MockBehavior behavior = PCLMock.MockBehavior.Strict)
                : base(behavior) { }

            public void DoSomething() => Apply(x => x.DoSomething());
        }

        [Fact]
        public void Verify_PCLMockAndSkugga_Succeeds()
        {
            var pclMock = new ThingMock();
            pclMock.When(x => x.DoSomething());
            pclMock.DoSomething();
            pclMock.Verify(x => x.DoSomething());

            var skugga = Mock.Create<IThing>();
            skugga.DoSomething();
            skugga.Verify(x => x.DoSomething(), Times.AtLeast(1));
        }
    }
}
