using System.Collections.ObjectModel;

namespace Nevermore
{
    public class Parameters : KeyedCollection<string, Parameter>
    {
        public Parameters()
        {
        }

        public Parameters(Parameters parameters)
        {
            AddRange(parameters);
        }

        public Parameters(params Parameters[] parameters)
        {
            foreach (var parametersCollection in parameters)
                AddRange(parametersCollection);
        }

        protected override string GetKeyForItem(Parameter item) => item.ParameterName;

        public void AddRange(Parameters parameters)
        {
            foreach (var parameter in parameters)
                Add(parameter);
        }
    }
}