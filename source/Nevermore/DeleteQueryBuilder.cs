using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nevermore.AST;
using Nevermore.Mapping;
using Nevermore.Util;

namespace Nevermore
{
    internal class DeleteQueryBuilder<TRecord> : IDeleteQueryBuilder<TRecord> where TRecord : class
    {
        readonly IUniqueParameterNameGenerator uniqueParameterNameGenerator;
        readonly Action<Type, Where, CommandParameterValues> executeDelete;
        readonly IEnumerable<IWhereClause> whereClauses;
        readonly CommandParameterValues parameterValues;

        public DeleteQueryBuilder(
            IUniqueParameterNameGenerator uniqueParameterNameGenerator, 
            Action<Type, Where, CommandParameterValues> executeDelete, 
            IEnumerable<IWhereClause> whereClauses, 
            CommandParameterValues parameterValues)
        {
            this.uniqueParameterNameGenerator = uniqueParameterNameGenerator;
            this.executeDelete = executeDelete;
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
            var uniqueParameter = new UniqueParameter(uniqueParameterNameGenerator, parameter);
            return new UnaryParameterDeleteQueryBuilder<TRecord>(
                AddWhereClause(new UnaryWhereClause(new WhereFieldReference(fieldName), operand, uniqueParameter.ParameterName)), 
                uniqueParameter);
        }

        public IBinaryParametersDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName,
            BinarySqlOperand operand, Parameter startValueParameter,
            Parameter endValueParameter)
        {
            var uniqueStartParameter = new UniqueParameter(uniqueParameterNameGenerator, startValueParameter);
            var uniqueEndParameter = new UniqueParameter(uniqueParameterNameGenerator, endValueParameter);
            return new BinaryParametersDeleteQueryBuilder<TRecord>(
                AddWhereClause(new BinaryWhereClause(new WhereFieldReference(fieldName), operand, uniqueStartParameter.ParameterName, uniqueEndParameter.ParameterName)), 
                uniqueStartParameter, 
                uniqueEndParameter);
        }

        public IArrayParametersDeleteQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand,
            IEnumerable<Parameter> parameterNames)
        {
            var parameterNamesList = parameterNames.Select(p => new UniqueParameter(uniqueParameterNameGenerator, p)).ToList();
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
            return new DeleteQueryBuilder<TRecord>(uniqueParameterNameGenerator, executeDelete, whereClauses.Concat(new [] {clause}), parameterValues);
        }

        public IDeleteQueryBuilder<TRecord> Parameter(Parameter parameter, object value)
        {
            return new DeleteQueryBuilder<TRecord>( 
                uniqueParameterNameGenerator, 
                executeDelete, 
                whereClauses, 
                new CommandParameterValues(parameterValues) {{parameter.ParameterName, value}});
        }

        public IDeleteQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class
        {
            return new DeleteQueryBuilder<TNewRecord>(uniqueParameterNameGenerator, executeDelete, whereClauses, parameterValues);
        }

        public void Delete()
        {
            var whereClausesList = whereClauses.ToList();
            var where = whereClausesList.Any() ? new Where(new AndClause(whereClausesList)) : new Where();

            executeDelete(typeof(TRecord), where, parameterValues);
        }
    }
}