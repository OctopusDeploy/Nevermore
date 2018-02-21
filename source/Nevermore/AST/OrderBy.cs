using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.AST
{
    public class OrderBy
    {
        readonly IReadOnlyList<OrderByField> fields;

        public OrderBy(IReadOnlyList<OrderByField> fields)
        {
            if (fields.Count < 1) throw new ArgumentException("Fields must have at least one value");
            this.fields = fields;
        }

        public string GenerateSql() => $"ORDER BY {string.Join(", ", fields.Select(f => f.GenerateSql()))}";
    }

    public enum OrderByDirection
    {
        Ascending,
        Descending
    }

    // todo: make this immutable
    public class OrderByField
    {
        readonly string fieldName;
        public string TableAlias { get; set; }
        public OrderByDirection Direction { get; set; } = OrderByDirection.Ascending;

        public OrderByField(string fieldName)
        {
            this.fieldName = fieldName;
        }

        public string GenerateSql()
        {
            var direction = Direction == OrderByDirection.Descending ? " DESC" : string.Empty;
            var field = TableAlias == null ? $"[{fieldName}]" : $"{TableAlias}.[{fieldName}]";
            return $"{field}{direction}";
        }
    }
}