using System.Collections.Generic;
using System.Linq;
using Nevermore.Querying;
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

        public override void AddWhere(UnaryWhereParameter whereParams)
        {
            if (From.ColumnNames.Contains(whereParams.FieldName))
            {
                base.AddWhere(whereParams);
            }
            else
            {
                WhereClauses.Add(new UnaryWhereClause(new JsonValueFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.ParameterName));
            }
        }

        public override void AddWhere(BinaryWhereParameter whereParams)
         {
             if (From.ColumnNames.Contains(whereParams.FieldName))
             {
                 base.AddWhere(whereParams);
             }
             else
             {
                 WhereClauses.Add(new BinaryWhereClause(new JsonValueFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.FirstParameterName, whereParams.SecondParameterName));
             }
         }
 
         public override void AddWhere(ArrayWhereParameter whereParams)
         {
             if (From.ColumnNames.Contains(whereParams.FieldName))
             {
                 base.AddWhere(whereParams);
             }
             else
             {
                 WhereClauses.Add(new ArrayWhereClause(new JsonValueFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.ParameterNames));
             }
         }
 
         public override void AddWhere(IsNullWhereParameter whereParams)
         {
             if (From.ColumnNames.Contains(whereParams.FieldName))
             {
                 base.AddWhere(whereParams);
             }
             else
             {
                 WhereClauses.Add(new IsNullClause(new JsonValueFieldReference(whereParams.FieldName), whereParams.Not));
             }
         }
    }
}