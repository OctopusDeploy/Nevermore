using Nevermore.Querying;

namespace Nevermore
{
    public interface IUnaryParameterDeleteQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Provides a value for the parameter which was declared in the previous statement
        /// </summary>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IDeleteQueryBuilder<TRecord> ParameterValue(object value);
    }

    public class UnaryParameterDeleteQueryBuilder<TRecord> : IUnaryParameterDeleteQueryBuilder<TRecord> where TRecord : class
    {
        readonly IDeleteQueryBuilder<TRecord> deleteQueryBuilder;
        readonly UniqueParameter parameter;

        public UnaryParameterDeleteQueryBuilder(IDeleteQueryBuilder<TRecord> deleteQueryBuilder, UniqueParameter parameter)
        {
            this.deleteQueryBuilder = deleteQueryBuilder;
            this.parameter = parameter;
        }

        public IDeleteQueryBuilder<TRecord> ParameterValue(object value)
        {
            return deleteQueryBuilder.Parameter(parameter, value);
        }
    }
}