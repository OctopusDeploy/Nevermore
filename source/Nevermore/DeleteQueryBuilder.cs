using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nevermore.AST;

namespace Nevermore
{
    public class DeleteQueryBuilder<TRecord> : IDeleteQueryBuilder<TRecord> where TRecord : class
    {
        readonly IRelationalTransaction relationalTransaction;
        readonly IUniqueParameterGenerator uniqueParameterGenerator;
        readonly string tableName;
        readonly IEnumerable<IWhereClause> whereClauses;
        readonly CommandParameterValues parameterValues;

        public DeleteQueryBuilder(IRelationalTransaction relationalTransaction, 
            IUniqueParameterGenerator uniqueParameterGenerator, 
            string tableName, 
            IEnumerable<IWhereClause> whereClauses, 
            CommandParameterValues parameterValues)
        {
            this.relationalTransaction = relationalTransaction;
            this.uniqueParameterGenerator = uniqueParameterGenerator;
            this.tableName = tableName;
            this.whereClauses = whereClauses;
            this.parameterValues = parameterValues;
        }

        public IDeleteQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                var whereClauseNormalised = Regex.Replace(whereClause, @"@\w+", m => new Parameter(m.Value).ParameterName);
                return AddWhereClause(new CustomWhereClause(whereClauseNormalised));
            }

            return this;
        }

        public IUnaryParameterDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand,
            Parameter parameter)
        {
            var uniqueParameter = uniqueParameterGenerator.GenerateUniqueParameterName(parameter);
            return new UnaryParameterDeleteQueryBuilder<TRecord>(
                AddWhereClause(new UnaryWhereClause(new WhereFieldReference(fieldName), operand, uniqueParameter.ParameterName)), 
                uniqueParameter);
        }

        public IBinaryParametersDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName,
            BinarySqlOperand operand, Parameter startValueParameter,
            Parameter endValueParameter)
        {
            var uniqueStartParameter = uniqueParameterGenerator.GenerateUniqueParameterName(startValueParameter);
            var uniqueEndParameter = uniqueParameterGenerator.GenerateUniqueParameterName(endValueParameter);
            return new BinaryParametersDeleteQueryBuilder<TRecord>(
                AddWhereClause(new BinaryWhereClause(new WhereFieldReference(fieldName), operand, uniqueStartParameter.ParameterName, uniqueEndParameter.ParameterName)), 
                uniqueStartParameter, 
                uniqueEndParameter);
        }

        public IArrayParametersDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand,
            IEnumerable<Parameter> parameterNames)
        {
            var parameterNamesList = parameterNames.Select(uniqueParameterGenerator.GenerateUniqueParameterName).ToList();
            if (!parameterNamesList.Any())
            {
                return new ArrayParametersDeleteQueryBuilder<TRecord>(AddWhereClause(AlwaysFalseWhereClause()), parameterNamesList);
            }

            return new ArrayParametersDeleteQueryBuilder<TRecord>(
                AddWhereClause(new ArrayWhereClause(new WhereFieldReference(fieldName), operand, parameterNamesList.Select(p => p.ParameterName).ToList())), 
                parameterNamesList);
        }
        
        static CustomWhereClause AlwaysFalseWhereClause()
        {
            return new CustomWhereClause("0 = 1");
        }

        IDeleteQueryBuilder<TRecord> AddWhereClause(IWhereClause clause)
        {
            return new DeleteQueryBuilder<TRecord>(relationalTransaction, uniqueParameterGenerator, tableName, whereClauses.Concat(new [] {clause}), parameterValues);
        }

        public IDeleteQueryBuilder<TRecord> Parameter(Parameter parameter, object value)
        {
            return new DeleteQueryBuilder<TRecord>(relationalTransaction, 
                uniqueParameterGenerator, 
                tableName, 
                whereClauses, 
                new CommandParameterValues(parameterValues) {{parameter.ParameterName, value}});
        }

        public IDeleteQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class
        {
            return new DeleteQueryBuilder<TNewRecord>(relationalTransaction, uniqueParameterGenerator, tableName, whereClauses, parameterValues);
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