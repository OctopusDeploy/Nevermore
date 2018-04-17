using System.Collections.Generic;

namespace Nevermore
{
    public interface IDeleteQueryBuilder<TRecord> where TRecord : class
    {
        IDeleteQueryBuilder<TRecord> Where(string whereClause);
        IUnaryParameterDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter);
        IBinaryParametersDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter);
        IArrayParametersDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames);

        IDeleteQueryBuilder<TRecord> Parameter(Parameter parameter, object value);

        IDeleteQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class;

        void Delete();
    }
}