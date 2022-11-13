using Nevermore.Advanced.SelectBuilders;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.QueryBuilders
{
    public class TableSourceQueryBuilder<TRecord> : SourceQueryBuilder<TRecord>, ITableSourceQueryBuilder<TRecord> where TRecord : class
    {
        string tableOrViewName;
        string alias;
        string schemaName;
        string idColumnName;
        UnaryWhereParameter typeFilter;

        public TableSourceQueryBuilder(string tableOrViewName,
            string schemaName,
            string idColumnName,
            string typeColumnName,
            object typeColumnValue,
            IReadTransaction readQueryExecutor,
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterNameGenerator uniqueParameterNameGenerator,
            CommandParameterValues parameterValues,
            Parameters parameters,
            ParameterDefaults parameterDefaults)
            : base(readQueryExecutor, tableAliasGenerator, uniqueParameterNameGenerator, parameterValues, parameters, parameterDefaults)
        {
            this.schemaName = schemaName;
            this.tableOrViewName = tableOrViewName;
            this.idColumnName = idColumnName;

            if (!string.IsNullOrEmpty(typeColumnName) && typeColumnValue is not null)
            {
                var parameter = new UniqueParameter(UniqueParameterNameGenerator, new Parameter("__type"));
                ParamValues[parameter.ParameterName] = typeColumnValue;
                typeFilter = new UnaryWhereParameter(typeColumnName, UnarySqlOperand.Equal, parameter);
            }
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            var builder = new TableSelectBuilder(CreateSimpleTableSource(), new Column(idColumnName));
            if (typeFilter is not null)
            {
                builder.AddWhere(typeFilter);
            }
            return builder;
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults)
        {
            return new JoinSourceQueryBuilder<TRecord>(AsAliasedSource(),
                joinType,
                source,
                ReadQueryExecutor,
                TableAliasGenerator,
                UniqueParameterNameGenerator,
                new CommandParameterValues(ParamValues, parameterValues),
                new Parameters(Params, parameters),
                new ParameterDefaults(ParamDefaults, parameterDefaults));
        }

        public ITableSourceQueryBuilder<TRecord> View(string viewName)
        {
            tableOrViewName = viewName;
            return this;
        }

        public ITableSourceQueryBuilder<TRecord> Table(string tableName)
        {
            tableOrViewName = tableName;
            return this;
        }

        public ITableSourceQueryBuilder<TRecord> Alias(string tableAlias)
        {
            alias = tableAlias;
            return this;
        }

        public IAliasedSelectSource AsAliasedSource()
        {
            return typeFilter is null ? CreateAliasedTableSource() : CreateSubquerySource();
        }

        public IQueryBuilder<TRecord> Hint(string tableHint)
        {
            var source = new TableSourceWithHint(CreateSimpleTableSource(), tableHint);
            return CreateQueryBuilder(new TableSelectBuilder(source, new Column(idColumnName)));
        }

        ISimpleTableSource CreateSimpleTableSource()
        {
            var columnNames = ReadQueryExecutor.GetColumnNames(schemaName, tableOrViewName);

            return alias == null 
                ? new SimpleTableSource(tableOrViewName, schemaName, columnNames) 
                : new AliasedTableSource(new SimpleTableSource(tableOrViewName, schemaName, columnNames), alias);
        }

        AliasedTableSource CreateAliasedTableSource()
        {
            var columnNames = ReadQueryExecutor.GetColumnNames(schemaName, tableOrViewName);
            return new AliasedTableSource(new SimpleTableSource(tableOrViewName, schemaName, columnNames), alias ?? TableAliasGenerator.GenerateTableAlias(tableOrViewName));
        }

        SubquerySource CreateSubquerySource()
        {
            if (string.IsNullOrEmpty(alias))
            {
                Alias(TableAliasGenerator.GenerateTableAlias());
            }
            var select = CreateSelectBuilder().GenerateSelectWithoutDefaultOrderBy();
            return new SubquerySource(select, alias);
        }
    }
}