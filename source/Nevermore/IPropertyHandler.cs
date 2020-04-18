using System;

namespace Nevermore
{
    public interface IPropertyHandler
    {
        object Read(object target);
        void Write(object target, object value);
    }
}