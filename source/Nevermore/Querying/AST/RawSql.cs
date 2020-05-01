namespace Nevermore.Querying.AST
{
    public class RawSql : ISelect 
    {
        readonly string rawSql;

        public RawSql(string rawSql)
        {
            this.rawSql = rawSql;
        }


        public string Schema => null;

        public string GenerateSql()
        {
            return rawSql;
        }
    }
}