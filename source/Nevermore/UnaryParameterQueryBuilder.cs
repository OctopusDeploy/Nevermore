namespace Nevermore
{
    public interface IUnaryParameterQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Provides a value for the parameter which was declared in the previous statement
        /// </summary>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterValue(object value);

        /// <summary>
        /// Provides a default value for the parameter which was declared in the previous statement
        /// </summary>
        /// <param name="defaultValue">The default value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterDefault(object defaultValue);
    }

    public class UnaryParameterQueryBuilder<TRecord> : IUnaryParameterQueryBuilder<TRecord> where TRecord : class
    {
        readonly IQueryBuilder<TRecord> queryBuilder;
        readonly UniqueParameter parameter;

        public UnaryParameterQueryBuilder(IQueryBuilder<TRecord> queryBuilder, UniqueParameter parameter)
        {
            this.queryBuilder = queryBuilder;
            this.parameter = parameter;
        }

        public IQueryBuilder<TRecord> ParameterValue(object value)
        {
            return queryBuilder.Parameter(parameter, value);
        }

        public IQueryBuilder<TRecord> ParameterDefault(object defaultValue)
        {
            return queryBuilder.ParameterDefault(parameter, defaultValue);
        }
    }
}