using Skugga.Core;
using Xunit;

namespace Skugga.Core.Tests
{
    public class ParamsArrayTests
    {
        public interface IService
        {
            int Sum(params int[] numbers);
            string Concat(string prefix, params string[] parts);
        }

        [Fact]
        public void Mock_ParamsArray_LiteralMatch()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Sum(1, 2, 3)).Returns(6);

            Assert.Equal(6, mock.Sum(1, 2, 3));
        }

        [Fact]
        public void Mock_ParamsArray_IsAnyArrayMatch()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Sum(It.IsAny<int[]>())).Returns(100);

            Assert.Equal(100, mock.Sum(1, 2, 3));
            Assert.Equal(100, mock.Sum(1, 2));
            Assert.Equal(100, mock.Sum());
        }

        [Fact]
        public void Mock_ParamsArray_MixIsAnyAndLiteral()
        {
            var mock = Mock.Create<IService>();
            mock.Setup(x => x.Concat("P:", It.IsAny<string>(), "end")).Returns("matched");

            Assert.Equal("matched", mock.Concat("P:", "middle", "end"));
        }
    }
}
