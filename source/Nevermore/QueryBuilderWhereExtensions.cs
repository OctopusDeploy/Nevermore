using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nevermore.Util;

namespace Nevermore
{
    public static class QueryBuilderWhereExtensions
    {
        /// <summary>
        /// Adds a where expression to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">The expression that will be converted into a where clause in the query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, string>> expression)
            where TRecord : class => queryBuilder.Where((string) GetValueFromExpression(expression.Body, typeof(string)));

        /// <summary>
        /// Adds a where expression to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="predicate">A predicate which will be converted into a where clause in the query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, bool>> predicate)
            where TRecord : class => AddWhereClauseFromExpression(queryBuilder, predicate.Body);


        static IQueryBuilder<TRecord> AddWhereClauseFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, Expression expr)
            where TRecord : class
        {
            if (expr is BinaryExpression binExpr)
            {
                switch (binExpr.NodeType)
                {
                    case ExpressionType.Equal:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.Equal, binExpr);
                    case ExpressionType.NotEqual:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.NotEqual, binExpr);
                    case ExpressionType.GreaterThan:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.GreaterThan, binExpr);
                    case ExpressionType.GreaterThanOrEqual:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.GreaterThanOrEqual, binExpr);
                    case ExpressionType.LessThan:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.LessThan, binExpr);
                    case ExpressionType.LessThanOrEqual:
                        return AddUnaryWhereClauseFromExpression(queryBuilder, UnarySqlOperand.LessThanOrEqual, binExpr);
                    case ExpressionType.AndAlso:
                        return AddLogicalAndOperatorWhereClauses(queryBuilder, binExpr);
                    default:
                        throw new NotSupportedException($"The operand {binExpr.NodeType.ToString()} is not supported.");
                }
            }

            if (expr is MethodCallExpression methExpr)
            {
                if (methExpr.Arguments.Count == 1 && methExpr.Method.DeclaringType == typeof(string))
                    return AddStringMethodFromExpression(queryBuilder, methExpr);

                if (methExpr.Method.Name == "Contains")
                    return AddContainsFromExpression(queryBuilder, methExpr);
                
                if (methExpr.Method.Name == "In")
                    return AddInExpression(ArraySqlOperand.In, queryBuilder, methExpr);
                
                if (methExpr.Method.Name == "NotIn")
                    return AddInExpression(ArraySqlOperand.NotIn, queryBuilder, methExpr);

                throw new NotSupportedException("Only method calls that take a single string argument and Enumerable.Contains methods are supported");
            }

            IQueryBuilder<TRecord> HandleMemberExpression(MemberExpression memExpr, bool rhsValue)
            {
                if (memExpr.Type == typeof(bool))
                    return queryBuilder.Where(memExpr.Member.Name, UnarySqlOperand.Equal, rhsValue);

                throw new NotSupportedException("Only boolean properties are allowed for where expressions without a comparison operator or method call");
            }

            if (expr is MemberExpression memExpr2)
                return HandleMemberExpression(memExpr2, true);

            if (expr is UnaryExpression unaExpr)
            {
                if (!(unaExpr.Operand is MemberExpression memExpr3))
                    throw new NotSupportedException("Only boolean properties are allowed when the ! operator is used, i.e. Where(e => !e.BoolProp)");

                return HandleMemberExpression(memExpr3, false);
            }

            throw new NotSupportedException($"The predicate supplied is not supported. Only simple BinaryExpressions, LogicalBinaryExpressions and some MethodCallExpressions are supported. The predicate is a {expr.GetType()}.");
        }

        static IQueryBuilder<TRecord> AddContainsFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, MethodCallExpression call) where TRecord : class
        {
            var property = GetProperty(call.Arguments.Count == 1 ? call.Arguments[0] : call.Arguments[1]);
            var value = (IEnumerable) GetValueFromExpression(call.Arguments.Count == 1 ? call.Object : call.Arguments[0], property.PropertyType);

            return queryBuilder.Where(property.Name, ArraySqlOperand.In, value);
        }

        static IQueryBuilder<TRecord> AddInExpression<TRecord>(ArraySqlOperand operand, IQueryBuilder<TRecord> queryBuilder, MethodCallExpression call) where TRecord : class
        {
            var property = GetProperty(call.Arguments[0]);
            var value = (IEnumerable) GetValueFromExpression(call.Arguments[1], property.PropertyType);
            
            return queryBuilder.Where(property.Name, operand, value);
        }

        static IQueryBuilder<TRecord> AddStringMethodFromExpression<TRecord>(IQueryBuilder<TRecord> queryBuilder, MethodCallExpression methExpr) where TRecord : class
        {
            var property = GetProperty(methExpr.Object);
            var fieldName = property.Name;

            var value = (string) GetValueFromExpression(methExpr.Arguments[0], typeof(string));

            switch (methExpr.Method.Name)
            {
                case "Contains":
                    return queryBuilder.Where(fieldName, UnarySqlOperand.Like, $"%{value}%");
                case "StartsWith":
                    return queryBuilder.Where(fieldName, UnarySqlOperand.Like, $"{value}%");
                case "EndsWith":
                    return queryBuilder.Where(fieldName, UnarySqlOperand.Like, $"%{value}");
                default:
                    throw new NotSupportedException($"The method {methExpr.Method.Name} is not supported. Only Contains, StartWith and EndsWith is supported");
            }
        }

        static IQueryBuilder<TRecord> AddLogicalAndOperatorWhereClauses<TRecord>(IQueryBuilder<TRecord> queryBuilder, BinaryExpression binExpr) where TRecord : class
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
            where TRecord : class
        {
            var property = GetProperty(binaryExpression.Left);

            var value = GetValueFromExpression(binaryExpression.Right, property.PropertyType);
            var fieldName = property.Name;

            if (value == null && new[] {UnarySqlOperand.Equal, UnarySqlOperand.NotEqual}.Contains(operand))
                return operand == UnarySqlOperand.Equal ? queryBuilder.WhereNull(fieldName) : queryBuilder.WhereNotNull(fieldName);
            return queryBuilder.Where(fieldName, operand, value);
        }

        static object GetValueFromExpression(Expression expression, Type resultType)
        {
            object result;

            if (expression is ConstantExpression constExpr)
            {
                result = constExpr.Value;
            }
            else
            {
                var objectMember = Expression.Convert(expression, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                result = getterLambda.Compile()();
            }

            if (resultType.GetTypeInfo().IsEnum &&
                result != null &&
                result.GetType().GetTypeInfo().IsPrimitive)
                return Enum.ToObject(resultType, result);
            
            return result;
        }

        /// <summary>
        /// Adds a unary where expression to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="operand">The SQL operator to be used in the where clause</param>
        /// <param name="value">The value to compare against the column values. It will be added to the query as a parameter.</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            UnarySqlOperand operand, object value) where TRecord : class
        {
            return queryBuilder.WhereParameterized(fieldName, operand, new Parameter(fieldName)).ParameterValue(value);
        }

        /// <summary>
        /// Adds a binary where expression to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="operand">The SQL operator to be used in the where clause</param>
        /// <param name="startValue">The first or starting value to be used to compare against the column values in the where clause. It will be added to the query as a parameter.</param>
        /// <param name="endValue">The second or ending value to be used to compare against the column values in the where clause. It will be added to the query as a parameter.</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            BinarySqlOperand operand, object startValue, object endValue) where TRecord : class
        {
            return queryBuilder.WhereParameterized(fieldName, operand, new Parameter("StartValue"), new Parameter("EndValue"))
                .ParameterValues(startValue, endValue);
        }

        /// <summary>
        /// Adds a where between or equal expression to the query.
        /// This determines if a column value is within the provided range by adding two unary where expressions to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="startValue">The first or starting value to be used to compare against the column values in the where clause. It will be added to the query as a parameter.</param>
        /// <param name="endValue">The second or ending value to be used to compare against the column values in the where clause. It will be added to the query as a parameter.</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> WhereBetweenOrEqual<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            string fieldName, object startValue, object endValue) where TRecord : class
        {
            return queryBuilder.WhereParameterized(fieldName, UnarySqlOperand.GreaterThanOrEqual, new Parameter("StartValue"))
                .ParameterValue(startValue)
                .WhereParameterized(fieldName, UnarySqlOperand.LessThanOrEqual, new Parameter("EndValue"))
                .ParameterValue(endValue);
        }

        /// <summary>
        /// Adds an array based where clause expression to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="operand">The SQL operator to be used in the where clause</param>
        /// <param name="values">The values to compare against the column values. Each value will be added to the query as a separate parameter.</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            ArraySqlOperand operand, IEnumerable values) where TRecord : class
        {
            var valuesList = values.OfType<object>().ToList();
            ListExtender.ExtendListRepeatingLastValue(valuesList);
            
            var parameters = valuesList.Select((v, i) => new Parameter($"{fieldName}{i + 1}")).ToArray();
            return queryBuilder.WhereParameterized(fieldName, operand, parameters).ParameterValues(valuesList);
        }

        /// <summary>
        /// Provides a value for a parameter that has already been added to the query.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="name">The name of the parameter for which the value applies</param>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> Parameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string name,
            object value) where TRecord : class
        {
            var parameter = new Parameter(name);
            return queryBuilder.Parameter(parameter, value);
        }

        /// <summary>
        /// Provides a value for a parameter that has already been added to the query.
        /// The provided value will be surrounded with "%" characters in the SQL string.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="name">The name of the parameter for which the value applies</param>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> LikeParameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            string name, object value) where TRecord : class
        {
            var likeValue = (value ?? string.Empty).ToString().EscapeForLikeComparison();
            return queryBuilder.Parameter(name,
                "%" + likeValue + "%");
        }

        /// <summary>
        /// Provides a value for a parameter that has already been added to the query.
        /// The provided value is a pipe separated list of values, and will be surrounded with "%" characters in the SQL string.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="name">The name of the parameter for which the value applies</param>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        [Pure] public static IQueryBuilder<TRecord> LikePipedParameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            string name, object value) where TRecord : class
        {
            return queryBuilder.Parameter(name,
                "%|" + (value ?? string.Empty).ToString().EscapeForLikeComparison() + "|%");
        }
    }
}