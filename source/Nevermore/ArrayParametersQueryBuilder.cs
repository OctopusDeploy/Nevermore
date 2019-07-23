using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public interface IArrayParametersQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Provides values for the parameters which were declared in the previous statement
        /// </summary>
        /// <param name="values">The values of the parameters</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterValues(IEnumerable<object> values);

        /// <summary>
        /// Provides default values for the parameters which were declared in the previous statement
        /// </summary>
        /// <param name="defaultValues">The default values of the parameters</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterDefaults(IEnumerable<object> defaultValues);
    }

    public class ArrayParametersQueryBuilder<TRecord> : IArrayParametersQueryBuilder<TRecord> where TRecord : class
    {
        readonly IQueryBuilder<TRecord> queryBuilder;
        readonly IReadOnlyList<UniqueParameter> parameters;

        public ArrayParametersQueryBuilder(IQueryBuilder<TRecord> queryBuilder, IReadOnlyList<UniqueParameter> parameters)
        {
            this.queryBuilder = queryBuilder;
            this.parameters = parameters;
        }

        public IQueryBuilder<TRecord> ParameterValues(IEnumerable<object> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count != parameters.Count)
            {
                throw new ArgumentException("The number of values provided must be the same as the number of parameters in the query. " +
                                            $"Number of parameters: ${parameters.Count}; Number of values: ${valuesList.Count}", nameof(values));
            }

            return valuesList.Zip(parameters, (value, parameter) => new {value, parameter})
                .Aggregate(queryBuilder, (qb, pv) => qb.Parameter(pv.parameter, pv.value));
        }

        public IQueryBuilder<TRecord> ParameterDefaults(IEnumerable<object> defaultValues)
        {
            var defaultValuesList = defaultValues.ToList();
            if (defaultValuesList.Count != parameters.Count)
            {
                throw new ArgumentException("The number of default values provided must be the same as the number of parameters in the query. " +
                                            $"Number of parameters: ${parameters.Count}; Number of default values: ${defaultValuesList.Count}", nameof(defaultValues));
            }

            return defaultValuesList.Zip(parameters, (defaultValue, parameter) => new {defaultValue, parameter})
                .Aggregate(queryBuilder, (qb, pv) => qb.ParameterDefault(pv.parameter, pv.defaultValue));
        }
    }
}