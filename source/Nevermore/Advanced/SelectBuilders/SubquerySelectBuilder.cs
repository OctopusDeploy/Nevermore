using System.Collections.Generic;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.SelectBuilders
{
    public class SubquerySelectBuilder : SelectBuilderBase<ISubquerySource>
    {
        public SubquerySelectBuilder(ISubquerySource from) 
            : this(from, new List<IWhereClause>(), new List<GroupByField>(), new List<OrderByField>())
        {
        }
        
        SubquerySelectBuilder(ISubquerySource from, List<IWhereClause> whereClauses,  List<GroupByField> groupByClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null) 
            : base(whereClauses, groupByClauses, orderByClauses, columnSelection, rowSelection)
        {
            From = @from;
        }

        protected override ISubquerySource From { get; }
        protected override ISelectColumns DefaultSelect => new SelectAllSource();

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), From.Alias));
        }

        public override ISelectBuilder Clone()
        {
            return new SubquerySelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<GroupByField>(GroupByClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }
    }
}