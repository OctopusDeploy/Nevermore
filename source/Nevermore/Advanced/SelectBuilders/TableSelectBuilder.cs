using System.Collections.Generic;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.SelectBuilders
{
    public class TableSelectBuilder : SelectBuilderBase<ITableSource>
    {
        public TableSelectBuilder(ITableSource from, IColumn idColumn) 
            : this(from, idColumn, new List<IWhereClause>(), new List<GroupByField>(), new List<OrderByField>())
        {
        }

        TableSelectBuilder(ITableSource from, IColumn idColumn,
            List<IWhereClause> whereClauses, List<GroupByField> groupByClauses,
            List<OrderByField> orderByClauses, ISelectColumns columnSelection = null, 
            IRowSelection rowSelection = null)
            : base(whereClauses, groupByClauses, orderByClauses, columnSelection, rowSelection)
        {
            From = from;
            IdColumn = idColumn;

            DefaultSelect = new SelectAllJsonColumnLast(from.ColumnNames);
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
            return new TableSelectBuilder(From, IdColumn, new List<IWhereClause>(WhereClauses), new List<GroupByField>(GroupByClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }
    }
}