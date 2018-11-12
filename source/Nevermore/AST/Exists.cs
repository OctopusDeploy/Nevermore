namespace Nevermore.AST
{
    public class Exists : ISelect
    {
        readonly ISelectBuilder selectBuilder;

        public Exists(ISelectBuilder selectBuilder)
        {
            this.selectBuilder = selectBuilder;
        }

        public string GenerateSql()
        {
            return $@"IF EXISTS({selectBuilder.GenerateSelect().GenerateSql()})
    SELECT 1
ELSE
    SELECT 0";
        }

        public override string ToString() => GenerateSql();
    }
}