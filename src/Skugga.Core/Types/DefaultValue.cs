#nullable enable

namespace Skugga.Core
{
    /// <summary>
    /// Specifies the default value strategy for un-setup members on a mock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Empty</b>: Returns CLR default values (null for reference types, 0 for value types,
    /// empty collections). This is the standard behavior.
    /// </para>
    /// <para>
    /// <b>Mock</b>: Returns mock instances for interface/abstract types (recursive mocking),
    /// and empty collections for collection types. This enables "fluent" mocking where
    /// properties that return interfaces automatically return mocks.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Empty default - properties return null
    /// var mock = Mock.Create&lt;IRepository&gt;(DefaultValue.Empty);
    /// var config = mock.Configuration; // Returns null
    ///
    /// // Mock default - properties return mocks (recursive mocking)
    /// var mock = Mock.Create&lt;IRepository&gt;(DefaultValue.Mock);
    /// var config = mock.Configuration; // Returns a mock of IConfiguration
    /// config.Setup(c => c.Timeout).Returns(30); // Can setup the nested mock
    /// </code>
    /// </example>
    public enum DefaultValue
    {
        /// <summary>
        /// Returns default CLR values (null for reference types, 0 for value types, empty collections).
        /// </summary>
        Empty,

        /// <summary>
        /// Returns mock instances for interface/abstract types (recursive mocking),
        /// empty collections for collection types.
        /// </summary>
        Mock
    }
}
