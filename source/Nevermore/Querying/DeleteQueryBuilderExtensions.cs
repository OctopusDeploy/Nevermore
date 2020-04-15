using System.Collections;
using System.Linq;

namespace Nevermore.Querying
{
    public static class DeleteQueryBuilderExtensions
    {
        public static IDeleteQueryBuilder<TRecord> Where<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string fieldName, UnarySqlOperand operand, object value) where TRecord : class
        {
            var parameter = new Parameter(fieldName);
            return queryBuilder.WhereParameterised(fieldName, operand, parameter)
                .ParameterValue(value);
        }

        public static IDeleteQueryBuilder<TRecord> Where<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string fieldName, BinarySqlOperand operand, object startValue, object endValue) where TRecord : class
        {
            var startParameter = new Parameter("StartValue");
            var endParameter = new Parameter("EndValue");

            return queryBuilder.WhereParameterised(fieldName, operand, startParameter, endParameter)
                .ParameterValues(startValue, endValue);
        }

        public static IDeleteQueryBuilder<TRecord> Where<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string fieldName, ArraySqlOperand operand, IEnumerable values) where TRecord : class
        {
            var stringValues = values.OfType<object>().Select(v => v.ToString()).ToArray();
            var parameters = stringValues.Select((v, i) => new Parameter($"{fieldName}{i}")).ToArray();
            return queryBuilder.WhereParameterised(fieldName, operand, parameters).ParameterValues(stringValues);
        }

        public static IDeleteQueryBuilder<TRecord> Parameter<TRecord>(this IDeleteQueryBuilder<TRecord> queryBuilder,
            string parameterName, object value) where TRecord : class
        {
            return queryBuilder.Parameter(new Parameter(parameterName), value);
        }
    }
}