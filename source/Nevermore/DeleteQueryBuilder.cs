using System.Collections.Generic;
using System.Linq;
using Nevermore.AST;

namespace Nevermore
{
    public class DeleteQueryBuilder<TRecord> : IDeleteQueryBuilder<TRecord> where TRecord : class
    {
        readonly IRelationalTransaction relationalTransaction;
        readonly string tableName;
        readonly IEnumerable<IWhereClause> whereClauses;
        readonly CommandParameterValues parameterValues;

        public DeleteQueryBuilder(IRelationalTransaction relationalTransaction, string tableName, IEnumerable<IWhereClause> whereClauses, CommandParameterValues parameterValues)
        {
            this.relationalTransaction = relationalTransaction;
            this.tableName = tableName;
            this.whereClauses = whereClauses;
            this.parameterValues = parameterValues;
        }

        public IDeleteQueryBuilder<TRecord> Where(string whereClause)
        {
            return AddWhereClause(new CustomWhereClause(whereClause));
        }

        public IDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            return AddWhereClause(new UnaryWhereClause(new WhereFieldReference(fieldName), operand,
                parameter.ParameterName));
        }

        public IDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter,
            Parameter endValueParameter)
        {
            return AddWhereClause(new BinaryWhereClause(new WhereFieldReference(fieldName), operand,
                startValueParameter.ParameterName, endValueParameter.ParameterName));
        }

        public IDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames)
        {
            var parameterNamesList = parameterNames.ToList();
            if (!parameterNamesList.Any())
            {
                return AddWhereClause(AlwaysFalseWhereClause());
            }

            return AddWhereClause(new ArrayWhereClause(new WhereFieldReference(fieldName), operand,
                parameterNamesList.Select(p => p.ParameterName).ToList()));
        }

        static CustomWhereClause AlwaysFalseWhereClause()
        {
            return new CustomWhereClause("0 = 1");
        }

        IDeleteQueryBuilder<TRecord> AddWhereClause(IWhereClause clause)
        {
            return new DeleteQueryBuilder<TRecord>(relationalTransaction, tableName, whereClauses.Concat(new [] {clause}), parameterValues);
        }

        public IDeleteQueryBuilder<TRecord> Parameter(Parameter parameter, object value)
        {
            return new DeleteQueryBuilder<TRecord>(relationalTransaction, tableName, whereClauses, 
                new CommandParameterValues(parameterValues) {{parameter.ParameterName, value}});
        }

        public void Delete()
        {
            var whereClausesList = whereClauses.ToList();
            var where = whereClausesList.Any() ? new Where(new AndClause(whereClausesList)) : new Where(); 
            var deleteQuery = new Delete(new SimpleTableSource(tableName), where).GenerateSql();
            relationalTransaction.ExecuteRawDeleteQuery(deleteQuery, parameterValues);
        }
    }
}