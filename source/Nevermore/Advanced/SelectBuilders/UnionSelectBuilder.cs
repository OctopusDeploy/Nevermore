using System.Collections.Generic;
using System.Linq;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.SelectBuilders
{
    public class UnionSelectBuilder : SelectBuilderBase<ISubquerySource>
    {
        readonly ISelect innerSelect;
        string customAlias;
        readonly ITableAliasGenerator tableAliasGenerator;

        public UnionSelectBuilder(ISelect innerSelect, 
            string customAlias, 
            ITableAliasGenerator tableAliasGenerator) 
            : this(innerSelect, customAlias, tableAliasGenerator, new List<IWhereClause>(), new List<GroupByField>(), new List<OrderByField>(), new List<IOptionClause>())
        {
        }

        UnionSelectBuilder(ISelect innerSelect, string customAlias, ITableAliasGenerator tableAliasGenerator,
            List<IWhereClause> whereClauses, List<GroupByField> groupByClauses, List<OrderByField> orderByClauses, List<IOptionClause> optionClauses,
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null)
            : base(whereClauses, groupByClauses, orderByClauses, optionClauses, columnSelection, rowSelection)
        {
            this.innerSelect = innerSelect;
            this.customAlias = customAlias;
            this.tableAliasGenerator = tableAliasGenerator;
        }

        protected override ISubquerySource From => new SubquerySource(innerSelect, GetAlias());

        public override ISelect GenerateSelectWithoutDefaultOrderBy()
        {
            var hasNoConfiguration = customAlias == null && !OrderByClauses.Any() && !WhereClauses.Any() &&
                                     ColumnSelection == null && RowSelection == null;

            return hasNoConfiguration ? innerSelect : base.GenerateSelectWithoutDefaultOrderBy();
        }

        string GetAlias()
        {
            if (string.IsNullOrEmpty(customAlias))
            {
                customAlias = tableAliasGenerator.GenerateTableAlias();
            }

            return customAlias;
        }

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), GetAlias()));
        }

        protected override ISelectColumns DefaultSelect => new SelectAllSource();

        public override ISelectBuilder Clone()
        {
            return new UnionSelectBuilder(innerSelect, customAlias, tableAliasGenerator,
                new List<IWhereClause>(WhereClauses),
                new List<GroupByField>(GroupByClauses),
                new List<OrderByField>(OrderByClauses),
                new List<IOptionClause>(OptionClauses),
                ColumnSelection,
                RowSelection);
        }
    }
}