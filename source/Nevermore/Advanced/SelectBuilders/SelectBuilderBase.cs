using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Querying;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.SelectBuilders
{
    public abstract class SelectBuilderBase<TSource> : ISelectBuilder where TSource : ISelectSource
    {
        protected abstract TSource From { get; }
        protected readonly List<OrderByField> OrderByClauses;
        protected readonly List<GroupByField> GroupByClauses;
        protected readonly List<IWhereClause> WhereClauses;
        protected readonly List<IOptionClause> OptionClauses;
        protected ISelectColumns ColumnSelection;
        protected IRowSelection RowSelection;

        protected SelectBuilderBase(
            List<IWhereClause> whereClauses,
            List<GroupByField> groupByClauses,
            List<OrderByField> orderByClauses,
            List<IOptionClause> optionClauses,
            ISelectColumns columnSelection = null,
            IRowSelection rowSelection = null)
        {
            WhereClauses = whereClauses;
            OrderByClauses = orderByClauses;
            GroupByClauses = groupByClauses;
            OptionClauses = optionClauses;
            RowSelection = rowSelection;
            ColumnSelection = columnSelection;
        }

        public ISelectSource SelectSource { get; }

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
            return new Select(GetRowSelection(), GetColumnSelection(), From, GetWhere() ?? new Where(), GetGroupBy(), GetOrderBy(getDefaultOrderBy), GetOption());
        }

        public abstract ISelectBuilder Clone();
        
        public bool HasCustomColumnSelection => ColumnSelection != null;

        Where GetWhere()
        {
            return WhereClauses.Any() ? new Where(new AndClause(WhereClauses)) : null;
        }

        GroupBy GetGroupBy()
        {
            return GroupByClauses.Any() ? new GroupBy(GroupByClauses) : null;
        }

        OrderBy GetOrderBy(Func<OrderBy> getDefaultOrderBy)
        {
            // If you are doing something like COUNT(*) then it doesn't make sense to include an Order By clause
            if (GetColumnSelection().AggregatesRows || GroupByClauses.Any())
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

        IOption GetOption()
        {
            return new Option(OptionClauses);
        }

        public void AddTop(int top)
        {
            RowSelection = new Top(top);
        }

        public void AddDistinct()
        {
            RowSelection = new Distinct();
        }

        public void AddGroupBy(string fieldName)
        {
            GroupByClauses.Add(new GroupByField(new Column(fieldName)));
        }
        
        public void AddGroupBy(string fieldName, string tableAlias)
        {
            GroupByClauses.Add(new GroupByField(new TableColumn(new Column(fieldName), tableAlias)));
        }
        
        public virtual void AddOrder(string fieldName, bool @descending)
        {
            OrderByClauses.Add(new OrderByField(new Column(fieldName), @descending ? OrderByDirection.Descending : OrderByDirection.Ascending));
        }
        
        public void AddOrder(string fieldName, string tableAlias, bool @descending)
        {
            OrderByClauses.Add(new OrderByField(new TableColumn(new Column(fieldName), tableAlias), @descending ? OrderByDirection.Descending : OrderByDirection.Ascending));
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

        public void AddOption(string queryHint)
        {
            OptionClauses.Add(new OptionClause(queryHint));
        }

        public void AddOptions(IReadOnlyList<string> queryHints)
        {
            foreach (var queryHint in queryHints)
            {
                AddOption(queryHint);
            }
        }

        ISelectColumns GetColumnSelection() => ColumnSelection ?? DefaultSelect;

        IRowSelection GetRowSelection() => RowSelection ?? new AllRows();
    }
}