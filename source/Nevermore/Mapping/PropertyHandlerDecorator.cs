namespace Nevermore.Mapping
{
    public class PropertyHandlerDecorator : IPropertyHandler
    {
        readonly IPropertyHandler original;

        public PropertyHandlerDecorator(IPropertyHandler original)
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