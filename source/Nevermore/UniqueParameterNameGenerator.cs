namespace Nevermore
{
    public interface IUniqueParameterNameGenerator
    {
        string GenerateUniqueParameterName(string parameterName);
    }

    internal class UniqueParameterNameGenerator : IUniqueParameterNameGenerator
    {
        int parameterCount;
        
        public string GenerateUniqueParameterName(string parameterName)
        {
            return $"{new Parameter(parameterName).ParameterName}_{parameterCount++}";
        }
    }
}