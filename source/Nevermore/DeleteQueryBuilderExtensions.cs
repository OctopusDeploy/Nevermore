using System.Collections;
using System.Linq;
using Nevermore.AST;

namespace Nevermore
{
    public static class DeleteQueryBuilderExtensions
    {
        public static IDeleteQueryBuilder<TRecord> Where<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string fieldName, UnarySqlOperand operand, object value) where TRecord : class
        {
            var parameter = new Parameter(queryBuilder.GenerateUniqueParameterName(fieldName));
            return queryBuilder.WhereParameterised(fieldName, operand, parameter)
                .Parameter(parameter, value);
        }

        public static IDeleteQueryBuilder<TRecord> Where<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string fieldName, BinarySqlOperand operand, object startValue, object endValue) where TRecord : class
        {
            var startParameter = new Parameter(queryBuilder.GenerateUniqueParameterName("StartValue"));
            var endParameter = new Parameter(queryBuilder.GenerateUniqueParameterName("EndValue"));

            return queryBuilder.WhereParameterised(fieldName, operand, startParameter, endParameter)
                .Parameter(startParameter, startValue)
                .Parameter(endParameter, endValue);
        }

        public static IDeleteQueryBuilder<TRecord> Where<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string fieldName, ArraySqlOperand operand, IEnumerable values) where TRecord : class
        {
            var stringValues = values.OfType<object>().Select(v => v.ToString()).ToArray();
            var parameters = stringValues.Select((v, i) => new Parameter(queryBuilder.GenerateUniqueParameterName($"{fieldName}{i}"))).ToArray();
            return stringValues.Zip(parameters, (value, parameter) => new {value, parameter})
                .Aggregate(queryBuilder.WhereParameterised(fieldName, operand, parameters),
                    (p, pv) => p.Parameter(pv.parameter, pv.value));
        }

        public static IDeleteQueryBuilder<TRecord> Parameter<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string parameterName, object value) where TRecord : class
        {
            return queryBuilder.Parameter(new Parameter(parameterName), value);
        }
    }
}