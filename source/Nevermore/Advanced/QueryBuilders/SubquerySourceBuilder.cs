using Nevermore.Advanced.SelectBuilders;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.QueryBuilders
{
    public class SubquerySourceBuilder<TRecord> : SourceQueryBuilder<TRecord>, ISubquerySourceBuilder<TRecord> where TRecord : class
    {
        readonly ISelect select;
        string alias;

        public SubquerySourceBuilder(ISelect select,
            IReadQueryExecutor readQueryExecutor,
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterNameGenerator uniqueParameterNameGenerator,
            CommandParameterValues parameterValues,
            Parameters parameters,
            ParameterDefaults parameterDefaults)
            : base(readQueryExecutor, tableAliasGenerator, uniqueParameterNameGenerator, parameterValues, parameters, parameterDefaults)
        {
            this.select = select;
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            return new SubquerySelectBuilder(AsSource());
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults)
        {
            return new JoinSourceQueryBuilder<TRecord>(AsSource(),
                joinType,
                source,
                ReadQueryExecutor,
                TableAliasGenerator,
                UniqueParameterNameGenerator,
                new CommandParameterValues(ParamValues, parameterValues),
                new Parameters(Params, parameters),
                new ParameterDefaults(ParamDefaults, parameterDefaults));
        }

        public ISubquerySource AsSource()
        {
            if (string.IsNullOrEmpty(alias))
            {
                Alias(TableAliasGenerator.GenerateTableAlias());
            }

            return new SubquerySource(select, alias);
        }

        public ISubquerySourceBuilder<TRecord> Alias(string subqueryAlias)
        {
            alias = subqueryAlias;
            return this;
        }
    }
}