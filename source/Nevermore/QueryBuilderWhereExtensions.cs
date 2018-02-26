using System;
using System.Linq;
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
            if (expr is BinaryExpression binExpr)
            {
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

            if (expr is MethodCallExpression methExpr)
            {
                if(methExpr.Arguments.Count == 1 && methExpr.Arguments[0].Type == typeof(string))
                    return AddStringMethodFromExpression(queryBuilder, methExpr);
                
                if(methExpr.Method.Name == "Contains")
                    return AddContainsFromExpression(queryBuilder, methExpr);
                
                throw new NotSupportedException("Only method calls that take a single string argument and Enumerable.Contains methods are supported");
            }

            throw new NotSupportedException($"The predicate supplied is not supported. Only simple BinaryExpressions, LogicalBinaryExpressions and some MethodCallExpressions are supported. The predicate is a {expr.GetType()}.");

        }

        static IQueryBuilder<TRecord> AddContainsFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, MethodCallExpression methExpr)
            where TRecord : class
        {
            var property = GetProperty(methExpr.Arguments.Count == 1 ? methExpr.Arguments[0] : methExpr.Arguments[1]);
            var value = GetValueFromExpression(methExpr.Arguments.Count == 1 ? methExpr.Object : methExpr.Arguments[0], property.PropertyType);

            return queryBuilder.Where(property.Name, SqlOperand.In, value);
        }
        

        static IQueryBuilder<TRecord> AddStringMethodFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, MethodCallExpression methExpr) 
            where TRecord : class
        {
            var property = GetProperty(methExpr.Object);
            var fieldName = property.Name;

            var value = (string) GetValueFromExpression(methExpr.Arguments[0], typeof(string));

            switch (methExpr.Method.Name)
            {
                case "Contains":
                    return queryBuilder.Where(fieldName, SqlOperand.Contains, value);
                case "StartsWith":
                    return queryBuilder.Where(fieldName, SqlOperand.StartsWith, value);
                case "EndsWith":
                    return queryBuilder.Where(fieldName, SqlOperand.EndsWith, value);
                default:
                    throw new NotSupportedException($"The method {methExpr.Method.Name} is not supported. Only Contains, StartWith and EndsWith is supported");
            }
        }


        static PropertyInfo GetProperty(Expression expression)
        {
            if (expression is UnaryExpression unaryExpr)
                expression = unaryExpr.Operand;

            if (expression is MemberExpression memberExpr && memberExpr.Member is PropertyInfo pi)
                return pi;

            throw new NotSupportedException(
                $"The left hand side of the predicate must be a property accessor (PropertyExpression or UnaryExpression). It is a {(expression?.GetType().Name ?? "<unknown>")}.");
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
            var property = GetProperty(binaryExpression.Left);

            var value = GetValueFromExpression(binaryExpression.Right, property.PropertyType);
            var fieldName = property.Name;
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