#nullable enable

namespace Skugga.Core
{
    /// <summary>
    /// Defines the behavior of a mock when un-setup members are accessed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Loose</b>: Returns default values for un-setup members (null for reference types,
    /// 0 for value types). This is the default behavior and is recommended for most scenarios.
    /// </para>
    /// <para>
    /// <b>Strict</b>: Throws a <see cref="MockException"/> when any un-setup member is accessed.
    /// Use this when you want to ensure all interactions are explicitly configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Loose behavior (default) - returns null/default for un-setup members
    /// var looseMock = Mock.Create&lt;IService&gt;(MockBehavior.Loose);
    /// var result = looseMock.GetData(); // Returns null without throwing
    ///
    /// // Strict behavior - throws for un-setup members
    /// var strictMock = Mock.Create&lt;IService&gt;(MockBehavior.Strict);
    /// var result = strictMock.GetData(); // Throws MockException
    /// </code>
    /// </example>
    public enum MockBehavior
    {
        /// <summary>
        /// Returns default values for un-setup members. This is the default and recommended behavior.
        /// </summary>
        Loose,

        /// <summary>
        /// Throws <see cref="MockException"/> when un-setup members are accessed.
        /// </summary>
        Strict
    }
}
