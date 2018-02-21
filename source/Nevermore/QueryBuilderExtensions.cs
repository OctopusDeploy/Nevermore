using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public static IOrderedQueryBuilder<TRecord> ThenBy<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder, string orderByClause)
        {
            return queryBuilder.OrderBy(orderByClause);
        }

        public static IOrderedQueryBuilder<TRecord> ThenByDescending<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder, string orderByClause)
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

    public static class QueryBuilderExtensions
    {
        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName, SqlOperand operand, object value)
        {
            switch(operand)
            {
                case SqlOperand.Equal:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Equal, value);
                case SqlOperand.GreaterThan:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.GreaterThan, value);
                case SqlOperand.GreaterThanOrEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.GreaterThanOrEqual, value);
                case SqlOperand.LessThan:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.LessThan, value);
                case SqlOperand.LessThanOrEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.LessThanOrEqual, value);
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
                    throw new ArgumentException($"The operand {operand} is not valid with only one value", nameof(operand));
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
                    return queryBuilder.WhereParameterised(fieldName, BinarySqlOperand.Between, startValueParameter, endValueParameter)
                        .Parameter(startValueParameter, startValue)
                        .Parameter(endValueParameter, endValue);
                case SqlOperand.BetweenOrEqual:
                    return queryBuilder.WhereParameterised(fieldName, UnarySqlOperand.GreaterThanOrEqual, startValueParameter)
                        .Parameter(startValueParameter, startValue)
                        .WhereParameterised(fieldName, UnarySqlOperand.LessThanOrEqual, endValueParameter)
                        .Parameter(endValueParameter, endValue);
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with two values", nameof(operand));
            }
        }

        static IQueryBuilder<TRecord> AddUnaryWhereClauseAndParameter<TRecord>(IQueryBuilder<TRecord> queryBuilder, string fieldName, UnarySqlOperand operand, object value)
        {
            var parameter = new Parameter(fieldName);
            return queryBuilder.WhereParameterised(fieldName, operand, parameter)
                .Parameter(parameter, value);
        }

        static IQueryBuilder<TRecord> AddWhereIn<TRecord>(IQueryBuilder<TRecord> queryBuilder, string fieldName, IEnumerable values)
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
                    throw new ArgumentException($"The operand {operand} is not valid with a list of values", nameof(operand));
            }
        }

        public static IQueryBuilder<TRecord> Parameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string name, object value)
        {
            var parameter = new Parameter(name);
            return queryBuilder.Parameter(parameter, value);
        }

        public static IQueryBuilder<TRecord> LikeParameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string name, object value)
        {
            return queryBuilder.Parameter(name, "%" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "%");
        }

        public static IQueryBuilder<TRecord> LikePipedParameter<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string name, object value)
        {
            return queryBuilder.Parameter(name, "%|" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "|%");
        }
    }
}