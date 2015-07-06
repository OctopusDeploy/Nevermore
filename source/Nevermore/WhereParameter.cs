namespace Nevermore
{
    public class WhereParameter
    {
        public string FieldName { get; set; }
        public SqlOperand Operand { get; set; }
        public object Value { get; set; }
        public string ParameterName { get { return FieldName.ToLower(); } }
    }
}
