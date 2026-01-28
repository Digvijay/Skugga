using System;
using Skugga.Core;

namespace AdvancedFeatures
{
    public interface IService
    {
        int Id { get; }
        string Name { get; }
        bool IsActive { get; }

        bool TryParse(string input, out int value);
        void Increment(ref int value);
    }

    public abstract class AbstractService
    {
        public string Execute(string input) => ProcessCore(input);
        protected abstract string ProcessCore(string input);
        protected abstract int RetryCount { get; }
    }

    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Skugga Advanced Features Demo");
            Console.WriteLine("=============================");

            RunLinqToMocks();
            RunRefOutSupport();
            RunMockRepository();
            RunProtectedMocking();
        }

        private static void RunLinqToMocks()
        {
            Console.WriteLine("\n1. LINQ to Mocks (Mock.Of)");

            // Functional setup of properties
            var service = Mock.Of<IService>(s =>
                s.Id == 1 &&
                s.Name == "Demo Service" &&
                s.IsActive
            );

            Console.WriteLine($"Id: {service.Id}");         // 1
            Console.WriteLine($"Name: {service.Name}");     // Demo Service
            Console.WriteLine($"Active: {service.IsActive}"); // True
        }

        private static void RunRefOutSupport()
        {
            Console.WriteLine("\n2. Ref/Out Parameter Support");

            var mock = Mock.Create<IService>();

            // Setup out parameter - use dummy variable in setup
            int outDummy = 0;
            mock.Setup(x => x.TryParse("100", out outDummy))
                .Returns(true)
                .OutValue(1, 100);  // Parameter index 1 (second parameter)

            int outResult;
            bool success = mock.TryParse("100", out outResult);
            Console.WriteLine($"TryParse: {success}, Value: {outResult}"); // True, 100

            // Setup ref parameter - use It.IsAny<T>() for matching
            int refDummy = It.IsAny<int>();
            mock.Setup(x => x.Increment(ref refDummy))
                .RefValue(0, 50);  // Parameter index 0 (first parameter)

            int refValue = 10;
            mock.Increment(ref refValue);
            Console.WriteLine($"Increment Ref: {refValue}"); // 50
        }

        private static void RunMockRepository()
        {
            Console.WriteLine("\n3. MockRepository");

            var repo = new MockRepository(MockBehavior.Strict);

            // Create verified mocks
            var service1 = repo.Create<IService>();

            service1.Setup(x => x.Name).Returns("Repo Mock");

            Console.WriteLine($"Repo Mock Name: {service1.Name}");

            // Verify all mocks in repo
            repo.VerifyAll();
            Console.WriteLine("Repository Verified!");
        }

        private static void RunProtectedMocking()
        {
            Console.WriteLine("\n4. Protected Members");

            var mock = Mock.Create<AbstractService>();

            // Setup protected method
            mock.Protected()
                .Setup<string>("ProcessCore", It.IsAny<string>())
                .Returns("Mocked Protected");

            var result = mock.Execute("test");
            Console.WriteLine($"Result: {result}"); // Mocked Protected
        }
    }
}
