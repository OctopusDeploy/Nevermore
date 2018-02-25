using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Nevermore.AST;

namespace Nevermore
{
    public static class TableSourceQueryBuilderExtensions
    {
        public static IQueryBuilder<TRecord> NoLock<TRecord>(this ITableSourceQueryBuilder<TRecord> queryBuilder)
        {
            return queryBuilder.Hint("NOLOCK");
        }
    }

    public static class OrderedQueryBuilderExtensions
    {
        public static IOrderedQueryBuilder<TRecord> ThenBy<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder,
            string orderByClause)
        {
            return queryBuilder.OrderBy(orderByClause);
        }

        public static IOrderedQueryBuilder<TRecord> ThenByDescending<TRecord>(
            this IOrderedQueryBuilder<TRecord> queryBuilder, string orderByClause)
        {
            return queryBuilder.OrderByDescending(orderByClause);
        }
    }

    public static class QueryBuilderJoinExtensions
    {
        public static IJoinSourceQueryBuilder<TRecord> InnerJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            IAliasedSelectSource source)
        {
            return queryBuilder.Join(source, JoinType.InnerJoin);
        }

        public static IJoinSourceQueryBuilder<TRecord> LeftHashJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            IAliasedSelectSource source)
        {
            return queryBuilder.Join(source, JoinType.LeftHashJoin);
        }
    }

    public enum SqlOperand
    {
        Equal,
        In,
        StartsWith,
        EndsWith,
        Between,
        BetweenOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        NotEqual,
        Contains
    }

    public static class QueryBuilderWhereExtensions
    {

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, string>> expression) 
            => queryBuilder.Where((string) GetValueFromExpression(expression.Body, typeof(string)));

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, bool>> predicate)
         => AddWhereClauseFromExpression(queryBuilder, predicate.Body);

        static IQueryBuilder<TRecord> AddWhereClauseFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder,
            Expression expr)
        {
            if (expr is BinaryExpression binExpr)
            {
                switch (binExpr.NodeType)
                {
                    case ExpressionType.Equal:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.Equal, binExpr);
                    case ExpressionType.GreaterThan:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.GreaterThan, binExpr);
                    case ExpressionType.GreaterThanOrEqual:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.GreaterThanOrEqual, binExpr);
                    case ExpressionType.LessThan:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.LessThan, binExpr);
                    case ExpressionType.LessThanOrEqual:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.LessThanOrEqual, binExpr);
                    case ExpressionType.NotEqual:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.NotEqual, binExpr);
                    case ExpressionType.AndAlso:
                        return AddLogicalAndOperatorWhereClauses(queryBuilder, binExpr);
                    default:
                        throw new NotSupportedException($"The operand {binExpr.NodeType.ToString()} is not supported.");
                }
            }

            if (expr is MethodCallExpression methExpr)
            {
                var property = GetProperty(methExpr.Object);
                var fieldName = property.Name;
                
                if(methExpr.Arguments.Count != 1 || methExpr.Arguments[0].Type != typeof(string))
                    throw new NotSupportedException("Only method calls that take a single string argument are supports");
                
                var value = (string) GetValueFromExpression(methExpr.Arguments[0], typeof(string));
                
                switch (methExpr.Method.Name)
                {
                    case "Contains":
                        return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"%{value}%");
                    case "StartsWith":
                        return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"{value}%");
                    case "EndsWith":
                        return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"%{value}");
                    default:
                        throw new NotSupportedException($"The method {methExpr.Method.Name} is not supported. Only Contains, StartWith and EndsWith is supported");
                }
            }

            throw new NotSupportedException($"The predicate supplied is not supported. Only simple BinaryExpressions, LogicalBinaryExpressions and some MethodCallExpressions are supported. The predicate is a {expr.GetType()}.");

        }

        static IQueryBuilder<TRecord> AddLogicalAndOperatorWhereClauses<TRecord>(IQueryBuilder<TRecord> queryBuilder, BinaryExpression binExpr)
        {
            queryBuilder = AddWhereClauseFromExpression(queryBuilder, binExpr.Left);
            return AddWhereClauseFromExpression(queryBuilder, binExpr.Right);
        }

        static PropertyInfo GetProperty(Expression expression)
        {
            if (expression is UnaryExpression unaryExpr)
                expression = unaryExpr.Operand;

            if (expression is MemberExpression memberExpr && memberExpr.Member is PropertyInfo pi)
                return pi;

            throw new NotSupportedException(
                $"The left hand side of the predicate must be a property accessor (PropertyExpression or UnaryExpression). It is a {expression.GetType()}.");
        }
        
        static IQueryBuilder<TRecord> AddUnaryWhereClauseFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, UnarySqlOperand operand, BinaryExpression binaryExpression)
        {
            var property = GetProperty(binaryExpression.Left);

            var value = GetValueFromExpression(binaryExpression.Right, property.PropertyType);
            var fieldName = property.Name;
            return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, operand, value);
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

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            SqlOperand operand, object value)
        {
            switch (operand)
            {
                case SqlOperand.Equal:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Equal, value);
                case SqlOperand.GreaterThan:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.GreaterThan, value);
                case SqlOperand.GreaterThanOrEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.GreaterThanOrEqual,
                        value);
                case SqlOperand.LessThan:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.LessThan, value);
                case SqlOperand.LessThanOrEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.LessThanOrEqual,
                        value);
                case SqlOperand.NotEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.NotEqual, value);
                case SqlOperand.Contains:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"%{value}%");
                case SqlOperand.StartsWith:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"{value}%");
                case SqlOperand.EndsWith:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"%{value}");
                case SqlOperand.In:
                    if (value is IEnumerable enumerable)
                        return AddWhereIn(queryBuilder, fieldName, enumerable);
                    else
                        throw new ArgumentException($"The operand {operand} is not valid with only one value",
                            nameof(operand));
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with only one value",
                        nameof(operand));
            }
        }

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            SqlOperand operand, object startValue, object endValue)
        {
            Parameter startValueParameter = new Parameter("StartValue");
            Parameter endValueParameter = new Parameter("EndValue");
            switch (operand)
            {
                case SqlOperand.Between:
                    return queryBuilder.WhereParameterised(fieldName, BinarySqlOperand.Between, startValueParameter,
                            endValueParameter)
                        .Parameter(startValueParameter, startValue)
                        .Parameter(endValueParameter, endValue);
                case SqlOperand.BetweenOrEqual:
                    return queryBuilder.WhereParameterised(fieldName, UnarySqlOperand.GreaterThanOrEqual,
                            startValueParameter)
                        .Parameter(startValueParameter, startValue)
                        .WhereParameterised(fieldName, UnarySqlOperand.LessThanOrEqual, endValueParameter)
                        .Parameter(endValueParameter, endValue);
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with two values", nameof(operand));
            }
        }

        static IQueryBuilder<TRecord> AddUnaryWhereClauseAndParameter<TRecord>(IQueryBuilder<TRecord> queryBuilder,
            string fieldName, UnarySqlOperand operand, object value)
        {
            var parameter = new Parameter(fieldName);
            return queryBuilder.WhereParameterised(fieldName, operand, parameter)
                .Parameter(parameter, value);
        }

        static IQueryBuilder<TRecord> AddWhereIn<TRecord>(IQueryBuilder<TRecord> queryBuilder, string fieldName,
            IEnumerable values)
        {
            var stringValues = values.OfType<object>().Select(v => v.ToString()).ToArray();
            var parameters = stringValues.Select((v, i) => new Parameter($"{fieldName}{i}")).ToArray();
            return stringValues.Zip(parameters, (value, parameter) => new {value, parameter})
                .Aggregate(queryBuilder.WhereParameterised(fieldName, ArraySqlOperand.In, parameters),
                    (p, pv) => p.Parameter(pv.parameter, pv.value));
        }

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            SqlOperand operand, IEnumerable<object> values)
        {
            switch (operand)
            {
                case SqlOperand.In:
                    return AddWhereIn(queryBuilder, fieldName, values);
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with a list of values",
                        nameof(operand));
            }
        }

        public static IQueryBuilder<TRecord> Parameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string name,
            object value)
        {
            var parameter = new Parameter(name);
            return queryBuilder.Parameter(parameter, value);
        }

        public static IQueryBuilder<TRecord> LikeParameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            string name, object value)
        {
            return queryBuilder.Parameter(name,
                "%" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "%");
        }

        public static IQueryBuilder<TRecord> LikePipedParameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            string name, object value)
        {
            return queryBuilder.Parameter(name,
                "%|" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "|%");
        }
    }
}