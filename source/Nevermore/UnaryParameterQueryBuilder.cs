namespace Nevermore
{
    public interface IUnaryParameterQueryBuilder<TRecord> where TRecord : class
    {
        IQueryBuilder<TRecord> ParameterValue(object value);
        IQueryBuilder<TRecord> ParameterDefault(object defaultValue);
    }

    public class UnaryParameterQueryBuilder<TRecord> : IUnaryParameterQueryBuilder<TRecord> where TRecord : class
    {
        readonly IQueryBuilder<TRecord> queryBuilder;
        readonly Parameter parameter;

        public UnaryParameterQueryBuilder(IQueryBuilder<TRecord> queryBuilder, Parameter parameter)
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