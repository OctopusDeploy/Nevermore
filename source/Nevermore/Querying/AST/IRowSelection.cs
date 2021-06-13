namespace Nevermore.Querying.AST
{
    public interface IRowSelection
    {
        string GenerateSql();
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
        public Distinct()
        {
            
        }

        public string GenerateSql() => $"DISTINCT ";
        public override string ToString() => GenerateSql();
    }
}