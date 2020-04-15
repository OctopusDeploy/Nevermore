namespace Nevermore.Querying
{
    public interface IBinaryParametersDeleteQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Provides values for the parameters which were declared in the previous statement
        /// </summary>
        /// <param name="startValue">The value of the start value parameter</param>
        /// <param name="endValue">The value of the end value parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IDeleteQueryBuilder<TRecord> ParameterValues(object startValue, object endValue);
    }

    public class BinaryParametersDeleteQueryBuilder<TRecord> : IBinaryParametersDeleteQueryBuilder<TRecord> where TRecord : class
    {
        readonly IDeleteQueryBuilder<TRecord> deleteQueryBuilder;
        readonly UniqueParameter startParameter;
        readonly UniqueParameter endParameter;

        public BinaryParametersDeleteQueryBuilder(IDeleteQueryBuilder<TRecord> deleteQueryBuilder, UniqueParameter startParameter, UniqueParameter endParameter)
        {
            this.deleteQueryBuilder = deleteQueryBuilder;
            this.startParameter = startParameter;
            this.endParameter = endParameter;
        }

        public IDeleteQueryBuilder<TRecord> ParameterValues(object startValue, object endValue)
        {
            return deleteQueryBuilder
                .Parameter(startParameter, startValue)
                .Parameter(endParameter, endValue);
        }
    }
}