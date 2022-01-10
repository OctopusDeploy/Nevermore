using System.Linq.Expressions;

namespace Nevermore.Util
{
    public static class MemberExpressionExtensions
    {
        public static bool IsBasedOff<TExpression>(this MemberExpression memberExpression) where TExpression : Expression
        {
            var visitor = new FindExpressionTypeVisitor<TExpression>();
            return visitor.Find(memberExpression);
        }

        class FindExpressionTypeVisitor<TExpression> : ExpressionVisitor where TExpression : Expression
        {
            bool found;

            public bool Find(Expression expression)
            {
                base.Visit(expression);
                return found;
            }

            public override Expression Visit(Expression node)
            {
                if (node is TExpression)
                {
                    found = true;
                }
                return base.Visit(node);
            }
        }
    }
}