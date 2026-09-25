#nullable enable
using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests
{
    public interface IPricingCalculator
    {
        decimal CalculateDiscount(decimal price, string category);
        decimal CalculateDiscount(decimal price);
        int Score(int value, string label);
    }

    /// <summary>
    /// Regression tests for argument matching on value-type parameters.
    ///
    /// Found via the AspNetCoreWebApi.Moq.Migration sample, whose tests configured
    /// <c>CalculateDiscount(999.99m, "Electronics")</c> and observed the mock return
    /// <c>default</c> instead of the configured value, so the discount was silently never applied.
    /// The setup call itself did not throw, which makes this the worst kind of failure for a
    /// mocking library: the test appears configured, the mock returns a default, and the assertion
    /// failure points at the system under test rather than at the mock.
    /// </summary>
    public class DecimalArgumentMatchingTests
    {
        [Fact]
        public void Setup_Matches_Decimal_And_String_Arguments()
        {
            var mock = Mock.Create<IPricingCalculator>();
            mock.Setup(p => p.CalculateDiscount(999.99m, "Electronics")).Returns(50m);

            Assert.Equal(50m, mock.CalculateDiscount(999.99m, "Electronics"));
        }

        [Fact]
        public void Setup_Matches_Single_Decimal_Argument()
        {
            var mock = Mock.Create<IPricingCalculator>();
            mock.Setup(p => p.CalculateDiscount(999.99m)).Returns(25m);

            Assert.Equal(25m, mock.CalculateDiscount(999.99m));
        }

        [Fact]
        public void Setup_Matches_Int_And_String_Arguments()
        {
            var mock = Mock.Create<IPricingCalculator>();
            mock.Setup(p => p.Score(7, "high")).Returns(99);

            Assert.Equal(99, mock.Score(7, "high"));
        }

        [Fact]
        public void Setup_Does_Not_Match_Different_Decimal_Argument()
        {
            var mock = Mock.Create<IPricingCalculator>();
            mock.Setup(p => p.CalculateDiscount(999.99m, "Electronics")).Returns(50m);

            Assert.Equal(0m, mock.CalculateDiscount(1.00m, "Electronics"));
        }
    }
}
