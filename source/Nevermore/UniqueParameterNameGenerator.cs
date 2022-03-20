using System;
using System.Collections.Generic;

namespace Nevermore
{
    public interface IUniqueParameterNameGenerator
    {
        string GenerateUniqueParameterName(string parameterName);
        void Return(IEnumerable<string> names);
    }

    internal class UniqueParameterNameGenerator : IUniqueParameterNameGenerator
    {
        readonly HashSet<string> assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public string GenerateUniqueParameterName(string parameterName)
        {
            lock (assigned)
            {
                var original = Parameter.Normalize(parameterName);
                var candidate = original;
                var counter = 0;

                while (!assigned.Add(candidate))
                {
                    counter++;
                    candidate = original + "_" + counter;
                }

                return candidate;
            }
        }

        public void Return(IEnumerable<string> names)
        {
            lock (assigned)
            {
                foreach (var name in names)
                {
                    assigned.Remove(name);
                }
            }
        }
    }
}