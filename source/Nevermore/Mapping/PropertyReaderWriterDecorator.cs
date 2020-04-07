namespace Nevermore.Mapping
{
    public class PropertyReaderWriterDecorator : IPropertyReaderWriter<object>, IPropertyReaderWriterWithConverter
    {
        readonly IPropertyReaderWriter<object> original;

        public PropertyReaderWriterDecorator(IPropertyReaderWriter<object> original)
        {
            this.original = original;
        }

        public virtual object Read(object target)
        {
            return original.Read(target);
        }

        public virtual void Write(object target, object value)
        {
            original.Write(target, value);
        }

        public void Initialize(IDatabaseValueConverter databaseValueConverter)
        {
            if (original is IPropertyReaderWriterWithConverter hasConverter)
            {
                hasConverter.Initialize(databaseValueConverter);
            }
        }
    }
}