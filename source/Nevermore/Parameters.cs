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
            foreach (var parameter in parameters)
            {
                Add(parameter);
            }
        }

        protected override string GetKeyForItem(Parameter item) => item.ParameterName;
    }
}