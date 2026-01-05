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
        /// Provides matchers for ref/out parameters.
        /// </summary>
        /// <typeparam name="T">The type of the ref/out parameter</typeparam>
        /// <remarks>
        /// <para>
        /// <b>Important:</b> Due to C# language limitations, you cannot use It.Ref in actual
        /// Setup/Verify expressions with ref/out modifiers. Instead:
        /// </para>
        /// <list type="number">
        /// <item><description>Pass a normal value (or dummy variable) in the Setup expression</description></item>
        /// <item><description>Use .OutValue() or .RefValue() extensions to configure the result</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // For out parameters
        /// int dummy = 0;
        /// mock.Setup(x => x.TryParse("42", out dummy))
        ///     .Returns(true)
        ///     .OutValue(1, 42);  // Configure out parameter value
        /// 
        /// // For ref parameters
        /// int refValue = 0;
        /// mock.Setup(x => x.Modify(ref refValue))
        ///     .RefValue(0, 100);  // Configure ref parameter value
        /// </code>
        /// </example>
        public static class Ref<T>
        {
            /// <summary>
            /// Placeholder for matching any value for a ref or out parameter.
            /// </summary>
            /// <remarks>
            /// <b>Note:</b> This is a placeholder and cannot be used in actual C# out/ref expressions.
            /// Use .OutValue()/.RefValue() extensions instead.
            /// </remarks>
            public static T IsAny => default(T)!;
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
    internal class ArgumentMatcher
    {
        /// <summary>
        /// Gets the type being matched.
        /// </summary>
        public Type MatchType { get; }
        
        /// <summary>
        /// Gets the predicate function that determines if a value matches.
        /// </summary>
        public Func<object?, bool> Predicate { get; }
        
        /// <summary>
        /// Gets the description of what this matcher matches (for error messages).
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Initializes a new ArgumentMatcher.
        /// </summary>
        /// <param name="matchType">The type being matched</param>
        /// <param name="predicate">The predicate function</param>
        /// <param name="description">Description for error messages</param>
        public ArgumentMatcher(Type matchType, Func<object?, bool> predicate, string description)
        {
            MatchType = matchType;
            Predicate = predicate;
            Description = description;
        }
        
        /// <summary>
        /// Determines if the specified value matches this matcher.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value matches; otherwise false</returns>
        /// <remarks>
        /// <para>
        /// First checks type compatibility, then evaluates the predicate.
        /// The predicate decides whether to accept null values.
        /// </para>
        /// </remarks>
        public bool Matches(object? value)
        {
            // Check type compatibility first (unless value is null)
            if (value != null && !MatchType.IsAssignableFrom(value.GetType()))
                return false;
            
            // Always run the predicate - it decides whether to accept null
            return Predicate(value);
        }
    }
}
