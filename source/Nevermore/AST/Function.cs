using System;
using System.Linq;

namespace Nevermore.AST
{
    public class Function
    {
        readonly ISelect select;
        readonly Parameters parameters;
        readonly string functionName;

        public Function(ISelect select, Parameters parameters, string functionName)
        {
            if (parameters.Any(p => p.DataType == null))
            {
                throw new ArgumentException("All parameters must have data types");
            }

            this.select = @select;
            this.parameters = parameters;
            this.functionName = functionName;
        }

        public string GenerateSql()
        {
            return $@"CREATE FUNCTION dbo.[{functionName}]
(
{string.Join("\r\n\t", parameters.Select(p => $"@{p.ParameterName} {p.DataType.GenerateSql()}"))}
)
RETURNS TABLE
AS
RETURN (
{select.GenerateSql()}
)";
        }
    }
}