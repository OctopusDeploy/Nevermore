using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Querying.AST
{
    public class OrderBy
    {
        readonly IReadOnlyList<OrderByField> fields;

        public OrderBy(IReadOnlyList<OrderByField> fields)
        {
            if (fields.Count < 1) throw new ArgumentException("Fields must have at least one value");
            this.fields = fields;
        }

        public string GenerateSql()
        {
            return $"ORDER BY {string.Join(@", ", fields.Select(f => f.GenerateSql()))}";
        }

        public override string ToString() => GenerateSql();
    }

    public enum OrderByDirection
    {
        Ascending,
        Descending
    }

    public class OrderByField
    {
        readonly IColumn column;
        readonly OrderByDirection direction;

        public OrderByField(IColumn column, OrderByDirection direction = OrderByDirection.Ascending)
        {
            this.column = column;
            this.direction = direction;
        }

        public string GenerateSql()
        {
            return $"{column.GenerateSql()}{(direction == OrderByDirection.Descending ? " DESC" : string.Empty)}";
        }

        public override string ToString() => GenerateSql();
    }
}