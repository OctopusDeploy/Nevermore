using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nevermore.Mapping;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.Queryable
{
    internal class SqlExpressionBuilder
    {
        readonly IRelationalStoreConfiguration configuration;

        readonly List<ISelectColumns> selectColumns = new();
        readonly List<OrderByField> orderByFields = new();
        readonly List<IWhereClause> whereClauses = new();
        string hint;
        readonly CommandParameterValues parameterValues = new();
        ITableSource from;
        int? skip;
        int? take;
        QueryType queryType = QueryType.SelectMany;
        volatile int paramCounter;

        public SqlExpressionBuilder(IRelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public DocumentMap DocumentMap { get; private set; }

        public void Column(ISelectColumns selectColumns)
        {
            this.selectColumns.Add(selectColumns);
        }

        public void Where(IWhereClause whereClause)
        {
            whereClauses.Add(whereClause);
        }

        public IWhereClause CreateWhere(IWhereFieldReference fieldReference, UnarySqlOperand operand, object value)
        {
            var param = AddParameter(value);
            return new UnaryWhereClause(fieldReference, operand, param.ParameterName);
        }

        public IWhereClause CreateWhere(IWhereFieldReference fieldReference, ArraySqlOperand operand, IEnumerable values)
        {
            var parameters = (from object x in values select AddParameter(x)).ToList();

            // if we have no parameters, then we generate a custom where clause with a fixed SQL
            // this sql changes based on which array operation you are doing
            if (parameters.Count == 0)
            {
                return operand switch
                {
                    ArraySqlOperand.In => new CustomWhereClause("1 = 0"),
                    ArraySqlOperand.NotIn => new CustomWhereClause("1 = 1"),
                    _ => throw new ArgumentOutOfRangeException(nameof(operand), operand, null)
                };
            }

            return new ArrayWhereClause(fieldReference, operand, parameters.Select(p => p.ParameterName));
        }

        public IWhereClause CreateWhere(object value, ArraySqlOperand operand, string jsonPath, Type elementType)
        {
            var parameter = AddParameter(value);
            return new JsonArrayWhereClause(parameter.ParameterName, operand, jsonPath, elementType);
        }

        public void OrderBy(OrderByField field)
        {
            orderByFields.Add(field);
        }

        public void Single()
        {
            Take(1);
            queryType = QueryType.SelectSingle;
        }

        public void Exists()
        {
            queryType = QueryType.Exists;
        }

        public void Count()
        {
            queryType = QueryType.Count;
        }

        public void Skip(int numberOfRows)
        {
            skip = numberOfRows;
        }

        public void Take(int numberOfRows)
        {
            take = numberOfRows;
        }

        public void Hint(string hint)
        {
            this.hint = hint;
        }

        public void From(Type documentType)
        {
            DocumentMap = configuration.DocumentMaps.Resolve(documentType);
            var schema = DocumentMap.SchemaName ?? configuration.DefaultSchema;
            from = new SimpleTableSource(DocumentMap.TableName, schema, GetDocumentColumns().ToArray());

            if (DocumentMap.TypeResolutionColumn is not null)
            {
                var discriminator = configuration.InstanceTypeResolvers.ResolveValueFromType(documentType);
                if (discriminator is not null)
                {
                    var discriminatorClause = CreateWhere(new WhereFieldReference(DocumentMap.TypeResolutionColumn.ColumnName), UnarySqlOperand.Equal, discriminator);
                    Where(discriminatorClause);
                }
            }
        }

        public (IExpression, CommandParameterValues, QueryType) Build()
        {
            if (from is ISimpleTableSource simpleTableSource && !string.IsNullOrEmpty(hint))
            {
                from = new TableSourceWithHint(simpleTableSource, hint);
            }

            var sqlExpression = queryType switch
            {
                QueryType.Exists => CreateExistsQuery(),
                QueryType.Count => CreateCountQuery(),
                _ => CreateSelectQuery()
            };

            return (sqlExpression, parameterValues, queryType);
        }


        IExpression CreateSelectQuery()
        {
            IRowSelection rowSelection = take.HasValue && !skip.HasValue ? new Top(take.Value) : null;
            var orderBy = orderByFields.Any() ? new OrderBy(orderByFields) : GetDefaultOrderBy();
            ISelectColumns columns = selectColumns.Any() ? new AggregateSelectColumns(selectColumns) : new SelectAllJsonColumnLast(GetDocumentColumns().ToList());
            var select = new Select(
                rowSelection ?? new AllRows(),
                skip.HasValue ? new AggregateSelectColumns(new [] { new SelectRowNumber(new Over(orderBy, null), "RowNum"), columns }) : columns,
                from,
                CreateWhereClause(),
                null,
                orderByFields.Any() && !skip.HasValue ? orderBy : null,
                new Option(Array.Empty<OptionClause>()));

            if (skip.HasValue)
            {
                var pagingFilters = new List<IWhereClause>();
                var skipParam = AddParameter(skip.Value);
                pagingFilters.Add(new UnaryWhereClause(new WhereFieldReference("RowNum"), UnarySqlOperand.GreaterThan, skipParam.ParameterName));

                if (take.HasValue)
                {
                    var takeParam = AddParameter(take.Value + (skip ?? 0));
                    pagingFilters.Add(new UnaryWhereClause(new WhereFieldReference("RowNum"), UnarySqlOperand.LessThanOrEqual, takeParam.ParameterName));
                }

                select = new Select(
                    new AllRows(),
                    new SelectAllColumnsWithTableAliasJsonLast("aliased", GetDocumentColumns().ToList()),
                    new SubquerySource(select, "aliased"),
                    new Where(new AndClause(pagingFilters)),
                    null,
                    new OrderBy(new[] { new OrderByField(new Column("RowNum")) }),
                    new Option(Array.Empty<OptionClause>()));
            }

            return select;
        }

        IExpression CreateExistsQuery()
        {
            IRowSelection rowSelection = take.HasValue && !skip.HasValue ? new Top(take.Value) : null;


            var select = new Select(
                rowSelection ?? new AllRows(),
                new SelectAllSource(),
                from,
                CreateWhereClause(),
                null,
                null,
                new Option(Array.Empty<OptionClause>()));
            var trueParameter = AddParameter(true);
            var falseParameter = AddParameter(false);

            return new IfExpression(new ExistsExpression(select), new SelectConstant(trueParameter), new SelectConstant(falseParameter));
        }

        IExpression CreateCountQuery()
        {
            return new Select(
                new AllRows(),
                new SelectCountSource(),
                from,
                CreateWhereClause(),
                null,
                null,
                new Option(Array.Empty<OptionClause>()));
        }

        Where CreateWhereClause()
        {
            return whereClauses.Any() ? new Where(new AndClause(whereClauses)) : new Where();
        }

        Parameter AddParameter(object value)
        {
            var index = Interlocked.Increment(ref paramCounter);
            var paramName = $"p{index}";
            parameterValues[paramName] = value;
            return new Parameter(paramName);
        }

        IEnumerable<string> GetDocumentColumns()
        {
            yield return DocumentMap.IdColumn!.ColumnName;

            foreach (var column in DocumentMap.Columns)
            {
                yield return column.ColumnName;
            }

            if (DocumentMap.HasJsonColumn())
            {
                yield return "JSON";
            }

            if (DocumentMap.HasJsonBlobColumn())
            {
                yield return "JSONBlob";
            }
        }

        OrderBy GetDefaultOrderBy()
        {
            return new OrderBy(new[] { new OrderByField(new Column(DocumentMap.IdColumn!.ColumnName)) });
        }
    }
}