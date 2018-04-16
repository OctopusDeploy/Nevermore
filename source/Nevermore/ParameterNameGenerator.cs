namespace Nevermore
{
    public interface IParameterNameGenerator
    {
        string GenerateUniqueParameterName(string parameterDescription);
    }

    class ParameterNameGenerator : IParameterNameGenerator
    {
        int parameterCount = 0;
        public string GenerateUniqueParameterName(string parameterDescription)
        {
            return $"{new Parameter(parameterDescription).ParameterName}_{parameterCount++}";
        }
    }
}