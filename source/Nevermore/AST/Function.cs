using System;
using System.Linq;

namespace Nevermore.AST
{
    public class Function
    {
        readonly ISelect select;
        readonly Parameters parameters;
        readonly ParameterDefaults defaults;
        readonly string functionName;

        public Function(ISelect @select, Parameters parameters, ParameterDefaults defaults, string functionName)
        {
            if (parameters.Any(p => p.DataType == null))
            {
                throw new ArgumentException("All parameters must have data types");
            }

            this.select = @select;
            this.parameters = parameters;
            this.defaults = defaults;
            this.functionName = functionName;
        }

        public string GenerateSql()
        {
            return $@"CREATE FUNCTION dbo.[{functionName}]
(
{string.Join("\r\n\t", parameters.Select(ParameterSql))}
)
RETURNS TABLE
AS
RETURN (
{select.GenerateSql()}
)";
        }

        string ParameterSql(Parameter p)
        {
            var defaultValue = defaults.Contains(p.ParameterName) ? $" = {defaults[p.ParameterName].GenerateSql()}": string.Empty;
            return $"@{p.ParameterName} {p.DataType.GenerateSql()}{defaultValue}";
        }
    }
}