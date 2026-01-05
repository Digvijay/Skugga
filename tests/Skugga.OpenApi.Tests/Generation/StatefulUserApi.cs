using Skugga.Core;

namespace Skugga.OpenApi.Tests.Generation.StatefulUsers
{
    /// <summary>
    /// Test interface for stateful mock behavior.
    /// Demonstrates CRUD operations with in-memory entity tracking.
    /// </summary>
    [SkuggaFromOpenApi("specs/stateful-users.yaml", StatefulBehavior = true)]
    public partial interface IStatefulUserApi
    {
    }
}

namespace Skugga.OpenApi.Tests.Generation.StatelessUsers
{
    /// <summary>
    /// Test interface WITHOUT stateful behavior for comparison.
    /// </summary>
    [SkuggaFromOpenApi("specs/stateful-users.yaml")]
    public partial interface IStatelessUserApi
    {
    }
}
