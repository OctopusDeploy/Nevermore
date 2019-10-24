using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    class ExtensibleEnumReaderWriter : PropertyReaderWriterDecorator
    {
        public ExtensibleEnumReaderWriter(IPropertyReaderWriter<object> original) : base(original)
        {
        }

        public override object Read(object target)
        {
            var value = base.Read(target) as ExtensibleEnum;
            if (value == null)
                return "";

            return value.Name;
        }

        public override void Write(object target, object value)
        {
            // ExtensibleEnum is write only to the database by it's nature, we don't need to write to
            // the object here because the ExtensibleEnumConverter will have taken care of that.
        }
    }
}