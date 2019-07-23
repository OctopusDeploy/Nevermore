using Nevermore.AST;

namespace Nevermore
{
    public class IsNullWhereParameter
    {
        public IsNullWhereParameter(string fieldName, bool not)
        {
            FieldName = fieldName;
            Not = not;
        }

        public string FieldName { get; }
        public bool Not { get; }
    }
}
