using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Querying.AST
{
    public interface IOption
    {
        string GenerateSql();
    }

    public interface IOptionClause
    {
        string GenerateSql();
    }

    public class Option : IOption
    {
        readonly IReadOnlyList<IOptionClause> optionClauses;

        public Option(IReadOnlyList<IOptionClause> optionClauses)
        {
            this.optionClauses = optionClauses;
        }

        public string GenerateSql()
        {
            return optionClauses.Any()
                ? $@"
OPTION ({string.Join(@", ", optionClauses.Select(f => f.GenerateSql()))})"
                : string.Empty;
        }

        public override string ToString() => GenerateSql();
    }

    public class OptionClause : IOptionClause
    {
        readonly string queryHint;

        public OptionClause(string queryHint)
        {
            this.queryHint = queryHint;
        }

        public string GenerateSql() => queryHint;

        public override string ToString() => GenerateSql();
    }
}