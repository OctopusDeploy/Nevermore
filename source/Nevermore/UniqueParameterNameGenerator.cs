namespace Nevermore
{
    public interface IUniqueParameterNameGenerator
    {
        string GenerateUniqueParameterName(string parameterName);
    }

    class UniqueParameterNameGenerator : IUniqueParameterNameGenerator
    {
        int parameterCount = 0;
        public string GenerateUniqueParameterName(string parameterName)
        {
            return $"{new Parameter(parameterName).ParameterName}_{parameterCount++}";
        }
    }
}