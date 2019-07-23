using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public interface IArrayParametersDeleteQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Provides values for the parameters which were declared in the previous statement
        /// </summary>
        /// <param name="values">The values of the parameters</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IDeleteQueryBuilder<TRecord> ParameterValues(IEnumerable<object> values);
    }

    public class ArrayParametersDeleteQueryBuilder<TRecord> : IArrayParametersDeleteQueryBuilder<TRecord> where TRecord : class
    {
        readonly IDeleteQueryBuilder<TRecord> deleteQueryBuilder;
        readonly IReadOnlyList<UniqueParameter> parameters;

        public ArrayParametersDeleteQueryBuilder(IDeleteQueryBuilder<TRecord> deleteQueryBuilder, IReadOnlyList<UniqueParameter> parameters)
        {
            this.deleteQueryBuilder = deleteQueryBuilder;
            this.parameters = parameters;
        }

        public IDeleteQueryBuilder<TRecord> ParameterValues(IEnumerable<object> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count != parameters.Count)
            {
                throw new ArgumentException("The number of values provided must be the same as the number of parameters in the query. " +
                                            $"Number of parameters: ${parameters.Count}; Number of values: ${valuesList.Count}", nameof(values));
            }
            
            return valuesList.Zip(parameters, (value, parameter) => new {value, parameter})
                .Aggregate(deleteQueryBuilder, (qb, pv) => qb.Parameter(pv.parameter, pv.value));
        }
    }
}