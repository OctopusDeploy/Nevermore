using System;

namespace Nevermore.Mapping
{
    public class DelegateHandler<TTarget, TProperty> : IPropertyHandler
    {
        readonly Func<TTarget, TProperty> reader;
        readonly Action<TTarget, TProperty> writer;

        public DelegateHandler(Func<TTarget, TProperty> reader) : this(reader, null)
        {
        }

        public DelegateHandler(Func<TTarget, TProperty> reader, Action<TTarget, TProperty> writer)
        {
            this.reader = reader;
            this.writer = writer;
        }

        public object Read(object target)
        {
            return reader((TTarget)target);
        }

        public void Write(object target, object value)
        {
            if (writer != null)
                writer((TTarget)target, (TProperty)value);
        }
    }
}