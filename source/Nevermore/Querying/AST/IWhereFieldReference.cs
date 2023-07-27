using System;

namespace Nevermore.Querying.AST
{
    public interface IWhereFieldReference
    {
        string GenerateSql();
    }

    public enum StringFunction
    {
        Lower,
        Upper,
        Trim,
        LeftTrim,
        RightTrim,
    }
    
    public class WhereFieldReferenceWithStringFunction : IWhereFieldReference
    {
        readonly IWhereFieldReference inner;
        readonly StringFunction function;

        public WhereFieldReferenceWithStringFunction(IWhereFieldReference inner, StringFunction function)
        {
            this.inner = inner;
            this.function = function;
        }

        public string GenerateSql()
        {
            switch (function)
            {
                case StringFunction.Lower:
                    return $"LOWER({inner.GenerateSql()})";
                case StringFunction.Upper:
                    return $"UPPER({inner.GenerateSql()})";
                case StringFunction.Trim:
                    return $"TRIM({inner.GenerateSql()})";
                case StringFunction.LeftTrim:
                    return $"LTRIM({inner.GenerateSql()})";
                case StringFunction.RightTrim:
                    return $"RTRIM({inner.GenerateSql()})";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class WhereFieldReference : IWhereFieldReference
    {
        readonly string fieldName;

        public WhereFieldReference(string fieldName)
        {
            this.fieldName = fieldName;
        }

        public string GenerateSql() => $"[{fieldName}]";
    }

    public class AliasedWhereFieldReference : IWhereFieldReference
    {
        readonly string tableAlias;
        readonly WhereFieldReference fieldReference;

        public AliasedWhereFieldReference(string tableAlias, WhereFieldReference fieldReference)
        {
            this.tableAlias = tableAlias;
            this.fieldReference = fieldReference;
        }

        public string GenerateSql() => $"{tableAlias}.{fieldReference.GenerateSql()}";
    }

    public class JsonValueFieldReference : IWhereFieldReference
    {
        readonly string jsonPath;

        public JsonValueFieldReference(string jsonPath)
        {
            this.jsonPath = jsonPath;
        }

        public string GenerateSql() => $"JSON_VALUE([JSON], 'strict {jsonPath}')";
    }

    public class JsonQueryFieldReference : IWhereFieldReference
    {
        readonly string jsonPath;

        public JsonQueryFieldReference(string jsonPath)
        {
            this.jsonPath = jsonPath;
        }

        public string GenerateSql() => $"JSON_QUERY([JSON], 'strict {jsonPath}')";
    }
}