using System;
using Newtonsoft.Json;

namespace Nevermore.TypedStrings
{
    [JsonConverter(typeof(TypedStringConverter))]
    public abstract class TypedString : IEquatable<TypedString>
    {
        protected TypedString(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static implicit operator string(TypedString typedString) => typedString?.Value;

        public override string ToString()
        {
            return Value;
        }

        public bool Equals(TypedString other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypedString) obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(TypedString left, TypedString right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypedString left, TypedString right)
        {
            return !Equals(left, right);
        }
    }
}