#nullable enable
using System;

namespace Skugga.Core
{
    /// <summary>
    /// Provides argument matching for Setup and Verify expressions.
    /// Enables flexible matching beyond exact value equality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These methods return default(T) and are never actually executed at runtime.
    /// The source generator intercepts them during compilation and converts them to ArgumentMatcher instances.
    /// </para>
    /// <para>
    /// <b>Available Matchers:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>IsAny&lt;T&gt;():</b> Matches any value of type T</description></item>
    /// <item><description><b>Is&lt;T&gt;(predicate):</b> Matches values satisfying a predicate</description></item>
    /// <item><description><b>IsIn&lt;T&gt;(values):</b> Matches values in a set</description></item>
    /// <item><description><b>IsNotNull&lt;T&gt;():</b> Matches non-null values</description></item>
    /// <item><description><b>IsRegex(pattern):</b> Matches strings matching a regex</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Match any integer
    /// mock.Setup(x => x.Process(It.IsAny&lt;int&gt;())).Returns(true);
    ///
    /// // Match positive numbers
    /// mock.Setup(x => x.Add(It.Is&lt;int&gt;(n => n > 0))).Returns(true);
    ///
    /// // Match specific values
    /// mock.Setup(x => x.SetStatus(It.IsIn("Active", "Pending"))).Returns(true);
    ///
    /// // Match non-null strings
    /// mock.Setup(x => x.Log(It.IsNotNull&lt;string&gt;())).Returns(true);
    ///
    /// // Match regex patterns
    /// mock.Setup(x => x.ValidatePhone(It.IsRegex(@"^\d{3}-\d{4}$"))).Returns(true);
    /// </code>
    /// </example>
    public static class It
    {
        /// <summary>
        /// Matches any value of type T.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <returns>A marker value (default(T)) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// This is the most commonly used matcher. It matches any value including null.
        /// </para>
        /// <para>
        /// <b>Note:</b> This method is never executed at runtime. The source generator
        /// intercepts it during compilation and replaces it with an ArgumentMatcher.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Setup accepts any integer
        /// mock.Setup(x => x.Process(It.IsAny&lt;int&gt;())).Returns(42);
        ///
        /// // Verify any string was passed
        /// mock.Verify(x => x.Log(It.IsAny&lt;string&gt;()), Times.Once());
        ///
        /// // Works with complex types too
        /// mock.Setup(x => x.Handle(It.IsAny&lt;MyClass&gt;())).Returns(true);
        /// </code>
        /// </example>
        public static T IsAny<T>()
        {
            // This method is never actually executed - it's intercepted in GetArgumentValue
            // Return default to make the expression compile
            return default(T)!;
        }

        /// <summary>
        /// Matches values that satisfy a custom predicate.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <param name="predicate">Function that returns true for matching values</param>
        /// <returns>A marker value (default(T)) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// Use this for custom matching logic. The predicate is evaluated at runtime
        /// for each method invocation to determine if the argument matches.
        /// </para>
        /// <para>
        /// The predicate should be side-effect free and deterministic.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Match positive numbers
        /// mock.Setup(x => x.Process(It.Is&lt;int&gt;(n => n > 0))).Returns("positive");
        ///
        /// // Match strings starting with "test"
        /// mock.Setup(x => x.Handle(It.Is&lt;string&gt;(s => s.StartsWith("test")))).Returns(true);
        ///
        /// // Match objects with specific property values
        /// mock.Setup(x => x.Save(It.Is&lt;User&gt;(u => u.Age >= 18))).Returns(true);
        ///
        /// // Verify with complex conditions
        /// mock.Verify(x => x.Log(It.Is&lt;LogLevel&gt;(l => l >= LogLevel.Warning)), Times.AtLeast(1));
        /// </code>
        /// </example>
        public static T Is<T>(Func<T, bool> predicate)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches values that are in the specified set.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <param name="values">Set of acceptable values</param>
        /// <returns>A marker value (default(T)) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// Convenient for matching against a known set of valid values.
        /// Uses Equals() comparison for matching.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Match specific status values
        /// mock.Setup(x => x.SetStatus(It.IsIn("Active", "Pending", "Complete"))).Returns(true);
        ///
        /// // Match specific numbers
        /// mock.Setup(x => x.Process(It.IsIn(1, 2, 3, 5, 8))).Returns("fibonacci");
        ///
        /// // Verify against set
        /// mock.Verify(x => x.SetPriority(It.IsIn(Priority.High, Priority.Critical)), Times.Once());
        /// </code>
        /// </example>
        public static T IsIn<T>(params T[] values)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches values that are in the specified collection.
        /// </summary>
        public static T IsIn<T>(IEnumerable<T> values)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches values that are NOT in the specified set.
        /// </summary>
        public static T IsNotIn<T>(params T[] values)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches values that are NOT in the specified collection.
        /// </summary>
        public static T IsNotIn<T>(IEnumerable<T> values)
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches values that are within the specified range.
        /// </summary>
        public static T IsInRange<T>(T from, T to, Range rangeKind) where T : IComparable
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches non-null values.
        /// </summary>
        /// <typeparam name="T">The type of argument to match</typeparam>
        /// <returns>A marker value (default(T)) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// Use this when you want to ensure the argument is not null, but don't care
        /// about the specific value.
        /// </para>
        /// <para>
        /// More specific than IsAny&lt;T&gt;() which also matches null.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Ensure non-null string is passed
        /// mock.Setup(x => x.Process(It.IsNotNull&lt;string&gt;())).Returns(true);
        ///
        /// // Verify non-null object was logged
        /// mock.Verify(x => x.Log(It.IsNotNull&lt;Exception&gt;()), Times.Once());
        ///
        /// // Works with nullable value types
        /// mock.Setup(x => x.Update(It.IsNotNull&lt;int?&gt;())).Returns(true);
        /// </code>
        /// </example>
        public static T IsNotNull<T>()
        {
            return default(T)!;
        }

        /// <summary>
        /// Matches strings that match a regular expression pattern.
        /// </summary>
        /// <param name="pattern">Regular expression pattern</param>
        /// <returns>A marker value (empty string) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// Convenient for validating string formats like phone numbers, emails, etc.
        /// Uses System.Text.RegularExpressions.Regex for matching.
        /// </para>
        /// <para>
        /// The pattern should be a valid .NET regular expression.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Match phone number format
        /// mock.Setup(x => x.Call(It.IsRegex(@"^\d{3}-\d{4}$"))).Returns(true);
        ///
        /// // Match email pattern
        /// mock.Setup(x => x.SendEmail(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$"))).Returns(true);
        ///
        /// // Match strings starting with "test"
        /// mock.Verify(x => x.Process(It.IsRegex(@"^test")), Times.AtLeast(1));
        ///
        /// // Match version numbers
        /// mock.Setup(x => x.ValidateVersion(It.IsRegex(@"^\d+\.\d+\.\d+$"))).Returns(true);
        /// </code>
        /// </example>
        public static string IsRegex(string pattern)
        {
            return string.Empty;
        }

        /// <summary>
        /// Matches strings that match a regular expression pattern with specified options.
        /// </summary>
        public static string IsRegex(string pattern, System.Text.RegularExpressions.RegexOptions options)
        {
            return string.Empty;
        }

        /// <summary>
        /// Provides matchers for ref and out parameters of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the ref/out parameter</typeparam>
        /// <remarks>
        /// Use <see cref="Ref{T}.IsAny"/> to match any value of the ref/out parameter.
        /// The actual value will be set using <c>RefValue()</c> or <c>OutValue()</c> on the setup.
        /// </remarks>
        public static class Ref<T>
        {
            /// <summary>
            /// Matches any value for a ref or out parameter of type <typeparamref name="T"/>.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This is a placeholder used in Setup expressions. The actual value returned
            /// via the ref/out parameter is configured using <c>RefValue()</c> or <c>OutValue()</c>.
            /// </para>
            /// </remarks>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Required for ref/out matching syntax")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA2211:Non-constant fields should not be visible", Justification = "Must be mutable for ref/out parameter syntax - readonly fields cannot be passed as ref/out")]
            public static T IsAny = default(T)!;
        }
    }

    /// <summary>
    /// Provides factory methods for creating custom reusable matchers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use Match.Create to define custom matcher methods that can be reused across tests.
    /// This is more readable than repeating complex predicates.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define custom matcher methods
    /// public static string IsLargeString() =>
    ///     Match.Create&lt;string&gt;(s => s != null && s.Length > 100, "large string");
    ///
    /// public static int IsPositive() =>
    ///     Match.Create&lt;int&gt;(n => n > 0, "positive number");
    ///
    /// // Use in tests
    /// mock.Setup(x => x.Process(IsLargeString())).Returns("handled large");
    /// mock.Setup(x => x.Add(IsPositive())).Returns(true);
    /// mock.Verify(x => x.Log(IsLargeString()), Times.AtLeast(1));
    /// </code>
    /// </example>
    public static class Match
    {
        /// <summary>
        /// Creates a custom matcher based on a predicate function.
        /// </summary>
        /// <typeparam name="T">The type of value to match</typeparam>
        /// <param name="predicate">Function that returns true if the value matches</param>
        /// <returns>A marker value (default(T)) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// Use this to create reusable matcher methods with descriptive names.
        /// The predicate is evaluated at runtime for each invocation.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create reusable matchers
        /// public static string IsValidEmail() =>
        ///     Match.Create&lt;string&gt;(s => s != null && s.Contains("@"));
        ///
        /// public static User IsAdult() =>
        ///     Match.Create&lt;User&gt;(u => u.Age >= 18);
        ///
        /// // Use in setup
        /// mock.Setup(x => x.SendEmail(IsValidEmail())).Returns(true);
        /// mock.Setup(x => x.Register(IsAdult())).Returns(true);
        /// </code>
        /// </example>
        public static T Create<T>(Func<T, bool> predicate)
        {
            return default(T)!;
        }

        /// <summary>
        /// Creates a custom matcher with a description for error messages.
        /// </summary>
        /// <typeparam name="T">The type of value to match</typeparam>
        /// <param name="predicate">Function that returns true if the value matches</param>
        /// <param name="description">Description of what the matcher matches (used in error messages)</param>
        /// <returns>A marker value (default(T)) that is replaced with a matcher during compilation</returns>
        /// <remarks>
        /// <para>
        /// The description is included in verification error messages, making failures easier to understand.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create matcher with helpful description
        /// public static int IsEven() =>
        ///     Match.Create&lt;int&gt;(n => n % 2 == 0, "even number");
        ///
        /// public static string IsJson() =>
        ///     Match.Create&lt;string&gt;(s => s?.StartsWith("{") == true, "JSON string");
        ///
        /// // Error messages will say "Expected call with even number" or "Expected call with JSON string"
        /// mock.Verify(x => x.Process(IsEven()), Times.Once());
        /// </code>
        /// </example>
        public static T Create<T>(Func<T, bool> predicate, string description)
        {
            return default(T)!;
        }
    }

    /// <summary>
    /// Internal class representing a matcher for verifying or matching method arguments.
    /// Created during compilation when It.* or Match.* methods are intercepted.
    /// </summary>
    /// <remarks>
    /// This class is not intended for direct use. The source generator creates instances
    /// when intercepting matcher method calls in Setup/Verify expressions.
    /// </remarks>
    public abstract class ArgumentMatcher
    {
        public Type MatchType { get; }
        public string Description { get; }

        protected ArgumentMatcher(Type matchType, string description)
        {
            MatchType = matchType;
            Description = description;
        }

        public abstract bool Matches(object? value);
    }

    public class ArgumentMatcher<T> : ArgumentMatcher
    {
        public Func<T, bool> Predicate { get; }

        public ArgumentMatcher(Func<T, bool> predicate, string description)
            : base(typeof(T), description)
        {
            Predicate = predicate;
        }

        public override bool Matches(object? value)
        {
            if (value != null && !MatchType.IsAssignableFrom(value.GetType()))
                return false;

            if (value is T t)
            {
                return Predicate(t);
            }

            if (value == null)
            {
                return Predicate((T)(object)null!);
            }
            return false;
        }
    }

    /// <summary>
    /// Specifies the kind of range for IsInRange matcher.
    /// </summary>
    public enum Range
    {
        /// <summary>
        /// Both from and to values are included in the range. [from, to]
        /// </summary>
        Inclusive,
        /// <summary>
        /// Both from and to values are excluded from the range. (from, to)
        /// </summary>
        Exclusive
    }
}
