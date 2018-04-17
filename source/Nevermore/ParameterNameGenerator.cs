namespace Nevermore
{
    public interface IUniqueParameterGenerator
    {
        UniqueParameter GenerateUniqueParameterName(Parameter parameter);
    }

    class UniqueParameterGenerator : IUniqueParameterGenerator
    {
        int parameterCount = 0;
        public UniqueParameter GenerateUniqueParameterName(Parameter parameter)
        {
            var uniqueParameterName = $"{parameter.ParameterName}_{parameterCount++}";
            return new UniqueParameter(uniqueParameterName, parameter.DataType);
        }
    }
}