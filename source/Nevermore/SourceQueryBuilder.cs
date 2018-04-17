using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.AST;

namespace Nevermore
{
    public class SubquerySourceBuilder<TRecord> : SourceQueryBuilder<TRecord>, ISubquerySourceBuilder<TRecord> where TRecord : class
    {
        readonly ISelect select;
        string alias;

        public SubquerySourceBuilder(ISelect select, 
            string alias, 
            IRelationalTransaction relationalTransaction, 
            ITableAliasGenerator tableAliasGenerator, 
            IUniqueParameterGenerator uniqueParameterGenerator, 
            CommandParameterValues parameterValues, 
            Parameters parameters, 
            ParameterDefaults parameterDefaults) 
            : base(relationalTransaction, tableAliasGenerator, uniqueParameterGenerator, parameterValues, parameters, parameterDefaults)
        {
            this.select = select;
            this.alias = alias;
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
                RelationalTransaction, 
                TableAliasGenerator, 
                UniqueParameterGenerator, 
                new CommandParameterValues(ParamValues, parameterValues), 
                new Parameters(Params, parameters), 
                new ParameterDefaults(ParamDefaults, parameterDefaults));
        }

        public ISubquerySource AsSource()
        {
            return new SubquerySource(select, alias);
        }

        public ISubquerySourceBuilder<TRecord> Alias(string subqueryAlias)
        {
            alias = subqueryAlias;
            return this;
        }
    }

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
            IRelationalTransaction relationalTransaction, 
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterGenerator uniqueParameterGenerator,
            CommandParameterValues parameterValues, 
            Parameters parameters, 
            ParameterDefaults parameterDefaults) 
            : base(relationalTransaction, tableAliasGenerator, uniqueParameterGenerator, parameterValues, parameters, parameterDefaults)
        {
            this.originalSource = originalSource;
            clauses = new List<JoinClause>();
            joinSource = nextJoin;
            type = joinType;
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            if (clauses.Count == 0)
            {
                throw new InvalidOperationException("Must have at least one 'ON' clause per join");
            }
            var joinedSource = new JoinedSource(originalSource, intermediateJoins.Concat(new [] {new Join(clauses.ToList(), joinSource, type)}).ToList());
            return new JoinSelectBuilder(joinedSource);
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults)
        {
            if (clauses.Count == 0)
            {
                throw new InvalidOperationException("Must have at least one 'ON' clause per join");
            }

            intermediateJoins.Add(new Join(clauses.ToList(), joinSource, type));
            clauses = new List<JoinClause>();
            joinSource = source;
            type = joinType;
            return this;
        }

        public IJoinSourceQueryBuilder<TRecord> On(string leftField, JoinOperand operand, string rightField)
        {
            var newClause = new JoinClause(leftField, operand, rightField);
            clauses.Add(newClause);
            return this;
        }
    }

    public class TableSourceQueryBuilder<TRecord> : SourceQueryBuilder<TRecord>, ITableSourceQueryBuilder<TRecord> where TRecord : class
    {
        string tableOrViewName;
        string alias;

        public TableSourceQueryBuilder(string tableOrViewName, 
            IRelationalTransaction relationalTransaction, 
            ITableAliasGenerator tableAliasGenerator, 
            IUniqueParameterGenerator uniqueParameterGenerator,
            CommandParameterValues parameterValues,
            Parameters parameters,
            ParameterDefaults parameterDefaults) 
            : base(relationalTransaction, tableAliasGenerator, uniqueParameterGenerator, parameterValues, parameters, parameterDefaults)
        {
            this.tableOrViewName = tableOrViewName;
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            return new TableSelectBuilder(CreateSimpleTableSource());
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults)
        {
            return new JoinSourceQueryBuilder<TRecord>(CreateAliasedTableSource(), 
                joinType,
                source, 
                RelationalTransaction, 
                TableAliasGenerator, 
                UniqueParameterGenerator, 
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
            return CreateAliasedTableSource();
        }

        public IQueryBuilder<TRecord> Hint(string tableHint)
        {
            var source = new TableSourceWithHint(CreateSimpleTableSource(), tableHint);
            return CreateQueryBuilder(new TableSelectBuilder(source));
        }

        ISimpleTableSource CreateSimpleTableSource()
        {
            if (alias == null)
            {
                return new SimpleTableSource(tableOrViewName);
            }
            return new AliasedTableSource(new SimpleTableSource(tableOrViewName), alias);
        }
        
        AliasedTableSource CreateAliasedTableSource()
        {
            return new AliasedTableSource(new SimpleTableSource(tableOrViewName), alias ?? TableAliasGenerator.GenerateTableAlias(tableOrViewName));
        }
    }

    public abstract class SourceQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        protected readonly IRelationalTransaction RelationalTransaction;
        protected readonly ITableAliasGenerator TableAliasGenerator;
        protected readonly IUniqueParameterGenerator UniqueParameterGenerator;
        protected readonly CommandParameterValues ParamValues;
        protected readonly Parameters Params;
        protected readonly ParameterDefaults ParamDefaults;

        protected SourceQueryBuilder(IRelationalTransaction relationalTransaction, 
            ITableAliasGenerator tableAliasGenerator, 
            IUniqueParameterGenerator uniqueParameterGenerator,
            CommandParameterValues parameterValues, 
            Parameters parameters, 
            ParameterDefaults parameterDefaults)
        {
            RelationalTransaction = relationalTransaction;
            TableAliasGenerator = tableAliasGenerator;
            UniqueParameterGenerator = uniqueParameterGenerator;
            ParamValues = parameterValues;
            Params = parameters;
            ParamDefaults = parameterDefaults;
        }

        protected abstract ISelectBuilder CreateSelectBuilder();

        IQueryBuilder<TRecord> Builder
        {
            get
            {
                var selectBuilder = CreateSelectBuilder();
                return CreateQueryBuilder(selectBuilder);
            }
        }

        protected IQueryBuilder<TRecord> CreateQueryBuilder(ISelectBuilder selectBuilder)
        {
            return new QueryBuilder<TRecord, ISelectBuilder>(selectBuilder, RelationalTransaction, TableAliasGenerator, UniqueParameterGenerator, ParamValues, Params, ParamDefaults);
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            return Builder.Where(whereClause);
        }

        public IUnaryParameterQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            return Builder.WhereParameterised(fieldName, operand, parameter);
        }

        public IBinaryParametersQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand,
            Parameter startValueParameter, Parameter endValueParameter)
        {
            return Builder.WhereParameterised(fieldName, operand, startValueParameter, endValueParameter);
        }

        public IArrayParametersQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand,
            IEnumerable<Parameter> parameterNames)
        {
            return Builder.WhereParameterised(fieldName, operand, parameterNames);
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName)
        {
            return Builder.OrderBy(fieldName);
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName)
        {
            return Builder.OrderByDescending(fieldName);
        }

        public IQueryBuilder<TRecord> Column(string name)
        {
            return Builder.Column(name);
        }

        public IQueryBuilder<TRecord> Column(string name, string columnAlias)
        {
            return Builder.Column(name, columnAlias);
        }

        public IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias)
        {
            return Builder.Column(name, columnAlias, tableAlias);
        }

        public IQueryBuilder<TRecord> AllColumns()
        {
            return Builder.AllColumns();
        }

        public IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias)
        {
            return Builder.CalculatedColumn(expression, columnAlias);
        }

        public IQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class
        {
            return Builder.AsType<TNewRecord>();
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias)
        {
            return Builder.AddRowNumberColumn(columnAlias);
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns)
        {
            return Builder.AddRowNumberColumn(columnAlias, partitionByColumns);
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns)
        {
            return Builder.AddRowNumberColumn(columnAlias, partitionByColumns);
        }

        public IQueryBuilder<TRecord> Parameter(Parameter parameter)
        {
            return Builder.Parameter(parameter);
        }

        public IQueryBuilder<TRecord> ParameterDefault(Parameter parameter, object defaultValue)
        {
            return Builder.ParameterDefault(parameter, defaultValue);
        }

        public IQueryBuilder<TRecord> Parameter(Parameter parameter, object value)
        {
            return Builder.Parameter(parameter, value);
        }

        public abstract IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults);
        public ISubquerySourceBuilder<TRecord> Union(IQueryBuilder<TRecord> queryBuilder)
        {
            return Builder.Union(queryBuilder);
        }

        public ISubquerySourceBuilder<TRecord> Subquery()
        {
            return Builder.Subquery();
        }

        public ISelectBuilder GetSelectBuilder()
        {
            return Builder.GetSelectBuilder();
        }

        public int Count()
        {
            return Builder.Count();
        }

        public bool Any()
        {
            return Builder.Any();
        }

        public TRecord First()
        {
            return Builder.First();
        }

        public IEnumerable<TRecord> Take(int take)
        {
            return Builder.Take(take);
        }

        public List<TRecord> ToList(int skip, int take)
        {
            return Builder.ToList(skip, take);
        }

        public List<TRecord> ToList(int skip, int take, out int totalResults)
        {
            return Builder.ToList(skip, take, out totalResults);
        }

        public List<TRecord> ToList()
        {
            return Builder.ToList();
        }

        public IEnumerable<TRecord> Stream()
        {
            return Builder.Stream();
        }

        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Builder.ToDictionary(keySelector);
        }

        public Parameters Parameters => Builder.Parameters;
        public ParameterDefaults ParameterDefaults => Builder.ParameterDefaults;
        public CommandParameterValues ParameterValues => Builder.ParameterValues;

        public string DebugViewRawQuery()
        {
            return Builder.DebugViewRawQuery();
        }
    }
}