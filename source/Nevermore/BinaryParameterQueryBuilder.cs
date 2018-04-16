namespace Nevermore
{
    public interface IBinaryParametersQueryBuilder<TRecord> where TRecord : class
    {
        IQueryBuilder<TRecord> ParameterValues(object startValue, object endValue);
        IQueryBuilder<TRecord> ParameterDefaults(object defaultStartValue, object defaultEndValue);
    }

    public class BinaryParametersQueryBuilder<TRecord> : IBinaryParametersQueryBuilder<TRecord> where TRecord : class
    {
        readonly IQueryBuilder<TRecord> queryBuilder;
        readonly Parameter startParameter;
        readonly Parameter endParameter;

        public BinaryParametersQueryBuilder(IQueryBuilder<TRecord> queryBuilder, Parameter startParameter, Parameter endParameter)
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