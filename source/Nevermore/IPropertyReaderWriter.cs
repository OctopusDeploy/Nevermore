using System;

namespace Nevermore
{
    public interface IPropertyReaderWriter
    {
        object Read(object target);
        void Write(object target, object value);
    }
}