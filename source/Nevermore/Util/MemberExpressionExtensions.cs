using System.Linq.Expressions;

namespace Nevermore.Util
{
    public static class MemberExpressionExtensions
    {
        public static bool IsBasedOff<TExpression>(this MemberExpression memberExpression) where TExpression : Expression
        {
            var visitor = new FindExpressionTypeVisitor<TExpression>();
            return visitor.Find(memberExpression) != null;
        }

        public static TExpression FindChildOfType<TExpression>(this MemberExpression memberExpression) where TExpression : Expression
        {
            var visitor = new FindExpressionTypeVisitor<TExpression>();
            return visitor.Find(memberExpression);
        }

        class FindExpressionTypeVisitor<TExpression> : ExpressionVisitor where TExpression : Expression
        {
            TExpression found;

            public TExpression Find(Expression expression)
            {
                base.Visit(expression);
                return found;
            }

            public override Expression Visit(Expression node)
            {
                if (node is TExpression matchingExpression)
                {
                    found = matchingExpression;
                }
                return base.Visit(node);
            }
        }
    }
}