namespace Nevermore.Mapping
{
    public class PropertyReaderWriterDecorator : IPropertyReaderWriter
    {
        readonly IPropertyReaderWriter original;

        public PropertyReaderWriterDecorator(IPropertyReaderWriter original)
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
    }
}