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
}