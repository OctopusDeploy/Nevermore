using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Querying.AST
{
    public interface IRowSelection
    {
        string GenerateSql();
    }

    public class CompositeRowSelection : IRowSelection
    {
        readonly IEnumerable<IRowSelection> rowSelections;

        public CompositeRowSelection(IEnumerable<IRowSelection> rowSelections)
        {
            this.rowSelections = rowSelections;
        }

        public string GenerateSql()
        {
            return string.Join(" ", rowSelections.Select(r => r.GenerateSql()));
        }
    }

    public class Top : IRowSelection
    {
        readonly int numberOfRows;

        public Top(int numberOfRows)
        {
            this.numberOfRows = numberOfRows;
        }

        public string GenerateSql() => $"TOP {numberOfRows} ";
        public override string ToString() => GenerateSql();
    }

    public class AllRows : IRowSelection
    {
        public string GenerateSql() => "";
        public override string ToString() => GenerateSql();
    }

    public class Distinct : IRowSelection
    {
        public string GenerateSql() => $"DISTINCT ";
        public override string ToString() => GenerateSql();
    }
}