using System;
using System.Collections;
using System.Collections.Generic;

namespace Nevermore.TypedStrings
{
    public class TypedStringComparer<T> : IComparer, IEqualityComparer, IComparer<T>, IEqualityComparer<T> where T : TypedString
    {
        public static TypedStringComparer<T> Ordinal { get; } = new TypedStringComparer<T>(StringComparer.Ordinal);
        public static TypedStringComparer<T> CurrentCulture { get; } = new TypedStringComparer<T>(StringComparer.CurrentCulture);
        public static TypedStringComparer<T> InvariantCulture { get; } = new TypedStringComparer<T>(StringComparer.InvariantCulture);

        public static TypedStringComparer<T> OrdinalIgnoreCase { get; } = new TypedStringComparer<T>(StringComparer.OrdinalIgnoreCase);
        public static TypedStringComparer<T> CurrentCultureIgnoreCase { get; } = new TypedStringComparer<T>(StringComparer.CurrentCultureIgnoreCase);
        public static TypedStringComparer<T> InvariantCultureIgnoreCase { get; } = new TypedStringComparer<T>(StringComparer.InvariantCultureIgnoreCase);

        readonly StringComparer innerComparer;

        TypedStringComparer(StringComparer innerComparer)
        {
            this.innerComparer = innerComparer;
        }

        public static TypedStringComparer<T> Of(StringComparer stringComparer)
        {
            return new TypedStringComparer<T>(stringComparer);
        }

        public int Compare(object x, object y)
        {
            return innerComparer.Compare(x, y);
        }

        public bool Equals(object x, object y)
        {
            return innerComparer.Equals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return innerComparer.GetHashCode(obj);
        }

        public int Compare(T x, T y)
        {
            return innerComparer.Compare(x, y);
        }

        public bool Equals(T x, T y)
        {
            return innerComparer.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return innerComparer.GetHashCode(obj);
        }
    }
}
