using System;

namespace Nevermore.Mapping
{
    public class IdentityPrimaryKeyHandler<T> : IIdentityPrimaryKeyHandler
    {
        public Type Type => typeof(T);
    }
}