namespace Nevermore.AST
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
    }

    public class AllRows : IRowSelection
    {
        public string GenerateSql() => "";
    }
}