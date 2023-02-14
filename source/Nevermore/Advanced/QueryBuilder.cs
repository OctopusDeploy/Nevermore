using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced.QueryBuilders;
using Nevermore.Advanced.SelectBuilders;
using Nevermore.Querying;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced
{
    public class QueryBuilder<TRecord, TSelectBuilder> : IOrderedQueryBuilder<TRecord> where TSelectBuilder: ISelectBuilder where TRecord : class
    {
        readonly TSelectBuilder selectBuilder;
        readonly IReadQueryExecutor readQueryExecutor;
        readonly ITableAliasGenerator tableAliasGenerator;
        readonly IUniqueParameterNameGenerator uniqueParameterNameGenerator;
        readonly CommandParameterValues paramValues;
        readonly Parameters @params;
        readonly ParameterDefaults paramDefaults;
        readonly List<string> optionClauses = new();
        TimeSpan? commandTimeout;

        public QueryBuilder(TSelectBuilder selectBuilder,
            IReadQueryExecutor readQueryExecutor,
            ITableAliasGenerator tableAliasGenerator,
            IUniqueParameterNameGenerator uniqueParameterNameGenerator,
            CommandParameterValues paramValues,
            Parameters @params,
            ParameterDefaults paramDefaults)
        {
            this.selectBuilder = selectBuilder;
            this.readQueryExecutor = readQueryExecutor;
            this.tableAliasGenerator = tableAliasGenerator;
            this.uniqueParameterNameGenerator = uniqueParameterNameGenerator;
            this.paramValues = paramValues;
            this.@params = @params;
            this.paramDefaults = paramDefaults;
        }

        public ICompleteQuery<TRecord> WithTimeout(TimeSpan timeout)
        {
            commandTimeout = timeout;
            return this;
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!String.IsNullOrWhiteSpace(whereClause))
            {
                var whereClauseNormalised = Regex.Replace(whereClause, @"@\w+", m => Nevermore.Parameter.Normalize(m.Value));
                selectBuilder.AddWhere(whereClauseNormalised);
            }
            return this;
        }

        public IQueryBuilder<TRecord> WhereNull(string fieldName)
        {
            selectBuilder.AddWhere(new IsNullWhereParameter(fieldName, false));
            return this;
        }

        public IQueryBuilder<TRecord> WhereNotNull(string fieldName)
        {
            selectBuilder.AddWhere(new IsNullWhereParameter(fieldName, true));
            return this;
        }

        public IUnaryParameterQueryBuilder<TRecord> WhereParameterized(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            var uniqueParameter = new UniqueParameter(uniqueParameterNameGenerator, parameter);
            selectBuilder.AddWhere(new UnaryWhereParameter(fieldName, operand, uniqueParameter));
            return new UnaryParameterQueryBuilder<TRecord>(Parameter(uniqueParameter), uniqueParameter);
        }

        public IBinaryParametersQueryBuilder<TRecord> WhereParameterized(string fieldName, BinarySqlOperand operand,
            Parameter startValueParameter, Parameter endValueParameter)
        {
            var uniqueStartParameter = new UniqueParameter(uniqueParameterNameGenerator, startValueParameter);
            var uniqueEndParameter = new UniqueParameter(uniqueParameterNameGenerator, endValueParameter);
            selectBuilder.AddWhere(new BinaryWhereParameter(fieldName, operand, uniqueStartParameter, uniqueEndParameter));
            return new BinaryParametersQueryBuilder<TRecord>(Parameter(uniqueStartParameter).Parameter(uniqueEndParameter), uniqueStartParameter, uniqueEndParameter);
        }

        public IArrayParametersQueryBuilder<TRecord> WhereParameterized(string fieldName, ArraySqlOperand operand,
            IEnumerable<Parameter> parameterNames)
        {
            var parameterNamesList = parameterNames.Select(p => new UniqueParameter(uniqueParameterNameGenerator, p)).ToList();
            if (!parameterNamesList.Any())
            {
                return new ArrayParametersQueryBuilder<TRecord>(AddAlwaysFalseWhere(), parameterNamesList);
            }

            selectBuilder.AddWhere(new ArrayWhereParameter(fieldName, operand, parameterNamesList));
            IQueryBuilder<TRecord> builder = this;
            return new ArrayParametersQueryBuilder<TRecord>(parameterNamesList.Aggregate(builder, (b, p) => b.Parameter(p)), parameterNamesList);
        }

        IQueryBuilder<TRecord> AddAlwaysFalseWhere()
        {
            return Where("0 = 1");
        }

        public IQueryBuilder<TRecord> AllColumns()
        {
            selectBuilder.AddDefaultColumnSelection();
            return this;
        }

        public IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias)
        {
            selectBuilder.AddColumnSelection(new AliasedColumn(new CalculatedColumn(new CustomExpression(expression)), columnAlias));
            return this;
        }

        public IQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class
        {
            return new QueryBuilder<TNewRecord, TSelectBuilder>(selectBuilder, readQueryExecutor, tableAliasGenerator, uniqueParameterNameGenerator, ParameterValues, Parameters, ParameterDefaults);
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias)
        {
            return AddRowNumberColumn(columnAlias, new string[0]);
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns)
        {
            selectBuilder.AddRowNumberColumn(columnAlias, partitionByColumns.Select(c => new Column(c)).ToList());
            return this;
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns)
        {
            selectBuilder.AddRowNumberColumn(columnAlias, partitionByColumns.Select(c => new TableColumn(new Column(c.ColumnName), c.Table)).ToList());
            return this;
        }

        public IQueryBuilder<TRecord> Parameter(Parameter parameter)
        {
            @params.Add(parameter);
            return this;
        }

        public IQueryBuilder<TRecord> ParameterDefault(Parameter parameter, object defaultValue)
        {
            paramDefaults.Add(new ParameterDefault(parameter, defaultValue));
            return this;
        }

        public IQueryBuilder<TRecord> Parameter(Parameter parameter, object value)
        {
            paramValues.Add(parameter.ParameterName, value);
            return this;
        }

        public IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults, string queryAlias = null)
        {
            var subquery = new SubquerySource(selectBuilder.GenerateSelectWithoutDefaultOrderBy(), queryAlias ?? tableAliasGenerator.GenerateTableAlias());
            return new JoinSourceQueryBuilder<TRecord>(subquery,
                joinType,
                source,
                readQueryExecutor,
                tableAliasGenerator,
                uniqueParameterNameGenerator,
                new CommandParameterValues(ParameterValues, parameterValues),
                new Parameters(Parameters, parameters),
                new ParameterDefaults(ParameterDefaults, parameterDefaults));
        }

        public ISubquerySourceBuilder<TRecord> Union(IQueryBuilder<TRecord> queryBuilder)
        {
            return new UnionSourceBuilder<TRecord>(new Union(new [] { selectBuilder.GenerateSelectWithoutDefaultOrderBy(), queryBuilder.GetSelectBuilder().GenerateSelectWithoutDefaultOrderBy() }),
                readQueryExecutor,
                tableAliasGenerator,
                uniqueParameterNameGenerator,
                new CommandParameterValues(ParameterValues, queryBuilder.ParameterValues),
                new Parameters(Parameters, queryBuilder.Parameters),
                new ParameterDefaults(ParameterDefaults, queryBuilder.ParameterDefaults));
        }

        public ISubquerySourceBuilder<TRecord> Subquery()
        {
            return new SubquerySourceBuilder<TRecord>(selectBuilder.GenerateSelectWithoutDefaultOrderBy(),
                readQueryExecutor,
                tableAliasGenerator,
                uniqueParameterNameGenerator,
                ParameterValues,
                Parameters,
                ParameterDefaults);
        }

        SubquerySelectBuilder CreateSubqueryBuilder(ISelectBuilder subquerySelectBuilder)
        {
            return new SubquerySelectBuilder(new SubquerySource(subquerySelectBuilder.GenerateSelectWithoutDefaultOrderBy(), tableAliasGenerator.GenerateTableAlias()));
        }

        public ISelectBuilder GetSelectBuilder()
        {
            return selectBuilder.Clone();
        }

        public IQueryBuilder<TRecord> GroupBy(string fieldName)
        {
            selectBuilder.AddGroupBy(fieldName);
            return this;
        }

        public IQueryBuilder<TRecord> GroupBy(string fieldName, string tableAlias)
        {
            selectBuilder.AddGroupBy(fieldName, tableAlias);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName)
        {
            selectBuilder.AddOrder(fieldName, false);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName, string tableAlias)
        {
            selectBuilder.AddOrder(fieldName, tableAlias, false);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName)
        {
            selectBuilder.AddOrder(fieldName, true);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName, string tableAlias)
        {
            selectBuilder.AddOrder(fieldName, tableAlias, true);
            return this;
        }

        public IQueryBuilder<TRecord> Option(string queryHint)
        {
            optionClauses.Add(queryHint);
            return this;
        }

        public IQueryBuilder<TRecord> Column(string name)
        {
            selectBuilder.AddColumn(name);
            return this;
        }

        public IQueryBuilder<TRecord> Column(string name, string columnAlias)
        {
            selectBuilder.AddColumn(name, columnAlias);
            return this;
        }

        public IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias)
        {
            selectBuilder.AddColumnSelection(new AliasedColumn(new TableColumn(new Column(name), tableAlias), columnAlias));
            return this;
        }

        public int Count()
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddColumnSelection(new SelectCountSource());
            clonedSelectBuilder.AddOptions(optionClauses);
            var count = readQueryExecutor.ExecuteScalar<int>(clonedSelectBuilder.GenerateSelect().GenerateSql(), paramValues, RetriableOperation.Select, commandTimeout);
            return count;
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddColumnSelection(new SelectCountSource());
            clonedSelectBuilder.AddOptions(optionClauses);
            var count = await readQueryExecutor.ExecuteScalarAsync<int>(clonedSelectBuilder.GenerateSelect().GenerateSql(), paramValues, RetriableOperation.Select, commandTimeout, cancellationToken).ConfigureAwait(false);
            return count;
        }

        public bool Any()
        {
            const int trueValue = 1;
            const int falseValue = 0;
            var trueParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("true"));
            var falseParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("false"));

            var result = readQueryExecutor.ExecuteScalar<int>(CreateQuery().GenerateSql(), CreateParameterValues(), RetriableOperation.Select, commandTimeout);

            return result != falseValue;

            CommandParameterValues CreateParameterValues()
            {
                return new CommandParameterValues(paramValues)
                {
                    {trueParameter.ParameterName, trueValue},
                    {falseParameter.ParameterName, falseValue}
                };
            }

            IExpression CreateQuery()
            {
                var clonedSelectBuilder = selectBuilder.Clone();
                clonedSelectBuilder.RemoveOrderBys();
                return new IfExpression(new ExistsExpression(clonedSelectBuilder.GenerateSelectWithoutDefaultOrderBy()),
                    new SelectConstant(trueParameter),
                    new SelectConstant(falseParameter));
            }
        }

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            const int trueValue = 1;
            const int falseValue = 0;
            var trueParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("true"));
            var falseParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("false"));

            var result = await readQueryExecutor.ExecuteScalarAsync<int>(CreateQuery().GenerateSql(), CreateParameterValues(), RetriableOperation.Select, commandTimeout, cancellationToken).ConfigureAwait(false);

            return result != falseValue;

            CommandParameterValues CreateParameterValues()
            {
                return new CommandParameterValues(paramValues)
                {
                    {trueParameter.ParameterName, trueValue},
                    {falseParameter.ParameterName, falseValue}
                };
            }

            IExpression CreateQuery()
            {
                var clonedSelectBuilder = selectBuilder.Clone();
                clonedSelectBuilder.RemoveOrderBys();
                return new IfExpression(new ExistsExpression(clonedSelectBuilder.GenerateSelectWithoutDefaultOrderBy()),
                    new SelectConstant(trueParameter),
                    new SelectConstant(falseParameter));
            }
        }

        public TRecord First()
        {
            return FirstOrDefault();
        }

        public TRecord FirstOrDefault()
        {
            return Take(1).FirstOrDefault();
        }

        public async Task<TRecord> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            await foreach (var item in TakeAsync(1, cancellationToken).ConfigureAwait(false))
            {
                return item;
            }
            return default;
        }

        public IEnumerable<TRecord> Distinct()
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddDistinct();
            clonedSelectBuilder.AddOptions(optionClauses);
            var stream = readQueryExecutor.Stream<TRecord>(clonedSelectBuilder.GenerateSelectWithoutDefaultOrderBy().GenerateSql(), paramValues, commandTimeout);
            return stream;
        }

        public IAsyncEnumerable<TRecord> DistinctAsync(CancellationToken cancellationToken = default)
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddDistinct();
            clonedSelectBuilder.AddOptions(optionClauses);
            var stream = readQueryExecutor.StreamAsync<TRecord>(clonedSelectBuilder.GenerateSelectWithoutDefaultOrderBy().GenerateSql(), paramValues, commandTimeout, cancellationToken);
            return stream;
        }

        public IEnumerable<TRecord> Take(int take)
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddTop(take);
            clonedSelectBuilder.AddOptions(optionClauses);
            var stream = readQueryExecutor.Stream<TRecord>(clonedSelectBuilder.GenerateSelect().GenerateSql(), paramValues, commandTimeout);
            return stream;
        }

        public IAsyncEnumerable<TRecord> TakeAsync(int take, CancellationToken cancellationToken = default)
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddTop(take);
            clonedSelectBuilder.AddOptions(optionClauses);
            var stream = readQueryExecutor.StreamAsync<TRecord>(clonedSelectBuilder.GenerateSelect().GenerateSql(), paramValues, commandTimeout, cancellationToken);
            return stream;
        }

        public List<TRecord> ToList(int skip, int take)
        {
            var subqueryBuilder = BuildToList(skip, take, out var parmeterValues);

            var result = readQueryExecutor.Stream<TRecord>(subqueryBuilder.GenerateSelect().GenerateSql(), parmeterValues, commandTimeout).ToList();
            return result;
        }

        public async Task<List<TRecord>> ToListAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            var subqueryBuilder = BuildToList(skip, take, out var parmeterValues);

            var results = new List<TRecord>();
            var enumerator = readQueryExecutor.StreamAsync<TRecord>(subqueryBuilder.GenerateSelect().GenerateSql(), parmeterValues, commandTimeout, cancellationToken);
            await foreach (var item in enumerator.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                results.Add(item);
            }

            return results;
        }

        CteSelectSource BuildToListCount(int skip, int take, out CommandParameterValues parameterValues, out string countColumnName)
        {
            const string rowNumberColumnName = "RowNum";
            const string totalCountColumnName = "CrossJoinCount";
            countColumnName = totalCountColumnName;
            var minRowParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("_minrow"));
            var maxRowParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("_maxrow"));
            parameterValues = new CommandParameterValues(paramValues)
            {
                {minRowParameter.ParameterName, skip + 1},
                {maxRowParameter.ParameterName, take + skip}
            };

            var cteTableReference = new SchemalessTableSource(tableAliasGenerator.GenerateTableAlias());
            var originalSelectBuilder = selectBuilder.Clone();
            var originalOrderBys = originalSelectBuilder.RemoveOrderBys();

            var cteTableBuilder = CreateCteQuery(cteTableReference, originalOrderBys);
            var primaryQuery = CreateSubQueryFromCte(cteTableBuilder);
            var innerCountSubQuery = CreateCountQueryFromCte(cteTableReference);
            var crossJoinPrimaryWithCount = CrossJoinDataWithTotalCount(primaryQuery, innerCountSubQuery);
            return new CteSelectSource(originalSelectBuilder.GenerateSelectWithoutDefaultOrderBy(), cteTableReference.TableName, crossJoinPrimaryWithCount.GenerateSelect());

            TableSelectBuilder CreateCteQuery(SchemalessTableSource cteTableSource, OrderByField[] orderBys)
            {
                var nullColumn = new CalculatedColumn(new RawSql("(SELECT NULL)")); //This Column Will be ignored unless there is no ordering since we just end up returning the Count(*)
                var tableSelectBuilder = new TableSelectBuilder(cteTableSource, nullColumn);
                foreach (var order in orderBys)
                {
                    // Strip off any table alias information, the CTW will expect the columns to already be unique
                    var orderByField = order.Column is TableColumn tableColumn ? new OrderByField(tableColumn.Column, order.Direction) : order;
                    tableSelectBuilder.AddOrder(orderByField);
                }
                tableSelectBuilder.AddColumnSelection(new SelectAllSource());
                tableSelectBuilder.AddRowNumberColumn(rowNumberColumnName);
                return tableSelectBuilder;
            }

            SubquerySource CreateSubQueryFromCte(TableSelectBuilder cte)
            {
                var subQueryBuilder = CreateSubqueryBuilder(cte);
                subQueryBuilder.AddDefaultColumnSelection();
                subQueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.GreaterThanOrEqual, minRowParameter));
                subQueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.LessThanOrEqual, maxRowParameter));
                subQueryBuilder.AddOptions(optionClauses);
                var subQuery = subQueryBuilder.GenerateSelectWithoutDefaultOrderBy();
                return new SubquerySource(subQuery, tableAliasGenerator.GenerateTableAlias());
            }

            ISelectBuilder CrossJoinDataWithTotalCount(SubquerySource originalQuery, SubquerySource countQuery)
            {
                var combinedQueryWithCount = new JoinSourceQueryBuilder<TRecord>(originalQuery,
                        JoinType.CrossJoin,
                        countQuery,
                        readQueryExecutor,
                        tableAliasGenerator,
                        uniqueParameterNameGenerator,
                        new CommandParameterValues(ParameterValues),
                        new Parameters(Parameters),
                        new ParameterDefaults(ParameterDefaults))
                    .GetSelectBuilder();
                combinedQueryWithCount.AddDefaultColumnSelection();
                combinedQueryWithCount.AddColumnSelection(new TableColumn(new Column(totalCountColumnName), innerCountSubQuery.Alias));
                combinedQueryWithCount.AddOrder(rowNumberColumnName, false);
                return combinedQueryWithCount;
            }

            SubquerySource CreateCountQueryFromCte(SchemalessTableSource cteTableSource)
            {
                var innerCountTableBuilder = new TableSelectBuilder(cteTableSource, new Column(totalCountColumnName));
                innerCountTableBuilder.AddColumnSelection(new AliasedColumn(new SelectCountSource(), totalCountColumnName));
                return new SubquerySource(innerCountTableBuilder.GenerateSelectWithoutDefaultOrderBy(), tableAliasGenerator.GenerateTableAlias());
            }
        }

        SubquerySelectBuilder BuildToList(int skip, int take, out CommandParameterValues parameterValues)
        {
            const string rowNumberColumnName = "RowNum";
            var minRowParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("_minrow"));
            var maxRowParameter = new UniqueParameter(uniqueParameterNameGenerator, new Parameter("_maxrow"));

            var clonedSelectBuilder = selectBuilder.Clone();

            if(!clonedSelectBuilder.HasCustomColumnSelection)
                clonedSelectBuilder.AddDefaultColumnSelection();

            clonedSelectBuilder.AddRowNumberColumn(rowNumberColumnName, new List<Column>());

            var subqueryBuilder = CreateSubqueryBuilder(clonedSelectBuilder);
            subqueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.GreaterThanOrEqual,
                minRowParameter));
            subqueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.LessThanOrEqual,
                maxRowParameter));
            subqueryBuilder.AddOrder(rowNumberColumnName, false);
            subqueryBuilder.AddOptions(optionClauses);

            parameterValues = new CommandParameterValues(paramValues)
            {
                {minRowParameter.ParameterName, skip + 1},
                {maxRowParameter.ParameterName, take + skip}
            };
            return subqueryBuilder;
        }


        [Pure]
        public List<TRecord> ToList(int skip, int take, out int totalResults)
        {
            totalResults = Count();
            return ToList(skip, take);
        }

        /// <summary>
        /// Query using legacy 2-step operation.
        /// TODO: Remove this once we can deprecate the legacy approach
        /// </summary>
        async Task<(List<TRecord>, int)> ToListWithCountAsyncLegacy(int skip, int take, CancellationToken cancellationToken = default)
        {
            var count = await CountAsync(cancellationToken).ConfigureAwait(false);
            var list = await ToListAsync(skip, take, cancellationToken).ConfigureAwait(false);
            return (list, count);
        }

        async Task<(List<TRecord>, int)> ToListWithCountAsyncCte(int skip, int take, CancellationToken cancellationToken = default)
        async Task<(List<TRecord>, int)> ToListWithCountAsyncCte(int skip, int take, CancellationToken cancellationToken = default)
        {
            // Short circuit query if no results will be retrieved
            if (take == 0)
            {
                return await ReturnJustCount().ConfigureAwait(false);
            }

            var selectSource = BuildToListCount(skip, take, out var parmeterValues, out var countColumnName);
            int total = 0;
            var stream = readQueryExecutor.StreamAsync(selectSource.GenerateSql(), parmeterValues, map =>
            {
                if (total == 0)
                {
                    total = map.Read(reader => int.Parse(reader[countColumnName].ToString()));
                }
                return map.Map<TRecord>(string.Empty);
            }, commandTimeout, cancellationToken);

            var results = new List<TRecord>();
            await foreach (var item in stream.ConfigureAwait(false))
                results.Add(item);

            // If no result came back its possible that the page is greater than whats available
            // Fall back to using just the count
            if (!results.Any())
            {
                return await ReturnJustCount().ConfigureAwait(false);
            }

            async Task<(List<TRecord>, int)> ReturnJustCount()
            {
                var count = await CountAsync(cancellationToken).ConfigureAwait(false);
                return (new List<TRecord>(), count);
            }

            return (results, total);
        }

        [Pure]
        public async Task<(List<TRecord>, int)> ToListWithCountAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return FeatureFlags.UseCteBasedListWithCount
                ? await ToListWithCountAsyncCte(skip, take, cancellationToken).ConfigureAwait(false)
                : await ToListWithCountAsyncLegacy(skip, take, cancellationToken).ConfigureAwait(false);
        }

        [Pure]
        public List<TRecord> ToList()
        {
            return Stream().ToList();
        }

        public async Task<List<TRecord>> ToListAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TRecord>();

            await foreach (var item in StreamAsync(cancellationToken).ConfigureAwait(false))
                results.Add(item);

            return results;
        }

        [Pure]
        public TRecord[] ToArray()
        {
            return Stream().ToArray();
        }

        [Pure]
        public IEnumerable<TRecord> Stream()
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddOptions(optionClauses);
            var stream = readQueryExecutor.Stream<TRecord>(clonedSelectBuilder.GenerateSelect().GenerateSql(), paramValues, commandTimeout);
            return stream;
        }

        public IAsyncEnumerable<TRecord> StreamAsync(CancellationToken cancellationToken = default)
        {
            var clonedSelectBuilder = selectBuilder.Clone();
            clonedSelectBuilder.AddOptions(optionClauses);
            var stream = readQueryExecutor.StreamAsync<TRecord>(clonedSelectBuilder.GenerateSelect().GenerateSql(), paramValues, commandTimeout, cancellationToken);
            return stream;
        }

        [Pure]
        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Stream().ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IDictionary<string, TRecord>> ToDictionaryAsync(Func<TRecord, string> keySelector, CancellationToken cancellationToken = default)
        {
            return (await ToListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        public Parameters Parameters => new Parameters(@params);
        public ParameterDefaults ParameterDefaults => new ParameterDefaults(paramDefaults);
        public CommandParameterValues ParameterValues => new CommandParameterValues(paramValues);

        [Pure]
        public string DebugViewRawQuery()
        {
            return selectBuilder.GenerateSelect().GenerateSql();
        }
    }
}
