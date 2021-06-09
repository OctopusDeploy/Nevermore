using System;

namespace Nevermore.Util
{
    static class StronglyTypeIdExtensionMethods
    {
        public static bool IsStronglyTypedId(this Type type)
        {
            return type.IsClass && type != typeof(string);
        }
    }
}