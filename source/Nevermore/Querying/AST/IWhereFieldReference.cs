﻿namespace Nevermore.Querying.AST
{
    public interface IWhereFieldReference
    {
        string GenerateSql();
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