using System;
using System.Linq;

namespace Nevermore.AST
{
    public class StoredProcedure
    {
        readonly ISelect select;
        readonly Parameters parameters;
        readonly string procedureName;

        public StoredProcedure(ISelect select, Parameters parameters, string procedureName)
        {
            if (parameters.Any(p => p.DataType == null))
            {
                throw new ArgumentException("All parameters must have data types");
            }

            this.select = select;
            this.parameters = parameters;
            this.procedureName = procedureName;
        }

        public string GenerateSql()
        {
            return $@"CREATE PROCEDURE dbo.[{procedureName}]
(
{string.Join("\r\n\t", parameters.Select(p => $"@{p.ParameterName} {p.DataType.GenerateSql()}"))}
)
AS
BEGIN (
{select.GenerateSql()}
)
END";
        }
    }
}