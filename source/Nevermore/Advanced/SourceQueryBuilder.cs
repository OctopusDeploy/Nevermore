using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Querying;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced
{
    // A union statement is built like a subquery.
    // This is because if you add other configuration (order by, where, column selection etc), then the inner query needs to be in a subquery
    // However, if you don't add any other customizations, then the subquery is redundant and we don't need a subquery at all
    // This avoids heavily nested subqueries when unioning multiple statements.
    //
    // This behaviour is different to normal subquery building,
    // because if the consumer explicitly asks for a subquery, it would be too presumptuous of us to not create a subquery for them under some circumstances
    public class UnionSourceBuilder<TRecord> : SourceQueryBuilder<TRecord>, ISubquerySourceBuilder<TRecord> where TRecord : class
    {
        readonly ISelect select;
        string alias;

        public UnionSourceBuilder(ISelect select,
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
            return new UnionSelectBuilder(select, alias, TableAliasGenerator);
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
    }

    public class TableSourceQueryBuilder<TRecord> : SourceQueryBuilder<TRecord>, ITableSourceQueryBuilder<TRecord> where TRecord : class
    {
        string tableOrViewName;
        string alias;

        public TableSourceQueryBuilder(string tableOrViewName,
            IReadTransaction readQueryExecutor,
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterNameGenerator uniqueParameterNameGenerator,
            CommandParameterValues parameterValues,
            Parameters parameters,
            ParameterDefaults parameterDefaults)
            : base(readQueryExecutor, tableAliasGenerator, uniqueParameterNameGenerator, parameterValues, parameters, parameterDefaults)
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
        protected readonly IReadQueryExecutor ReadQueryExecutor;
        protected readonly ITableAliasGenerator TableAliasGenerator;
        protected readonly IUniqueParameterNameGenerator UniqueParameterNameGenerator;
        protected readonly CommandParameterValues ParamValues;
        protected readonly Parameters Params;
        protected readonly ParameterDefaults ParamDefaults;
        bool finished;

        protected SourceQueryBuilder(IReadQueryExecutor readQueryExecutor,
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterNameGenerator uniqueParameterNameGenerator,
            CommandParameterValues parameterValues,
            Parameters parameters,
            ParameterDefaults parameterDefaults)
        {
            ReadQueryExecutor = readQueryExecutor;
            TableAliasGenerator = tableAliasGenerator;
            UniqueParameterNameGenerator = uniqueParameterNameGenerator;
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
            return new QueryBuilder<TRecord, ISelectBuilder>(selectBuilder, ReadQueryExecutor, TableAliasGenerator, UniqueParameterNameGenerator, ParamValues, Params, ParamDefaults);
        }

        public ICompleteQuery<TRecord> WithTimeout(TimeSpan commandTimeout)
        {
            return Builder.WithTimeout(commandTimeout);
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            return Final(Builder.Where(whereClause));
        }

        public IUnaryParameterQueryBuilder<TRecord> WhereParameterized(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            return Final(Builder.WhereParameterized(fieldName, operand, parameter));
        }

        public IQueryBuilder<TRecord> WhereNull(string fieldName) => Final(Builder.WhereNull(fieldName));

        public IQueryBuilder<TRecord> WhereNotNull(string fieldName) => Final(Builder.WhereNotNull(fieldName));

        public IBinaryParametersQueryBuilder<TRecord> WhereParameterized(string fieldName, BinarySqlOperand operand,
            Parameter startValueParameter, Parameter endValueParameter)
        {
            return Final(Builder.WhereParameterized(fieldName, operand, startValueParameter, endValueParameter));
        }

        public IArrayParametersQueryBuilder<TRecord> WhereParameterized(string fieldName, ArraySqlOperand operand,
            IEnumerable<Parameter> parameterNames)
        {
            return Final(Builder.WhereParameterized(fieldName, operand, parameterNames));
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName)
        {
            return Final(Builder.OrderBy(fieldName));
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName, string tableAlias)
        {
            return Final(Builder.OrderBy(fieldName, tableAlias));
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName)
        {
            return Final(Builder.OrderByDescending(fieldName));
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName, string tableAlias)
        {
            return Final(Builder.OrderByDescending(fieldName, tableAlias));
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
            return Final(Builder.Count());
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return Final(Builder.CountAsync(cancellationToken));
        }

        public bool Any()
        {
            return Final(Builder.Any());
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return Final(Builder.AnyAsync(cancellationToken));
        }

        public TRecord First()
        {
            return Final(Builder.FirstOrDefault());
        }

        public TRecord FirstOrDefault()
        {
            return Final(Builder.FirstOrDefault());
        }

        public Task<TRecord> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            return Final(Builder.FirstOrDefaultAsync(cancellationToken));
        }

        public IEnumerable<TRecord> Take(int take)
        {
            return Final(Builder.Take(take));
        }

        public IAsyncEnumerable<TRecord> TakeAsync(int take, CancellationToken cancellationToken = default)
        {
            return Final(Builder.TakeAsync(take, cancellationToken));
        }

        public List<TRecord> ToList(int skip, int take)
        {
            return Final(Builder.ToList(skip, take));
        }

        public Task<List<TRecord>> ToListAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return Final(Builder.ToListAsync(skip, take, cancellationToken));
        }

        public List<TRecord> ToList(int skip, int take, out int totalResults)
        {
            return Final(Builder.ToList(skip, take, out totalResults));
        }

        public Task<List<TRecord>> ToListAsync(int skip, int take, out int totalResults, CancellationToken cancellationToken = default)
        {
            return Final(Builder.ToListAsync(skip, take, out totalResults, cancellationToken));
        }

        public List<TRecord> ToList()
        {
            return Final(Builder.ToList());
        }

        public Task<List<TRecord>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return Final(Builder.ToListAsync(cancellationToken));
        }

        public TRecord[] ToArray()
        {
            return Final(Builder.ToArray());
        }

        public IEnumerable<TRecord> Stream()
        {
            return Final(Builder.Stream());
        }

        public IAsyncEnumerable<TRecord> StreamAsync(CancellationToken cancellationToken = default)
        {
            return Final(Builder.StreamAsync(cancellationToken));
        }

        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Final(Builder.ToDictionary(keySelector));
        }

        public Task<IDictionary<string, TRecord>> ToDictionaryAsync(Func<TRecord, string> keySelector, CancellationToken cancellationToken = default)
        {
            return Final(Builder.ToDictionaryAsync(keySelector, cancellationToken));
        }

        public Parameters Parameters => Builder.Parameters;
        public ParameterDefaults ParameterDefaults => Builder.ParameterDefaults;
        public CommandParameterValues ParameterValues => Builder.ParameterValues;

        T Final<T>(T value)
        {
            if (finished)
            {
                throw new Exception("This query builder is finished. Calls like Where etc. return a new object, and methods should be chained together or assigned to a new variable. Change the structure of the query so that you are not calling multiple methods on the same object."); 
            }
            finished = true;
            return value;
        }

        public string DebugViewRawQuery()
        {
            return Builder.DebugViewRawQuery();
        }
    }
}