using Nevermore.AST;

namespace Nevermore
{
    public interface IUniqueParameterGenerator
    {
        UniqueParameter GenerateUniqueParameterName(Parameter parameter);
    }

    public class UniqueParameterGenerator : IUniqueParameterGenerator
    {
        int parameterCount = 0;

        public UniqueParameter GenerateUniqueParameterName(Parameter parameter)
        {
            var uniqueParameterName = $"{parameter.ParameterName}_{parameterCount++}";
            return CreateUniqueParameter(uniqueParameterName, parameter.DataType);
        }

        class UniqueParameterInstance : UniqueParameter
        {
            public UniqueParameterInstance(string parameterName, IDataType dataType) : base(parameterName, dataType)
            { }
        }

        UniqueParameter CreateUniqueParameter(string parameterName, IDataType dataType)
        {
            return new UniqueParameterInstance(parameterName, dataType);
        }
    }
}