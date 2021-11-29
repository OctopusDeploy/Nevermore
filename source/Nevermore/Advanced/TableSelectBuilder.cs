using System.Collections.Generic;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced
{
    public class TableSelectBuilder : SelectBuilderBase<ITableSource>
    {
        readonly IReadOnlyList<string> columnNames;

        public TableSelectBuilder(ITableSource from, IColumn idColumn, IReadOnlyList<string> columnNames) 
            : this(from, idColumn, columnNames, new List<IWhereClause>(), new List<GroupByField>(), new List<OrderByField>())
        {
        }

        TableSelectBuilder(ITableSource from, IColumn idColumn, IReadOnlyList<string> columnNames,
            List<IWhereClause> whereClauses, List<GroupByField> groupByClauses,
            List<OrderByField> orderByClauses, ISelectColumns columnSelection = null, 
            IRowSelection rowSelection = null)
            : base(whereClauses, groupByClauses, orderByClauses, columnSelection, rowSelection)
        {
            this.columnNames = columnNames;
            From = from;
            IdColumn = idColumn;

            DefaultSelect = new SelectAllJsonColumnLast(columnNames);
        }

        protected override ITableSource From { get; }
        protected override ISelectColumns DefaultSelect { get; }
        protected IColumn IdColumn { get; }

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(IdColumn);
        }

        public override ISelectBuilder Clone()
        {
            return new TableSelectBuilder(From, IdColumn, columnNames, new List<IWhereClause>(WhereClauses), new List<GroupByField>(GroupByClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }
    }
}