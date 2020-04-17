namespace Nevermore.IntegrationTests.SetUp
{
    public abstract class FixtureWithDatabase
    {
        protected IntegrationTestDatabase integrationTestDatabase;

        protected FixtureWithDatabase()
        {
            integrationTestDatabase = new IntegrationTestDatabase();
        }

        protected string ConnectionString => integrationTestDatabase.ConnectionString;

        protected void ExecuteSql(string sql)
        {
            integrationTestDatabase.ExecuteScript(sql);
        }
    }
}