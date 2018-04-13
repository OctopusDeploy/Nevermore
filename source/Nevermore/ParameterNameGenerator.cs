namespace Nevermore
{
    public interface IParameterNameGenerator
    {
        string GenerateUniqueParametername(string parameterDescription);
    }

    class ParameterNameGenerator : IParameterNameGenerator
    {
        int parameterCount = 1;
        public string GenerateUniqueParametername(string parameterDescription)
        {
            return $"{new Parameter(parameterDescription).ParameterName}_{parameterCount++}";
        }
    }
}