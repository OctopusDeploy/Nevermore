namespace Nevermore.Mapping
{
    public class UniqueRule
    {
        public UniqueRule(string constraintName, params string[] columns)
        {
            ConstraintName = constraintName;
            Columns = columns;
        }

        public string Message { get; set; }
        public string ConstraintName { get; set; }
        public string[] Columns { get; set; }
    }
}