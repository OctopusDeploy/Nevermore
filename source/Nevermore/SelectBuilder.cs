using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.AST;

namespace Nevermore
{
    public class JoinSelectBuilder : SelectBuilderBase<JoinedSource>
    {
        protected override JoinedSource From { get; }

        public JoinSelectBuilder(JoinedSource from) : this(from, new List<IWhereClause>(), new List<OrderByField>())
        {
        }

        JoinSelectBuilder(JoinedSource from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null) 
            : base(whereClauses, orderByClauses, columnSelection, rowSelection)
        {
            From = from;
        }

        protected override ISelectColumns DefaultSelect => new SelectAllFrom(From.Source.Alias);

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), From.Source.Alias));
        }

        public override ISelectBuilder Clone()
        {
            return new JoinSelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
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

    public class TableSelectBuilder : SelectBuilderBase<ITableSource>
    {
        public TableSelectBuilder(ITableSource from) 
            : this(from, new List<IWhereClause>(), new List<OrderByField>())
        {
        }

        TableSelectBuilder(ITableSource from, List<IWhereClause> whereClauses,
            List<OrderByField> orderByClauses, ISelectColumns columnSelection = null, 
            IRowSelection rowSelection = null)
            : base(whereClauses, orderByClauses, columnSelection, rowSelection)
        {
            From = from;
        }

        protected override ITableSource From { get; }
        protected override ISelectColumns DefaultSelect => new SelectAllSource();

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new Column("Id"));
        }

        public override ISelectBuilder Clone()
        {
            return new TableSelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }
    }
    
    public class UnionSelectBuilder : SelectBuilderBase<ISubquerySource>
    {
        readonly ISelect innerSelect;
        string customAlias;
        readonly ITableAliasGenerator tableAliasGenerator;

        public UnionSelectBuilder(ISelect innerSelect, 
            string customAlias, 
            ITableAliasGenerator tableAliasGenerator) 
            : this(innerSelect, customAlias, tableAliasGenerator, new List<IWhereClause>(), new List<OrderByField>())
        {
        }

        UnionSelectBuilder(ISelect innerSelect, string customAlias, ITableAliasGenerator tableAliasGenerator, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null) 
            : base(whereClauses, orderByClauses, columnSelection, rowSelection)
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
            return new UnionSelectBuilder(innerSelect, customAlias, tableAliasGenerator, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }
    }

    public class SubquerySelectBuilder : SelectBuilderBase<ISubquerySource>
    {
        public SubquerySelectBuilder(ISubquerySource from) 
            : this(from, new List<IWhereClause>(), new List<OrderByField>())
        {
        }
        
        SubquerySelectBuilder(ISubquerySource from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection = null, IRowSelection rowSelection = null) 
            : base(whereClauses, orderByClauses, columnSelection, rowSelection)
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
            return new SubquerySelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection);
        }
    }

    public abstract class SelectBuilderBase<TSource> : ISelectBuilder where TSource : ISelectSource
    {
        protected abstract TSource From { get; }
        protected readonly List<OrderByField> OrderByClauses;
        protected readonly List<IWhereClause> WhereClauses;
        protected ISelectColumns ColumnSelection;
        protected IRowSelection RowSelection;

        protected SelectBuilderBase(List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection = null,
            IRowSelection rowSelection = null)
        {
            WhereClauses = whereClauses;
            OrderByClauses = orderByClauses;
            this.RowSelection = rowSelection;
            this.ColumnSelection = columnSelection;
        }

        protected abstract ISelectColumns DefaultSelect { get; }

        protected abstract IEnumerable<OrderByField> GetDefaultOrderByFields();

        public void RemoveOrderBys()
        {
            OrderByClauses.Clear();
        }

        public ISelect GenerateSelect()
        {
            return GenerateSelectInner(GetDefaultOrderBy);
        }

        public virtual ISelect GenerateSelectWithoutDefaultOrderBy()
        {
            return GenerateSelectInner(() => null);
        }

        ISelect GenerateSelectInner(Func<OrderBy> getDefaultOrderBy)
        {
            return new Select(GetRowSelection(), GetColumnSelection(), From, GetWhere() ?? new Where(), GetOrderBy(getDefaultOrderBy));
        }

        public abstract ISelectBuilder Clone();

        Where GetWhere()
        {
            return WhereClauses.Any() ? new Where(new AndClause(WhereClauses)) : null;
        }

        OrderBy GetOrderBy(Func<OrderBy> getDefaultOrderBy)
        {
            // If you are doing something like COUNT(*) then it doesn't make sense to include an Order By clause
            if (GetColumnSelection().AggregatesRows)
            {
                return null;
            }

            if (OrderByClauses.Any()) return new OrderBy(OrderByClauses);

            return getDefaultOrderBy();
        }

        OrderBy GetDefaultOrderBy()
        {
            var orderByFields = GetDefaultOrderByFields().ToList();
            return !orderByFields.Any() ? null : new OrderBy(orderByFields);
        }

        public void AddTop(int top)
        {
            RowSelection = new Top(top);
        }

        public virtual void AddOrder(string fieldName, bool @descending)
        {
            OrderByClauses.Add(new OrderByField(new Column(fieldName), @descending ? OrderByDirection.Descending : OrderByDirection.Ascending));
        }

        public virtual void AddWhere(UnaryWhereParameter whereParams)
        {
            WhereClauses.Add(new UnaryWhereClause(new WhereFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.ParameterName));
        }

        public virtual void AddWhere(BinaryWhereParameter whereParams)
        {
            WhereClauses.Add(new BinaryWhereClause(new WhereFieldReference(whereParams.FieldName), whereParams.Operand, 
                whereParams.FirstParameterName, whereParams.SecondParameterName));
        }

        public virtual void AddWhere(ArrayWhereParameter whereParams)
        {
            WhereClauses.Add(new ArrayWhereClause(new WhereFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.ParameterNames));
        }

        public virtual void AddWhere(IsNullWhereParameter isNull)
        {
            WhereClauses.Add(new IsNullClause(new WhereFieldReference(isNull.FieldName), isNull.Not));
        }

        public void AddWhere(string whereClause)
        {
            WhereClauses.Add(new CustomWhereClause(whereClause));
        }

        public virtual void AddColumn(string columnName)
        {
            AddColumnSelection(new Column(columnName));
        }

        public virtual void AddColumn(string columnName, string columnAlias)
        {
            AddColumnSelection(new AliasedColumn(new Column(columnName), columnAlias));
        }

        public void AddColumnSelection(ISelectColumns columns)
        {
            ColumnSelection = ColumnSelection == null
                ? columns
                : new AggregateSelectColumns(new List<ISelectColumns>() { ColumnSelection, columns });
        }

        public virtual void AddRowNumberColumn(string alias, IReadOnlyList<Column> partitionBys)
        {
            InnerAddRowNumberColumn(alias, partitionBys);
        }

        public void AddRowNumberColumn(string alias, IReadOnlyList<TableColumn> partitionBys)
        {
            InnerAddRowNumberColumn(alias, partitionBys);
        }

        protected void InnerAddRowNumberColumn(string alias, IReadOnlyList<IColumn> partitionBys)
        {
            var orderByClauses = OrderByClauses.Any() 
                ? OrderByClauses 
                : GetDefaultOrderByFields().ToList();

            var partitionBy = partitionBys.Any() ? new PartitionBy(partitionBys) : null;
            AddColumnSelection(new SelectRowNumber(new Over(new OrderBy(orderByClauses.ToList()), partitionBy), alias));
            OrderByClauses.Clear();
        }

        public void AddDefaultColumnSelection()
        {
            AddColumnSelection(DefaultSelect);
        }

        ISelectColumns GetColumnSelection() => ColumnSelection ?? DefaultSelect;

        IRowSelection GetRowSelection() => RowSelection ?? new AllRows();
    }
}