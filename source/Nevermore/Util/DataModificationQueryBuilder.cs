using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json;

namespace Nevermore.Util
{
    /// <summary>
    /// Designed to only be used by RelationalTransaction directly
    /// </summary>
    internal class DataModificationQueryBuilder
    {
        const string IdVariableName = "Id";
        const string JsonVariableName = "JSON";

        readonly RelationalMappings mappings;
        readonly JsonSerializerSettings jsonSerializerSettings;

        public DataModificationQueryBuilder(RelationalMappings mappings, JsonSerializerSettings jsonSerializerSettings)
        {
            this.mappings = mappings;
            this.jsonSerializerSettings = jsonSerializerSettings;
        }

        (DocumentMap mapping, string statement, CommandParameterValues parameterValues) CreateInsertSingle(
            IId document,
            string tableName,
            string tableHint,
            Func<DocumentMap, string> allocateId)
        {
            var mapping = mappings.Get(document.GetType());
            var statement = string.Format(
                "INSERT INTO dbo.[{0}] {1} ({2}) VALUES \r\n({3})\r\n",
                tableName ?? mapping.TableName,
                tableHint ?? "",
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Union(new[] {mapping.IdColumn.ColumnName, "JSON"})),
                string.Join(", ", mapping.IndexedColumns.Select(c => "@" + c.ColumnName).Union(new[] {"@" + IdVariableName, "@JSON"}))
            );

            var parameters = GetDocumentParameters(allocateId, document, mapping);

            return (mapping, statement, parameters);
        }

        public (DocumentMap mapping, string statement, CommandParameterValues parameterValues) CreateInsert(
            IReadOnlyList<IId> documents,
            string tableName,
            string tableHint,
            Func<DocumentMap, string> allocateId,
            bool includeDefaultModelColumns)
        {
            if (documents.Count == 1)
                return CreateInsertSingle(documents[0], tableName, tableHint, allocateId);

            var mapping = mappings.Get(documents.First().GetType()); // All documents share the same mapping.

            var parameters = new CommandParameterValues();
            var valueStatements = new List<string>();
            var documentCount = 0;
            foreach (var document in documents)
            {
                var prefix = $"{documentCount}__";
                var documentParameters = GetDocumentParameters(allocateId, document, mapping, prefix);

                parameters.AddRange(documentParameters);

                var defaultIndexColumnPlaceholders = new string[] { };
                if (includeDefaultModelColumns)
                    defaultIndexColumnPlaceholders = new[] {$"@{prefix}Id", $"@{prefix}JSON"};

                valueStatements.Add($"({string.Join(", ", mapping.IndexedColumns.Select(c => $"@{prefix}{c.ColumnName}").Union(defaultIndexColumnPlaceholders))})");

                documentCount++;
            }

            var defaultIndexColumns = new string[] { };
            if (includeDefaultModelColumns)
                defaultIndexColumns = new[] {mapping.IdColumn.ColumnName, "JSON"};

            var statement = string.Format(
                "INSERT INTO dbo.[{0}] {1} ({2}) VALUES \r\n{3}\r\n",
                tableName ?? mapping.TableName,
                tableHint ?? "",
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Union(defaultIndexColumns)),
                string.Join("\r\n,", valueStatements)
            );

            return (mapping, statement, parameters);
        }


        public (DocumentMap, string, CommandParameterValues) CreateUpdate(IId document, string tableHint)
        {
            var mapping = mappings.Get(document.GetType());

            var updates = string.Join(", ", mapping.IndexedColumns.Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName).Union(new[] {$"[JSON] = @{JsonVariableName}"}));
            var statement = $"UPDATE dbo.[{mapping.TableName}] {tableHint ?? ""} SET {updates} WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}";

            var parameters = GetDocumentParameters(
                m => throw new Exception("Cannot update a document if it does not have an ID"),
                document,
                mapping,
                null
            );

            return (mapping, statement, parameters);
        }

        public (string statement, CommandParameterValues parameterValues) CreateDelete(IId document)
        {
            var mapping = mappings.Get(document.GetType());
            var id = (string) mapping.IdColumn.ReaderWriter.Read(document);
            var statement = CreateDelete(mapping, $"WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}");
            var parameters = new CommandParameterValues {{IdVariableName, id}};
            return (statement, parameters);
        }

        public (string statement, CommandParameterValues parameterValues) CreateDelete<TDocument>(string id)
            where TDocument : class, IId
        {
            var mapping = mappings.Get(typeof(TDocument));
            var statement = CreateDelete(mapping, $"WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}");
            var parameters = new CommandParameterValues {{IdVariableName, id}};
            return (statement, parameters);
        }

        public string CreateDelete(Type documentType, Where where)
            => CreateDelete(mappings.Get(documentType), where.GenerateSql());

        string CreateDelete(DocumentMap mapping, string whereClause)
        {
            return $"DELETE FROM [{mapping.TableName}] {whereClause}";
        }

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, IId document, DocumentMap mapping, string prefix = null)
        {
            var id = (string) mapping.IdColumn.ReaderWriter.Read(document);
            if (string.IsNullOrWhiteSpace(id))
                id = allocateId(mapping);
            
            var result = new CommandParameterValues
            {
                [$"{prefix}{IdVariableName}"] = id
            };

            var mType = mapping.InstanceTypeResolver.GetTypeFromInstance(document);

            result[$"{prefix}{JsonVariableName}"] = JsonConvert.SerializeObject(document, mType, jsonSerializerSettings);

            foreach (var c in mappings.Get(mType).IndexedColumns)
            {
                var value = c.ReaderWriter.Read(document);
                if (value != null && value != DBNull.Value && value is string && c.MaxLength > 0)
                {
                    var attemptedLength = ((string) value).Length;
                    if (attemptedLength > c.MaxLength)
                    {
                        throw new StringTooLongException(string.Format("An attempt was made to store {0} characters in the {1}.{2} column, which only allows {3} characters.", attemptedLength, mapping.TableName, c.ColumnName, c.MaxLength));
                    }
                }
                else if (value != null && value != DBNull.Value && value is DateTime && value.Equals(DateTime.MinValue))
                {
                    value = SqlDateTime.MinValue.Value;
                }

                result[$"{prefix}{c.ColumnName}"] = value;
            }

            return result;
        }
    }
}