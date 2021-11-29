using System.Collections.Generic;
using System.Linq;
using Nevermore.Querying;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced
{
    public class JoinSelectBuilder : SelectBuilderBase<JoinedSource>
    {
        readonly IReadOnlyList<string> columnNames;
        protected override JoinedSource From { get; }

        public JoinSelectBuilder(JoinedSource from, IReadOnlyList<string> columnNames) : this(from, columnNames, new List<IWhereClause>(), new List<GroupByField>(), new List<OrderByField>())
        {
        }

        JoinSelectBuilder(JoinedSource from, IReadOnlyList<string> columnNames, List<IWhereClause> whereClauses, List<GroupByField> groupByClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null) 
            : base(whereClauses, groupByClauses, orderByClauses, columnSelection, rowSelection)
        {
            From = from;
            this.columnNames = columnNames;
        }

        protected override ISelectColumns DefaultSelect => new SelectAllFromJsonColumnLast(From.Source.Alias, columnNames);

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), From.Source.Alias));
        }

        public override ISelectBuilder Clone()
        {
            return new JoinSelectBuilder(From, columnNames, new List<IWhereClause>(WhereClauses), new List<GroupByField>(GroupByClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }

        public override void AddWhere(UnaryWhereParameter whereParams)
        {
            WhereClauses.Add(new UnaryWhereClause(new AliasedWhereFieldReference(From.Source.Alias, new WhereFieldReference(whereParams.FieldName)), 
                whereParams.Operand, whereParams.ParameterName));
        }

        public override void AddWhere(BinaryWhereParameter whereParams)
        {
            WhereClauses.Add(new BinaryWhereClause(new AliasedWhereFieldReference(From.Source.Alias, new WhereFieldReference(whereParams.FieldName)), 
                whereParams.Operand, whereParams.FirstParameterName, whereParams.SecondParameterName));
        }

        public override void AddWhere(ArrayWhereParameter whereParams)
        {
            WhereClauses.Add(new ArrayWhereClause(new AliasedWhereFieldReference(From.Source.Alias, new WhereFieldReference(whereParams.FieldName)), 
                whereParams.Operand, whereParams.ParameterNames));
        }

        public override void AddOrder(string fieldName, bool @descending)
        {
            OrderByClauses.Add(new OrderByField(new TableColumn(new Column(fieldName), From.Source.Alias), @descending ? OrderByDirection.Descending : OrderByDirection.Ascending));
        }

        public override void AddColumn(string columnName)
        {
            AddColumnSelection(new TableColumn(new Column(columnName), From.Source.Alias));
        }

        public override void AddColumn(string columnName, string columnAlias)
        {
            AddColumnSelection(new AliasedColumn(new TableColumn(new Column(columnName), From.Source.Alias), columnAlias));
        }

        public override void AddRowNumberColumn(string alias, IReadOnlyList<Column> partitionBys)
        {
            InnerAddRowNumberColumn(alias, partitionBys.Select(c => new TableColumn(c, From.Source.Alias)).ToList());
        }
    }
}