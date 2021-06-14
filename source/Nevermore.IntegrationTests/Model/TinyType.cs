using System;
using System.Globalization;
using System.Reflection;

namespace Nevermore.IntegrationTests.Model
{
    public class TinyType<T>
    {
        internal TinyType(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Value.Equals(((TinyType<T>) obj).Value);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static TinyType<T> Create(Type tinyType, T value)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var instance = Activator.CreateInstance(tinyType, bindingFlags, null, new object[] {value}, CultureInfo.CurrentCulture);
            return (TinyType<T>) instance;
        }

        public static TTinyType Create<TTinyType>(T value) where TTinyType : TinyType<T>
        {
            return (TTinyType) Create(typeof(TTinyType), value);
        }
    }

    public class StringTinyType : TinyType<string>
    {
        internal StringTinyType(string value) : base(value)
        {
        }
    }
}