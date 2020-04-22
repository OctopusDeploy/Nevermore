using System;

namespace Nevermore.Mapping
{
    public class DelegatePropertyHandler<TTarget, TValue> : IPropertyHandler
    {
        readonly Func<TTarget, TValue> getter;
        readonly Action<TTarget, TValue> setter;

        public DelegatePropertyHandler(Func<TTarget, TValue> getter, Action<TTarget, TValue> setter = null)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public bool CanRead => getter != null;

        public object Read(object target)
        {
            return getter((TTarget) target);
        }

        public bool CanWrite => setter != null;

        public void Write(object target, object value)
        {
            setter((TTarget)target, (TValue)value);
        }
    }
}