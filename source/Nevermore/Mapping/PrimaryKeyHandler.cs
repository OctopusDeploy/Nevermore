#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Nevermore.Mapping
{
    public interface IPrimitivePrimaryKeyHandler : IPrimaryKeyHandler
    {
        [return: NotNullIfNotNull("id")]
        object? GetPrimitiveValue(object? id);

        object FormatKey(string tableName, int key);
    }

    public abstract class PrimitivePrimaryKeyHandler<T> : IPrimitivePrimaryKeyHandler
    {
        public Type Type => typeof(T);

        [return: NotNullIfNotNull("id")]
        public virtual object? GetPrimitiveValue(object? id)
        {
            return id;
        }

        public abstract object FormatKey(string tableName, int key);
    }

    public interface IStringBasedPrimitivePrimaryKeyHandler : IPrimitivePrimaryKeyHandler
    {
        void SetIdPrefix(Func<(string tableName, int key), string> idPrefix);
    }

    class StringPrimaryKeyHandler : PrimitivePrimaryKeyHandler<string>, IStringBasedPrimitivePrimaryKeyHandler
    {
        Func<(string tableName, int key), string> idPrefixFunc;
        public StringPrimaryKeyHandler(Func<(string tableName, int key), string>? idPrefix = null)
        {
            idPrefixFunc = idPrefix ?? (x => $"{x.tableName}s-{x.key}");
        }

        public void SetIdPrefix(Func<(string tableName, int key), string> idPrefix)
        {
            idPrefixFunc = idPrefix;
        }

        public override object FormatKey(string tableName, int key)
        {
            return idPrefixFunc((tableName, key));
        }
    }

    class IntPrimaryKeyHandler : PrimitivePrimaryKeyHandler<int>
    {
        public override object FormatKey(string tableName, int key)
        {
            return key;
        }
    }

    class LongPrimaryKeyHandler : PrimitivePrimaryKeyHandler<long>
    {
        public override object FormatKey(string tableName, int key)
        {
            return key;
        }
    }

    class GuidPrimaryKeyHandler : PrimitivePrimaryKeyHandler<Guid>
    {
        public override object FormatKey(string tableName, int key)
        {
            return key;
        }
    }

    public interface IIdentityPrimaryKeyHandler : IPrimaryKeyHandler
    {}

    public class IdentityPrimaryKeyHandler<T> : IIdentityPrimaryKeyHandler
    {
        public Type Type => typeof(T);
    }
}