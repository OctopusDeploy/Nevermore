using Nevermore.Advanced;
using System;
using System.Linq;

namespace Nevermore.Querying.AST
{
    public class Function
    {
        readonly ISelect select;
        readonly Parameters parameters;
        readonly ParameterDefaults defaults;
        readonly string functionName;
        readonly string schemaName;

        public Function(ISelect @select, Parameters parameters, ParameterDefaults defaults, string functionName, string schemaName = NevermoreDefaults.DefaultSchemaName)
        {
            if (parameters.Any(p => p.DataType == null))
            {
                throw new ArgumentException("All parameters must have data types");
            }

            this.select = @select;
            this.parameters = parameters;
            this.defaults = defaults;
            this.functionName = functionName;
            this.schemaName = schemaName;
        }

        public string GenerateSql()
        {
            return $@"CREATE FUNCTION [{schemaName}].[{functionName}]
(
{Format.IndentLines(string.Join("\r\n", parameters.Select(ParameterSql)))}
)
RETURNS TABLE
AS
RETURN (
{Format.IndentLines(select.GenerateSql())}
)";
        }

        string ParameterSql(Parameter p)
        {
            var defaultValue = defaults.Contains(p.ParameterName) ? $" = {defaults[p.ParameterName].GenerateSql()}": string.Empty;
            return $"@{p.ParameterName} {p.DataType.GenerateSql()}{defaultValue}";
        }
    }
}