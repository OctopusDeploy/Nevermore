namespace Nevermore.AST
{
    public interface IDataType
    {
        string GenerateSql();
    }

    public class NVarCharMax : IDataType
    {
        public string GenerateSql() => "NVARCHAR(MAX)";
    }

    public class NVarChar : IDataType
    {
        readonly int length;

        public NVarChar(int length)
        {
            this.length = length;
        }

        public string GenerateSql() => $"NVARCHAR({length})";
    }

    public class BigInt : IDataType
    {
        public string GenerateSql() => "BIGINT";
    }

    public class Int : IDataType
    {
        public string GenerateSql() => "INT";
    }

    public class Bit : IDataType
    {
        public string GenerateSql() => "BIT";
    }
}