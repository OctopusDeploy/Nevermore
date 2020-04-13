namespace Nevermore.AST
{
    public class RawSql : ISelect 
    {
        readonly string rawSql;

        public RawSql(string rawSql)
        {
            this.rawSql = rawSql;
        }

        public string GenerateSql()
        {
            return rawSql;
        }
    }
}