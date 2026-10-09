using System;
using System.Linq.Expressions;

namespace Nevermore.Util
{
    static class SpanExpressionExtensions
    {
        // C# 14 binds array.Contains(x) to MemoryExtensions.Contains(ReadOnlySpan<T>, T),
        // wrapping the collection in an implicit conversion to a span. Unwrap it so we can evaluate the underlying collection.
        public static Expression UnwrapImplicitSpanConversion(this Expression expression)
        {
            if (expression is MethodCallExpression { Method: { Name: "op_Implicit" } } call && call.Arguments.Count == 1 && IsSpanType(call.Type))
                return call.Arguments[0];
            if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary && IsSpanType(unary.Type))
                return unary.Operand;
            return expression;
        }

        static bool IsSpanType(Type type)
            => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>) || type.GetGenericTypeDefinition() == typeof(Span<>));
    }
}
