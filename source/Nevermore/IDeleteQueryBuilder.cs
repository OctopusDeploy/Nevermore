using System.Collections.Generic;

namespace Nevermore
{
    public interface IDeleteQueryBuilder<TRecord> where TRecord : class
    {
        IDeleteQueryBuilder<TRecord> Where(string whereClause);
        IDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter);
        IDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter);
        IDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames);

        IDeleteQueryBuilder<TRecord> Parameter(Parameter parameter, object value);
        string GenerateUniqueParameterName(string parameterDescription);

        IDeleteQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class;

        void Delete();
    }
}