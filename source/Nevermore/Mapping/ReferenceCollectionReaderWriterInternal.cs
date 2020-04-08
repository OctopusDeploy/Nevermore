using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    class ReferenceCollectionReaderWriterInternal : PropertyReaderWriterDecorator
    {
        public ReferenceCollectionReaderWriterInternal(IPropertyReaderWriter<object> original) : base(original)
        {
        }

        public override object Read(object target)
        {
            var value = base.Read(target) as ReferenceCollection;
            if (value == null || value.Count == 0)
                return "";

            return ReferenceCollectionReaderWriter.UnParse(value);
        }

        public override void Write(object target, object value)
        {
            var collection = base.Read(target) as ReferenceCollection;
            if (collection == null)
            {
                base.Write(target, collection = new ReferenceCollection());
            }

            collection.ReplaceAll(ReferenceCollectionReaderWriter.Parse(value?.ToString()));
        }
    }
}