namespace Nevermore
{
    public interface IBinaryParametersQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Provides values for the parameters which were declared in the previous statement
        /// </summary>
        /// <param name="startValue">The value of the start value parameter</param>
        /// <param name="endValue">The value of the end value parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterValues(object startValue, object endValue);

        /// <summary>
        /// Provides default values for the parameters which were declared in the previous statement.
        /// </summary>
        /// <param name="defaultStartValue">The default value of the start value parameter</param>
        /// <param name="defaultEndValue">The default value of the end value parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterDefaults(object defaultStartValue, object defaultEndValue);
    }

    public class BinaryParametersQueryBuilder<TRecord> : IBinaryParametersQueryBuilder<TRecord> where TRecord : class
    {
        readonly IQueryBuilder<TRecord> queryBuilder;
        readonly UniqueParameter startParameter;
        readonly UniqueParameter endParameter;

        public BinaryParametersQueryBuilder(IQueryBuilder<TRecord> queryBuilder, UniqueParameter startParameter, UniqueParameter endParameter)
        {
            this.queryBuilder = queryBuilder;
            this.startParameter = startParameter;
            this.endParameter = endParameter;
        }

        public IQueryBuilder<TRecord> ParameterValues(object startValue, object endValue)
        {
            return queryBuilder
                .Parameter(startParameter, startValue)
                .Parameter(endParameter, endValue);
        }

        public IQueryBuilder<TRecord> ParameterDefaults(object defaultStartValue, object defaultEndValue)
        {
            return queryBuilder
                .ParameterDefault(startParameter, defaultStartValue)
                .ParameterDefault(endParameter, defaultEndValue);
        }
    }
}