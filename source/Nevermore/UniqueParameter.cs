namespace Nevermore
{
    public sealed class UniqueParameter : Parameter
    {
        public UniqueParameter(IUniqueParameterNameGenerator parameterNameGenerator, Parameter parameter) 
            : base(parameterNameGenerator.GenerateUniqueParameterName(parameter.ParameterName), parameter.DataType)
        {
        }
    }
}