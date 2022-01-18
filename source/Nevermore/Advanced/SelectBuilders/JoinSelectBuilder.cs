using System.Collections.Generic;
using System.Linq;
using Nevermore.Querying;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.SelectBuilders
{
    public class JoinSelectBuilder : SelectBuilderBase<JoinedSource>
    {
        protected override JoinedSource From { get; }

        public JoinSelectBuilder(JoinedSource from) : this(from, new List<IWhereClause>(), new List<GroupByField>(), new List<OrderByField>(), new List<IOptionClause>())
        {
        }

        JoinSelectBuilder(JoinedSource from,
            List<IWhereClause> whereClauses, List<GroupByField> groupByClauses, List<OrderByField> orderByClauses, List<IOptionClause> optionClauses,
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null) 
            : base(whereClauses, groupByClauses, orderByClauses, optionClauses, columnSelection, rowSelection)
        {
            From = from;
        }

        protected override ISelectColumns DefaultSelect => From.Source is ISimpleTableSource fromTable 
            ? new SelectAllColumnsWithTableAliasJsonLast(From.Source.Alias, fromTable.ColumnNames)
            : new SelectAllFrom(From.Source.Alias);

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), From.Source.Alias));
        }

        public override ISelectBuilder Clone()
        {
            return new JoinSelectBuilder(From,
                new List<IWhereClause>(WhereClauses),
                new List<GroupByField>(GroupByClauses),
                new List<OrderByField>(OrderByClauses),
                new List<IOptionClause>(OptionClauses),
                ColumnSelection,
                RowSelection);
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