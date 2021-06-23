using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Querying.AST
{
    public class GroupBy
    {
        readonly IReadOnlyList<GroupByField> fields;

        public GroupBy(IReadOnlyList<GroupByField> fields)
        {
            if (fields.Count < 1) throw new ArgumentException("Fields must have at least one value");
            this.fields = fields;            
        }
        
        public string GenerateSql()
        {
            return @$"
GROUP BY {string.Join(@", ", fields.Select(f => f.GenerateSql()))}";
        }

        public override string ToString() => GenerateSql();
    }

    public class GroupByField
    {
        readonly IColumn column;

        public GroupByField(IColumn column)
        {
            this.column = column;
        }

        public string GenerateSql() => column.GenerateSql();

        public override string ToString() => GenerateSql();
    }
}