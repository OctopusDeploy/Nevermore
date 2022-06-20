using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Advanced.SelectBuilders;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.QueryBuilders
{
    public class JoinSourceQueryBuilder<TRecord> : SourceQueryBuilder<TRecord>, IJoinSourceQueryBuilder<TRecord> where TRecord : class
    {
        readonly IAliasedSelectSource originalSource;
        readonly List<Join> intermediateJoins = new List<Join>();
        JoinType type;
        IAliasedSelectSource joinSource;
        List<JoinClause> clauses;

        public JoinSourceQueryBuilder(IAliasedSelectSource originalSource,
            JoinType joinType,
            IAliasedSelectSource nextJoin,
            IReadQueryExecutor readQueryExecutor,
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterNameGenerator uniqueParameterNameGenerator,
            CommandParameterValues parameterValues,
            Parameters parameters,
            ParameterDefaults parameterDefaults)
            : base(readQueryExecutor, tableAliasGenerator, uniqueParameterNameGenerator, parameterValues, parameters, parameterDefaults)
        {
            this.originalSource = originalSource;
            clauses = new List<JoinClause>();
            joinSource = nextJoin;
            type = joinType;
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            ValidateJoinClauses(type);
            var joinedSource = new JoinedSource(originalSource, intermediateJoins.Concat(new [] {new Join(clauses.ToList(), joinSource, type)}).ToList());
            return new JoinSelectBuilder(joinedSource);
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults)
        {
            ValidateJoinClauses(joinType);
            intermediateJoins.Add(new Join(clauses.ToList(), joinSource, type));
            clauses = new List<JoinClause>();
            joinSource = source;
            type = joinType;

            var commandParameterValues = new CommandParameterValues(ParamValues, parameterValues);
            var combinedParameters = new Parameters(Params, parameters);
            var combinedParameterDefaults = new ParameterDefaults(ParamDefaults, parameterDefaults);

            ParamValues.Clear();
            ParamValues.AddRange(commandParameterValues);
            Params.Clear();
            Params.AddRange(combinedParameters);
            ParamDefaults.Clear();
            ParamDefaults.AddRange(combinedParameterDefaults);

            return this;
        }

        public IJoinSourceQueryBuilder<TRecord> On(string leftField, JoinOperand operand, string rightField)
        {
            return On(originalSource.Alias, leftField, operand, rightField);
        }

        public IJoinSourceQueryBuilder<TRecord> On(string leftTableAlias, string leftField, JoinOperand operand, string rightField)
        {
            var newClause = new JoinClause(leftTableAlias, leftField, operand, joinSource.Alias, rightField);
            clauses.Add(newClause);
            return this;
        }
        
        void ValidateJoinClauses(JoinType joinType)
        {
            if (joinType == JoinType.CrossApply && clauses.Any())
            {
                throw new InvalidOperationException("'CROSS APPLY' joins cannot include any join clauses");
            }

            if (joinType != JoinType.CrossApply && !clauses.Any())
            {
                throw new InvalidOperationException("Must have at least one 'ON' clause per join");
            }
        }
    }
}