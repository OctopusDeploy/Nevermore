using System.Collections.Generic;
using System.Linq;

namespace Nevermore.AST
{
    public class PartitionBy
    {
        readonly IReadOnlyList<IColumn> columns;

        public PartitionBy(IReadOnlyList<IColumn> columns)
        {
            this.columns = columns;
        }

        public string GenerateSql()
        {
            return $"PARTITION BY {string.Join(", ", columns.Select(c => c.GenerateSql()))}";
        }
    }
}