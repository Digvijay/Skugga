using Skugga.Core;

namespace Skugga.OpenApi.Tests.Generation.ValidatedProducts
{
    // Interface with validation enabled
    [SkuggaFromOpenApi("specs/validation-test.yaml", ValidateContracts = true)]
    public partial interface IValidatedProductApi
    {
    }
}

namespace Skugga.OpenApi.Tests.Generation.UnvalidatedProducts
{
    // Interface with validation disabled (for comparison)
    [SkuggaFromOpenApi("specs/validation-test.yaml")]
    public partial interface IUnvalidatedProductApi
    {
    }
}
