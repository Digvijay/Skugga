using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests.Setup
{
    public interface ILiteralArgumentService
    {
        decimal CalculateDiscount(decimal price, string category);
        bool ValidatePrice(decimal price);
        void SendPriceChangeNotification(int productId, decimal oldPrice, decimal newPrice);
    }

    public class LiteralArgumentTests
    {
        [Fact]
        public void Setup_WithLiteralDecimalArgument_UsesConfiguredNonDefaultReturn()
        {
            var mock = Mock.Create<ILiteralArgumentService>();
            mock.Setup(x => x.CalculateDiscount(999.99m, "Electronics")).Returns(50m);

            Assert.Equal(50m, mock.CalculateDiscount(999.99m, "Electronics"));
            Assert.Equal(0m, mock.CalculateDiscount(999.98m, "Electronics"));
        }

        [Fact]
        public void Setup_WithLiteralDecimalArgument_DoesNotHideMatchFailureBehindDefaultBool()
        {
            var mock = Mock.Create<ILiteralArgumentService>();
            mock.Setup(x => x.ValidatePrice(79.99m)).Returns(true);

            Assert.True(mock.ValidatePrice(79.99m));
            Assert.False(mock.ValidatePrice(79.98m));
        }

        [Fact]
        public void Verify_WithLiteralDecimalArguments_MatchesRecordedInvocation()
        {
            var mock = Mock.Create<ILiteralArgumentService>();

            mock.SendPriceChangeNotification(1, 999.99m, 899.99m);

            mock.Verify(x => x.SendPriceChangeNotification(1, 999.99m, 899.99m), Times.Once());
        }
    }
}
