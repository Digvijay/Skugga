#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Provides argument matchers for string-based protected member setups.
    /// These return actual ArgumentMatcher instances instead of marker values.
    /// </summary>
    public static class ItExpr
    {
        /// <summary>
        /// Matches any value of type T.
        /// </summary>
        public static object IsAny<T>()
        {
            return new ArgumentMatcher<T>(_ => true, $"It.IsAny<{typeof(T).Name}>()");
        }

        /// <summary>
        /// Matches values that satisfy a custom predicate.
        /// </summary>
        public static object Is<T>(Func<T, bool> predicate)
        {
            return new ArgumentMatcher<T>(predicate, $"It.Is<{typeof(T).Name}>(...)");
        }

        /// <summary>
        /// Matches non-null values.
        /// </summary>
        public static object IsNotNull<T>()
        {
            return new ArgumentMatcher<T>(v => v != null, $"It.IsNotNull<{typeof(T).Name}>()");
        }
    }
}
