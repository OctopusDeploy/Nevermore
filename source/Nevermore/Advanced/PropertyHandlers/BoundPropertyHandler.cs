using System;

namespace Nevermore
{
    /// <summary>
    /// A wrapper that binds IPropertyHandler and a target object together so you can call Read() directly
    /// </summary>
    class BoundPropertyHandler
    {
        readonly IPropertyHandler handler;
        readonly object target;

        public BoundPropertyHandler(IPropertyHandler handler, object target)
        {
            this.handler = handler;
            this.target = target;
        }

        public bool CanRead => handler.CanRead;
        public bool CanWrite => handler.CanWrite;
            
        public object Read() => handler.Read(target);
        public void Write(object value) => handler.Write(target, value);
    }
}