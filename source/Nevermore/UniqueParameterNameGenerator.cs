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
            var original = Parameter.Normalize(parameterName);
            var candidate = original;
            var counter = 0;
            
            while (true)
            {
                if (!assigned.Contains(candidate))
                {
                    assigned.Add(candidate);
                    return candidate;
                }
                
                counter++;
                candidate = original + "_" + counter;
            }
        }
    
        public void Return(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                assigned.Remove(name);
            }
        }
    }
}