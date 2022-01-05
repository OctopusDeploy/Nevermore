using System.Linq.Expressions;

namespace Nevermore.Util
{
    public static class MemberExpressionExtensions
    {
        public static bool IsBasedOff<TExpression>(this MemberExpression memberExpression) where TExpression : Expression
        {
            do
            {
                if (memberExpression.Expression is TExpression)
                {
                    return true;
                }
                memberExpression = memberExpression.Expression as MemberExpression;
            } while (memberExpression is not null);

            return false;
        }
    }
}