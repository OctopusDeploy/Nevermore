using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Querying.AST
{
    public class Union : ISelect
    {
        readonly IReadOnlyList<ISelect> selects;

        public Union(IReadOnlyList<ISelect> selects)
        {
            this.selects = selects;
        }

        public string Schema => selects.Select(s => s.Schema).FirstOrDefault(s => s != null);

        public string GenerateSql()
        {
            return string.Join("\r\nUNION\r\n", selects.Select(s => s.GenerateSql()));
        }

        public override string ToString() => GenerateSql();
    }
}