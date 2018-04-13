namespace Nevermore
{
    public interface IParameterNameGenerator
    {
        string GenerateUniqueParametername(string parameterDescription);
    }

    class ParameterNameGenerator : IParameterNameGenerator
    {
        int parameterCount;
        public string GenerateUniqueParametername(string parameterDescription)
        {
            return $"{new Parameter(parameterDescription).ParameterName}_{parameterCount++}";
        }
    }
}