using System;

namespace Nevermore
{
    public interface IPropertyReaderWriter<TCast>
    {
        TCast Read(object target);
        void Write(object target, TCast value);
    }
}