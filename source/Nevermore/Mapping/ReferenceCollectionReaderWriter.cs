using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Mapping
{
    public static class ReferenceCollectionReaderWriter
    {
        public static IEnumerable<string> Parse(string value)
        {
            return (value ?? string.Empty).Split('|').Where(item => !string.IsNullOrWhiteSpace(item));
        }
 
        public static string UnParse(IEnumerable<string> items)
        {
            return $"|{string.Join("|", items)}| ";
        }
    }
}