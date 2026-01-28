#nullable enable
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Skugga.Core
{
    /// <summary>
    /// Helper class to parse LINQ expressions for Mock.Of{T}.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class LinqToMocks
    {
        public static void ApplyFromExpression<T>(T mock, Expression<Func<T, bool>> predicate) where T : class
        {
            ArgumentNullException.ThrowIfNull(predicate);

            ParseExpression(mock, predicate.Body);
        }

        private static void ParseExpression<T>(T mock, Expression expression) where T : class
        {
            // Unwrap booleans
            // e.g. x.IsActive -> x.IsActive == true
            // !x.IsActive -> x.IsActive == false

            if (expression is BinaryExpression binaryExpr)
            {
                // Handle AND (x.Prop == 1 && x.Other == 2)
                if (binaryExpr.NodeType == ExpressionType.AndAlso)
                {
                    ParseExpression(mock, binaryExpr.Left);
                    ParseExpression(mock, binaryExpr.Right);
                    return;
                }

                // Handle Equality (x.Prop == Value)
                if (binaryExpr.NodeType == ExpressionType.Equal)
                {
                    SetupEquality(mock, binaryExpr.Left, binaryExpr.Right);
                    return;
                }
            }

            // Handle boolean properties (implicit == true)
            // e.g. x => x.IsActive
            if (expression is MemberExpression memberExpr && memberExpr.Type == typeof(bool))
            {
                SetupBoolean(mock, memberExpr, true);
                return;
            }

            // Handle negated boolean properties (!x.IsActive)
            if (expression is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Not)
            {
                if (unaryExpr.Operand is MemberExpression mem)
                {
                    SetupBoolean(mock, mem, false);
                    return;
                }
            }

            throw new NotSupportedException($"Expression '{expression}' is not supported by Mock.Of<T>.");
        }

        private static void SetupEquality<T>(T mock, Expression left, Expression right) where T : class
        {
            // Expect left to be MemberAccess (x.Prop)
            if (!(left is MemberExpression memberExpr))
            {
                // Maybe right is the member? (1 == x.Id)
                if (right is MemberExpression revMember)
                {
                    SetupEquality(mock, revMember, left);
                    return;
                }
                throw new NotSupportedException($"Left side of equality must be a property access. Found: {left}");
            }

            // Evaluate right side
            var value = Evaluate(right);

            // Apply Setup
            ApplySetup(mock, memberExpr.Member.Name, value);
        }

        private static void SetupBoolean<T>(T mock, MemberExpression memberExpr, bool value) where T : class
        {
            ApplySetup(mock, memberExpr.Member.Name, value);
        }

        private static object? Evaluate(Expression expr)
        {
            if (expr is ConstantExpression c) return c.Value;

            // Fallback: compile and invoke
            var lambda = Expression.Lambda(expr);
            return lambda.Compile().DynamicInvoke();
        }

        private static void ApplySetup<T>(T mock, string memberName, object? returnValue) where T : class
        {
            var handler = Mock.Get(mock).Handler;

            // We need to know if it's a property or method (Mock.Of usually targets properties)
            // Simulating a property getter setup:
            // method name "get_TheProp"

            var getterName = "get_" + memberName;

            // Should verify member exists? 
            // Assume property for Mock.Of convention.

            // Setup(mock, x => x.Prop).Returns(value)
            // Internally: handler.AddSetup("get_Prop", empty_args, returnValue)

            handler.AddSetup(getterName, Array.Empty<object?>(), returnValue, null);
        }
    }
}
