namespace Nevermore.AST
{
    public class SelectConstant : ISelect 
    {
        readonly string constant;

        public SelectConstant(string constant)
        {
            this.constant = constant;
        }

        public string GenerateSql()
        {
            return $"SELECT {constant}";
        }
    }
}