using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Joins;
using Nevermore.QueryGraph;

namespace Nevermore
{
    public class SubquerySourceBuilder<TRecord> : SourceQueryBuilder<TRecord>, ISubquerySourceBuilder<TRecord>
    {
        readonly ISelect select;
        string alias;

        public SubquerySourceBuilder(ISelect select, string alias, IRelationalTransaction relationalTransaction, ITableAliasGenerator tableAliasGenerator, CommandParameters parameters) : base(relationalTransaction, tableAliasGenerator, parameters)
        {
            this.select = select;
            this.alias = alias;
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            return new SubquerySelectBuilder(AsSource());
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType)
        {
            return new JoinSourceQueryBuilder<TRecord>(AsSource(), joinType,
                source, RelationalTransaction, TableAliasGenerator, new CommandParameters(Parameters));
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

    public class JoinSourceQueryBuilder<TRecord> : SourceQueryBuilder<TRecord>, IJoinSourceQueryBuilder<TRecord>
    {
        readonly IAliasedSelectSource originalSource;
        readonly List<Join> intermediateJoins = new List<Join>();
        Join lastJoin;

        public JoinSourceQueryBuilder(IAliasedSelectSource originalSource, JoinType joinType, IAliasedSelectSource nextJoin, IRelationalTransaction relationalTransaction, ITableAliasGenerator tableAliasGenerator, CommandParameters parameters) : base(relationalTransaction, tableAliasGenerator, parameters)
        {
            this.originalSource = originalSource;
            lastJoin = new Join(new List<JoinClause>(), nextJoin, joinType);
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            if (lastJoin.Clauses.Count == 0)
            {
                throw new InvalidOperationException("Must have at least one 'ON' clause per join");
            }
            var joinedSource = new JoinedSource(originalSource, intermediateJoins, lastJoin);
            return new JoinSelectBuilder(joinedSource);
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType)
        {
            if (lastJoin.Clauses.Count == 0)
            {
                throw new InvalidOperationException("Must have at least one 'ON' clause per join");
            }
            intermediateJoins.Add(lastJoin);
            lastJoin = new Join(new List<JoinClause>(), source, joinType);
            return this;
        }

        public IJoinSourceQueryBuilder<TRecord> On(string leftField, JoinOperand operand, string rightField)
        {
            var newClause = new JoinClause(leftField, operand, rightField);
            lastJoin = new Join(lastJoin.Clauses.Concat(new[] {newClause}).ToList(), lastJoin.Source, lastJoin.Type);
            return this;
        }

        public IJoinSourceQueryBuilder<TRecord> ThenInnerJoin(IAliasedSelectSource source)
        {
            return Join(source, JoinType.InnerJoin);
        }
    }

    public class TableSourceQueryBuilder<TRecord> : SourceQueryBuilder<TRecord>, ITableSourceQueryBuilder<TRecord>
    {
        string tableOrViewName;
        string alias;

        public TableSourceQueryBuilder(string tableOrViewName, IRelationalTransaction relationalTransaction, 
            ITableAliasGenerator tableAliasGenerator, 
            CommandParameters parameters) 
            : base(relationalTransaction, tableAliasGenerator, parameters)
        {
            this.tableOrViewName = tableOrViewName;
        }

        protected override ISelectBuilder CreateSelectBuilder()
        {
            return new TableSelectBuilder(CreateSimpleTableSource());
        }

        public override IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType)
        {
            return new JoinSourceQueryBuilder<TRecord>(CreateAliasedTableSource(), joinType,
                source, RelationalTransaction, TableAliasGenerator, new CommandParameters(Parameters));
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

        public IQueryBuilder<TRecord> NoLock()
        {
            return Hint("NOLOCK");
        }

        ISimpleTableSource CreateSimpleTableSource()
        {
            if (alias == null)
            {
                return new NewTableSource(tableOrViewName);
            }
            return new AliasedTableSource(new NewTableSource(tableOrViewName), alias);
        }
        
        AliasedTableSource CreateAliasedTableSource()
        {
            return new AliasedTableSource(new NewTableSource(tableOrViewName), alias ?? TableAliasGenerator.GenerateTableAlias(tableOrViewName));
        }
    }

    public abstract class SourceQueryBuilder<TRecord> : IQueryBuilder<TRecord>
    {
        protected readonly IRelationalTransaction RelationalTransaction;
        protected readonly ITableAliasGenerator TableAliasGenerator;
        protected readonly CommandParameters Parameters;

        protected SourceQueryBuilder(IRelationalTransaction relationalTransaction, ITableAliasGenerator tableAliasGenerator, CommandParameters parameters)
        {
            RelationalTransaction = relationalTransaction;
            TableAliasGenerator = tableAliasGenerator;
            Parameters = parameters;
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
            return new QueryBuilder<TRecord, ISelectBuilder>(selectBuilder, RelationalTransaction, TableAliasGenerator, new CommandParameters(Parameters));
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            return Builder.Where(whereClause);
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value)
        {
            return Builder.Where(fieldName, operand, value);
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue)
        {
            return Builder.Where(fieldName, operand, startValue, endValue);
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values)
        {
            return Builder.Where(fieldName, operand, values);
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string orderByClause)
        {
            return Builder.OrderBy(orderByClause);
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string orderByClause)
        {
            return Builder.OrderByDescending(orderByClause);
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

        public IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias)
        {
            return Builder.CalculatedColumn(expression, columnAlias);
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

        public IQueryBuilder<TRecord> Parameter(string name, object value)
        {
            return Builder.Parameter(name, value);
        }

        public IQueryBuilder<TRecord> LikeParameter(string name, object value)
        {
            return Builder.LikeParameter(name, value);
        }

        public IQueryBuilder<TRecord> LikePipedParameter(string name, object value)
        {
            return Builder.LikePipedParameter(name, value);
        }

        public abstract IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType);
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

        public void Delete()
        {
            Builder.Delete();
        }

        public IEnumerable<TRecord> Stream()
        {
            return Builder.Stream();
        }

        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Builder.ToDictionary(keySelector);
        }

        public string DebugViewRawQuery()
        {
            return Builder.DebugViewRawQuery();
        }

        public CommandParameters QueryParameters => Builder.QueryParameters;
    }
}