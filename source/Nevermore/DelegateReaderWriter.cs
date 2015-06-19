using System;

namespace Nevermore
{
    public class DelegateReaderWriter<TTarget, TProperty> : IPropertyReaderWriter<object>
    {
        readonly Func<TTarget, TProperty> reader;
        readonly Action<TTarget, TProperty> writer;

        public DelegateReaderWriter(Func<TTarget, TProperty> reader) : this(reader, null)
        {
        }

        public DelegateReaderWriter(Func<TTarget, TProperty> reader, Action<TTarget, TProperty> writer)
        {
            this.reader = reader;
            this.writer = writer;
        }

        public object Read(object target)
        {
            return reader((TTarget) target);
        }

        public void Write(object target, object value)
        {
            if (writer != null)
                writer((TTarget) target, (TProperty) value);
        }
    }
}