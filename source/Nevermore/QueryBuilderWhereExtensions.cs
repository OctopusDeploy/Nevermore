using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Nevermore
{
    public static class QueryBuilderWhereExtensions
    {
        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, string>> expression)
            where TRecord : class
            => queryBuilder.Where((string) GetValueFromExpression(expression.Body, typeof(string)));

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, bool>> predicate)
            where TRecord : class
            => AddWhereClauseFromExpression(queryBuilder, predicate.Body);

        static IQueryBuilder<TRecord> AddWhereClauseFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, Expression expr)
            where TRecord : class
        {
            if (!(expr is BinaryExpression binExpr))
                throw new NotSupportedException(
                    $"The predicate supplied is not supported. Only simple BinaryExpressions and LogicalBinaryExpressions are supported. The predicate is a {expr.GetType()}.");

            switch (binExpr.NodeType)
            {
                case ExpressionType.Equal:
                    return AddUnaryWhereClauseFromExpression(queryBuilder, SqlOperand.Equal, binExpr);
                case ExpressionType.GreaterThan:
                    return AddUnaryWhereClauseFromExpression(queryBuilder, SqlOperand.GreaterThan, binExpr);
                case ExpressionType.GreaterThanOrEqual:
                    return AddUnaryWhereClauseFromExpression(queryBuilder, SqlOperand.GreaterThanOrEqual, binExpr);
                case ExpressionType.LessThan:
                    return AddUnaryWhereClauseFromExpression(queryBuilder, SqlOperand.LessThan, binExpr);
                case ExpressionType.LessThanOrEqual:
                    return AddUnaryWhereClauseFromExpression(queryBuilder, SqlOperand.LessThanOrEqual, binExpr);
                case ExpressionType.NotEqual:
                    return AddUnaryWhereClauseFromExpression(queryBuilder, SqlOperand.NotEqual, binExpr);
                case ExpressionType.AndAlso:
                    return AddLogicalAndOperatorWhereClauses(queryBuilder, binExpr);
                default:
                    throw new NotSupportedException($"The operand {binExpr.NodeType.ToString()} is not supported.");
            }
        }

        static IQueryBuilder<TRecord> AddLogicalAndOperatorWhereClauses<TRecord>(IQueryBuilder<TRecord> queryBuilder, BinaryExpression binExpr)
            where TRecord : class
        {
            queryBuilder = AddWhereClauseFromExpression(queryBuilder, binExpr.Left);
            return AddWhereClauseFromExpression(queryBuilder, binExpr.Right);
        }

        static IQueryBuilder<TRecord> AddUnaryWhereClauseFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, SqlOperand operand, BinaryExpression binaryExpression)
            where TRecord : class
        {
            PropertyInfo GetLeft()
            {
                var leftExpr = binaryExpression.Left;
                if (binaryExpression.Left is UnaryExpression unaryExpr)
                    leftExpr = unaryExpr.Operand;

                if (leftExpr is MemberExpression memberExpr && memberExpr.Member is PropertyInfo pi)
                    return pi;

                throw new NotSupportedException(
                    $"The left hand side of the predicate must be a property accessor (PropertyExpression or UnaryExpression). It is a {binaryExpression.Left.GetType()}.");
            }

            var left = GetLeft();

            var value = GetValueFromExpression(binaryExpression.Right, left.PropertyType);
            var fieldName = left.Name;
            return queryBuilder.Where(fieldName, operand, value);
        }

        static object GetValueFromExpression(Expression expression, Type resultType)
        {
            if (expression is ConstantExpression constExpr)
                return resultType.GetTypeInfo().IsEnum
                    ? Enum.ToObject(resultType, constExpr.Value)
                    : constExpr.Value;

            var objectMember = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            return getterLambda.Compile()();
        }
    }
}