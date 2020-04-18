using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using Nevermore.Advanced;
using Nevermore.Advanced.Serialization;
using Nevermore.Mapping;
using Nevermore.Querying.AST;

namespace Nevermore.Util
{
    /// <summary>
    /// Designed to only be used by RelationalTransaction directly
    /// </summary>
    internal class DataModificationQueryBuilder
    {
        const string IdVariableName = "Id";
        const string JsonVariableName = "JSON";
        const string JsonBlobVariableName = "JSONBlob";

        readonly IDocumentMapRegistry mappings;
        readonly IDocumentSerializer serializer;
        readonly Func<DocumentMap, string> keyAllocator;

        public DataModificationQueryBuilder(IDocumentMapRegistry mappings, IDocumentSerializer serializer, Func<DocumentMap, string> keyAllocator)
        {
            this.mappings = mappings;
            this.serializer = serializer;
            this.keyAllocator = keyAllocator;
        }

        public PreparedCommand PrepareInsert(IReadOnlyList<object> documents, InsertOptions options = null)
        {
            options ??= InsertOptions.Default;
            var mapping = GetMapping(documents);

            var sb = new StringBuilder();
            AppendInsertStatement(sb, mapping, options.TableName, options.Hint, documents.Count, options.IncludeDefaultModelColumns);
            var parameters = GetDocumentParameters(m => string.IsNullOrEmpty(options.CustomAssignedId) ? keyAllocator(m) : options.CustomAssignedId, documents, mapping);

            AppendRelatedDocumentStatementsForInsert(sb, parameters, mapping, documents);
            return new PreparedCommand(sb.ToString(), parameters, RetriableOperation.Insert, mapping, options.CommandTimeout);
        }

        public PreparedCommand PrepareUpdate(object document, UpdateOptions options = null)
        {
            options ??= UpdateOptions.Default;
            
            var mapping = mappings.Resolve(document.GetType());

            var updateStatements = mapping.WritableIndexedColumns().Select(c => $"[{c.ColumnName}] = @{c.ColumnName}").ToList();
            switch (mapping.JsonStorageFormat)
            {
                case JsonStorageFormat.TextOnly:
                    updateStatements.Add($"[{JsonVariableName}] = @{JsonVariableName}");
                    break;
                case JsonStorageFormat.CompressedOnly:
                    updateStatements.Add($"[{JsonBlobVariableName}] = @{JsonBlobVariableName}");
                    break;
                case JsonStorageFormat.MixedPreferCompressed:
                    updateStatements.Add($"[{JsonVariableName}] = @{JsonVariableName}");
                    updateStatements.Add($"[{JsonBlobVariableName}] = @{JsonBlobVariableName}");
                    break;
                case JsonStorageFormat.MixedPreferText:
                    updateStatements.Add($"[{JsonVariableName}] = @{JsonVariableName}");
                    updateStatements.Add($"[{JsonBlobVariableName}] = @{JsonBlobVariableName}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            var updates = string.Join(", ", updateStatements);
            
            var statement = $"UPDATE dbo.[{mapping.TableName}] {options.Hint ?? ""} SET {updates} WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}";

            var parameters = GetDocumentParameters(
                m => throw new Exception("Cannot update a document if it does not have an ID"),
                document,
                mapping
            );

            statement = AppendRelatedDocumentStatementsForUpdate(statement, parameters, mapping, document);
            return new PreparedCommand(statement, parameters, RetriableOperation.Update, mapping, options.CommandTimeout);
        }

        public PreparedCommand PrepareDelete(object document, DeleteOptions options = null)
        {
            var mapping = mappings.Resolve(document.GetType());
            var id = (string) mapping.IdColumn.PropertyHandler.Read(document);
            return PrepareDelete(mapping, id, options);
        }

        public PreparedCommand PrepareDelete<TDocument>(string id, DeleteOptions options = null) where TDocument : class
        {
            var mapping = mappings.Resolve(typeof(TDocument));
            return PrepareDelete(mapping, id, options);
        }

        static PreparedCommand PrepareDelete(DocumentMap mapping, string id, DeleteOptions options = null)
        {
            options ??= DeleteOptions.Default;
            var statement = new StringBuilder();
            statement.AppendLine($"DELETE FROM [{mapping.TableName}] WITH (ROWLOCK) WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}");

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName, idColumnName: m.IdColumnName)).Distinct())
                statement.AppendLine($"DELETE FROM [{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] = @{IdVariableName}");

            var parameters = new CommandParameterValues {{IdVariableName, id}};
            return new PreparedCommand(statement.ToString(), parameters, RetriableOperation.Delete, mapping, options.CommandTimeout);
        }

        public PreparedCommand PrepareDelete(Type documentType, Where where, CommandParameterValues parameters, DeleteOptions options = null)
        {
            return PrepareDelete(mappings.Resolve(documentType), where, parameters, options);
        }

        public PreparedCommand PrepareDelete(DocumentMap mapping, Where where, CommandParameterValues parameters, DeleteOptions options = null)
        {
            options ??= DeleteOptions.Default;
            
            if (!mapping.RelatedDocumentsMappings.Any())
                return new PreparedCommand($"DELETE FROM [{options.TableName ?? mapping.TableName}]{options.Hint??""} {where.GenerateSql()}", parameters, RetriableOperation.Delete, mapping, options.CommandTimeout);

            var statement = new StringBuilder();
            statement.AppendLine("DECLARE @Ids as TABLE (Id nvarchar(400))");
            statement.AppendLine();
            statement.AppendLine("INSERT INTO @Ids");
            statement.AppendLine($"SELECT [{mapping.IdColumn.ColumnName}]");
            statement.AppendLine($"FROM [{mapping.TableName}] WITH (ROWLOCK)");
            statement.AppendLine(where.GenerateSql());
            statement.AppendLine();

            statement.AppendLine($"DELETE FROM [{mapping.TableName}] WITH (ROWLOCK) WHERE [{mapping.IdColumn.ColumnName}] in (SELECT Id FROM @Ids)");

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName, idColumnName: m.IdColumnName)).Distinct())
                statement.AppendLine($"DELETE FROM [{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] in (SELECT Id FROM @Ids)");

            return new PreparedCommand(statement.ToString(), parameters, RetriableOperation.Delete, mapping, options.CommandTimeout);
        }

        DocumentMap GetMapping(IReadOnlyList<object> documents)
        {
            var allMappings = documents.Select(i => mappings.Resolve(i)).Distinct().ToArray();
            if (allMappings.Length == 0)
                throw new Exception($"No mapping found for type {documents[0].GetType()}");

            if (allMappings.Length != 1)
                throw new Exception("InsertMany cannot be used with documents that have different mappings");
            return allMappings[0];
        }

        // TODO: includeDefaultModelColumns seems dumb. Use a NonQuery instead?
        void AppendInsertStatement(StringBuilder sb, DocumentMap mapping, string tableName, string tableHint, int numberOfInstances, bool includeDefaultModelColumns)
        {
            var columns = new List<string>();
            
            if (includeDefaultModelColumns) 
                columns.Add(mapping.IdColumn.ColumnName);
            
            columns.AddRange(mapping.WritableIndexedColumns().Select(c => c.ColumnName));
            
            if (includeDefaultModelColumns)
            {
                switch (mapping.JsonStorageFormat)
                {
                    case JsonStorageFormat.TextOnly:
                        columns.Add(JsonVariableName);
                        break;
                    case JsonStorageFormat.CompressedOnly:
                        columns.Add(JsonBlobVariableName);
                        break;
                    case JsonStorageFormat.MixedPreferCompressed:
                    case JsonStorageFormat.MixedPreferText:
                        columns.Add(JsonBlobVariableName);
                        columns.Add(JsonVariableName);
                        break;
                }
            }

            var columnNames = string.Join(", ", columns.Select(columnName => $"[{columnName}]"));

            var actualTableName = tableName ?? mapping.TableName;

            sb.AppendLine($"INSERT INTO dbo.[{actualTableName}] {tableHint} ({columnNames}) VALUES ");

            void Append(string prefix)
            {
                var columnVariableNames = string.Join(", ", columns.Select(c => $"@{prefix}{c}"));
                sb.AppendLine($"({columnVariableNames})");
            }

            if (numberOfInstances == 1)
            {
                Append("");
                return;
            }

            for (var x = 0; x < numberOfInstances; x++)
            {
                if (x > 0)
                    sb.Append(",");

                Append($"{x}__");
            }
        }


        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, IReadOnlyList<object> documents, DocumentMap mapping)
        {
            if (documents.Count == 1)
                return GetDocumentParameters(allocateId, documents[0], mapping, "");

            var parameters = new CommandParameterValues();
            for (var x = 0; x < documents.Count; x++)
            {
                var instanceParameters = GetDocumentParameters(allocateId, documents[x], mapping, $"{x}__");
                parameters.AddRange(instanceParameters);
            }

            return parameters;
        }

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, object document, DocumentMap mapping, string prefix = null)
        {
            var id = (string) mapping.IdColumn.PropertyHandler.Read(document);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = allocateId(mapping);
                mapping.IdColumn.PropertyHandler.Write(document, id);
            }

            var result = new CommandParameterValues
            {
                [$"{prefix}{IdVariableName}"] = id
            };

            switch (mapping.JsonStorageFormat)
            {
                case JsonStorageFormat.TextOnly:
                    result[$"{prefix}{JsonVariableName}"] = serializer.SerializeText(document, mapping);
                    break;
                case JsonStorageFormat.CompressedOnly:
                    result[$"{prefix}{JsonBlobVariableName}"] = serializer.SerializeCompressed(document, mapping);
                    break;
                case JsonStorageFormat.MixedPreferCompressed:
                    result[$"{prefix}{JsonBlobVariableName}"] = serializer.SerializeCompressed(document, mapping);
                    result[$"{prefix}{JsonVariableName}"] = null;
                    break;
                case JsonStorageFormat.MixedPreferText:
                    result[$"{prefix}{JsonVariableName}"] = serializer.SerializeText(document, mapping);
                    result[$"{prefix}{JsonBlobVariableName}"] = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            foreach (var c in mapping.WritableIndexedColumns())
            {
                var value = c.PropertyHandler.Read(document);
                if (value != null && value != DBNull.Value && value is string && c.MaxLength > 0)
                {
                    var attemptedLength = ((string) value).Length;
                    if (attemptedLength > c.MaxLength)
                    {
                        throw new StringTooLongException($"An attempt was made to store {attemptedLength} characters in the {mapping.TableName}.{c.ColumnName} column, which only allows {c.MaxLength} characters.");
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


        void AppendRelatedDocumentStatementsForInsert(
            StringBuilder sb,
            CommandParameterValues parameters,
            DocumentMap mapping,
            IReadOnlyList<object> documents)
        {
            var relatedDocumentData = GetRelatedDocumentTableData(mapping, documents);

            foreach (var data in relatedDocumentData.Where(g => g.Related.Length > 0))
            {
                var relatedVariablePrefix = $"{data.TableName.ToLower()}_";

                sb.AppendLine($"INSERT INTO [{data.TableName}] ([{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}]) VALUES");
                var related = data.Related;

                for (var x = 0; x < related.Length; x++)
                {
                    var parentIdVariable = related[x].parentIdVariable;
                    var relatedDocumentId = related[x].relatedDocumentId;
                    var relatedTableName = related[x].relatedTableName;

                    var relatedVariableName = relatedVariablePrefix + x;
                    parameters.Add(relatedVariableName, relatedDocumentId);
                    if (x > 0)
                        sb.Append(",");
                    sb.AppendLine($"(@{parentIdVariable}, '{mapping.TableName}', @{relatedVariableName}, '{relatedTableName}')");
                }
            }
        }

        string AppendRelatedDocumentStatementsForUpdate(
            string statement,
            CommandParameterValues parameters,
            DocumentMap mapping,
            object document)
        {
            var relatedDocumentData = GetRelatedDocumentTableData(mapping, new[] {document});
            if (relatedDocumentData.Count == 0)
                return statement;

            var sb = new StringBuilder();
            sb.AppendLine(statement);
            sb.AppendLine();

            if (relatedDocumentData.Any(d => d.Related.Any()))
                sb.AppendLine("DECLARE @references as TABLE (Reference nvarchar(400), ReferenceTable nvarchar(400))");

            foreach (var data in relatedDocumentData)
            {
                if (data.Related.Any())
                {
                    var relatedVariablePrefix = $"{data.TableName.ToLower()}_";

                    sb.AppendLine();
                    sb.AppendLine("DELETE FROM @references");
                    sb.AppendLine();

                    var valueBlocks = data.Related.Select((r, idx) => $"(@{relatedVariablePrefix}{idx}, '{r.relatedTableName}')");
                    sb.Append("INSERT INTO @references VALUES ");
                    sb.AppendLine(string.Join(", ", valueBlocks));
                    sb.AppendLine();

                    sb.AppendLine($"DELETE FROM [{data.TableName}] WHERE [{data.IdColumnName}] = @{IdVariableName}");
                    sb.AppendLine($"    AND [{data.RelatedDocumentIdColumnName}] not in (SELECT Reference FROM @references)");
                    sb.AppendLine();

                    sb.AppendLine($"INSERT INTO [{data.TableName}] ([{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}])");
                    sb.AppendLine($"SELECT @{IdVariableName}, '{mapping.TableName}', Reference, ReferenceTable FROM @references t");
                    sb.AppendLine($"WHERE NOT EXISTS (SELECT null FROM [{data.TableName}] r WHERE r.[{data.IdColumnName}] = @{IdVariableName} AND r.[{data.RelatedDocumentIdColumnName}] = t.Reference )");


                    for (var x = 0; x < data.Related.Length; x++)
                        parameters.Add(relatedVariablePrefix + x, data.Related[x].relatedDocumentId);
                }
                else
                {
                    sb.AppendLine($"DELETE FROM [{data.TableName}] WHERE [{data.IdColumnName}] = @Id");
                }
            }

            return sb.ToString();
        }

        IReadOnlyList<RelatedDocumentTableData> GetRelatedDocumentTableData(DocumentMap mapping, IReadOnlyList<object> documents)
        {
            var documentAndIds = documents.Count == 1
                ? new[] {(parentIdVariable: IdVariableName, document: documents[0])}
                : documents.Select((i, idx) => (idVariable: $"{idx}__{IdVariableName}", document: i));
            
            var groupedByTable = from m in mapping.RelatedDocumentsMappings
                group m by m.TableName
                into g
                let related = (
                    from m in g
                    from i in documentAndIds
                    from relId in (m.Handler.Read(i.document) as IEnumerable<(string id, Type type)>) ?? new (string id, Type type)[0]
                    let relatedTableName = mappings.Resolve(relId.type).TableName
                    select (parentIdVariable: i.idVariable, relatedDocumentId: relId.id, relatedTableName: relatedTableName)
                ).Distinct().ToArray()
                select new RelatedDocumentTableData
                {
                    TableName = g.Key,
                    IdColumnName = g.Select(m => m.IdColumnName).Distinct().Single(),
                    IdTableColumnName = g.Select(m => m.IdTableColumnName).Distinct().Single(),
                    RelatedDocumentIdColumnName = g.Select(m => m.RelatedDocumentIdColumnName).Distinct().Single(),
                    RelatedDocumentTableColumnName = g.Select(m => m.RelatedDocumentTableColumnName).Distinct().Single(),
                    Related = related
                };
            return groupedByTable.ToArray();
        }


        class RelatedDocumentTableData
        {
            public string TableName { get; set; }
            public string IdColumnName { get; set; }
            public string RelatedDocumentIdColumnName { get; set; }
            public (string parentIdVariable, string relatedDocumentId, string relatedTableName)[] Related { get; set; }
            public string IdTableColumnName { get; set; }
            public string RelatedDocumentTableColumnName { get; set; }
        }
    }
    
    internal static class DataModificationQueryBuilderExtensions
    {
        public static IEnumerable<ColumnMapping> WritableIndexedColumns(this DocumentMap doc) =>
            doc.IndexedColumns.Where(c => c.Direction == ColumnDirection.Both || c.Direction == ColumnDirection.ToDatabase);
    }
}