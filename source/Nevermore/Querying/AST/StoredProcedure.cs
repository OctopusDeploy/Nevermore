using System;
using System.Linq;

namespace Nevermore.AST
{
    public class StoredProcedure
    {
        readonly ISelect select;
        readonly Parameters parameters;
        readonly ParameterDefaults defaults;
        readonly string procedureName;

        public StoredProcedure(ISelect select, Parameters parameters, ParameterDefaults defaults, string procedureName)
        {
            if (parameters.Any(p => p.DataType == null))
            {
                throw new ArgumentException("All parameters must have data types");
            }

            this.select = select;
            this.parameters = parameters;
            this.defaults = defaults;
            this.procedureName = procedureName;
        }

        public string GenerateSql()
        {
            return $@"CREATE PROCEDURE dbo.[{procedureName}]
(
{Format.IndentLines(string.Join("\r\n", parameters.Select(ParameterSql)))}
)
AS
BEGIN (
{Format.IndentLines(select.GenerateSql())}
)
END";
        }

        string ParameterSql(Parameter p)
        {
            var defaultValue = defaults.Contains(p.ParameterName) ? $" = {defaults[p.ParameterName].GenerateSql()}": string.Empty;
            return $"@{p.ParameterName} {p.DataType.GenerateSql()}{defaultValue}";
        }
    }
}