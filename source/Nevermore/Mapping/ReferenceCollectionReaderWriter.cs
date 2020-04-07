
using System.Collections.Generic;
using System.Linq;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    class ReferenceCollectionReaderWriter : PropertyReaderWriterDecorator
    {
        public ReferenceCollectionReaderWriter(IPropertyReaderWriter<object> original) : base(original)
        {
        }

        public override object Read(object target)
        {
            var value = base.Read(target) as ReferenceCollection;
            if (value == null || value.Count == 0)
                return "";

            return UnParse(value);
        }

        public override void Write(object target, object value)
        {
            var collection = base.Read(target) as ReferenceCollection;
            if (collection == null)
            {
                base.Write(target, collection = new ReferenceCollection());
            }

            collection.ReplaceAll(Parse(value?.ToString()));
        }

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