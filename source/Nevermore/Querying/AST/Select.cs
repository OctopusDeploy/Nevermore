﻿namespace Nevermore.Querying.AST
{
    public class Select : ISelect
    {
        readonly IRowSelection rowSelection;
        readonly ISelectColumns columns;
        readonly ISelectSource from;
        readonly Where where;
        readonly OrderBy orderBy; // Can be null
        readonly GroupBy groupBy; // Can be null
        readonly IOption option;

        public Select(IRowSelection rowSelection, ISelectColumns columns, ISelectSource from, Where where, GroupBy groupBy, OrderBy orderBy, IOption option)
        {
            this.rowSelection = rowSelection;
            this.columns = columns;
            this.from = from;
            this.where = where;
            this.orderBy = orderBy;
            this.groupBy = groupBy;
            this.option = option;
        }

        public string Schema => @from.Schema;

        public string GenerateSql()
        {
            var orderByString = orderBy != null ? $@"
{orderBy.GenerateSql()}" : string.Empty;
            
            var groupByString = groupBy?.GenerateSql();
            
            return $@"SELECT {rowSelection.GenerateSql()}{columns.GenerateSql()}
FROM {from.GenerateSql()}{where.GenerateSql()}{groupByString}{orderByString}{option.GenerateSql()}";
        }

        public override string ToString()
        {
            return GenerateSql();
        }
    }
}