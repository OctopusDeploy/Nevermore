#nullable enable
using System;
using System.Globalization;
using System.Reflection;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomIdType<T>
    {
        internal CustomIdType(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public override string? ToString()
        {
            return Value?.ToString();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return !(Value is null) && Value.Equals(((CustomIdType<T>) obj).Value);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static CustomIdType<T>? Create(Type customType, T value)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var instance = Activator.CreateInstance(customType, bindingFlags, null, new object[] { value! }, CultureInfo.CurrentCulture);
            return instance as CustomIdType<T>;
        }

        public static TCustomIdType? Create<TCustomIdType>(T value) where TCustomIdType : CustomIdType<T>
        {
            return (TCustomIdType?) Create(typeof(TCustomIdType), value);
        }
    }

    public class StringCustomIdType : CustomIdType<string>
    {
        internal StringCustomIdType(string value) : base(value)
        {
        }
    }
}