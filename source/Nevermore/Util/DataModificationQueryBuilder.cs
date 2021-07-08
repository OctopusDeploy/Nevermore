using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient.Server;
using Nevermore.Advanced;
using Nevermore.Advanced.Serialization;
using Nevermore.Mapping;
using Nevermore.Querying.AST;
#pragma warning disable 618

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
        readonly IRelationalStoreConfiguration configuration;
        readonly Func<DocumentMap, object> keyAllocator;

        public DataModificationQueryBuilder(IRelationalStoreConfiguration configuration, Func<DocumentMap, object> keyAllocator)
        {
            this.mappings = configuration.DocumentMaps;
            this.serializer = configuration.DocumentSerializer;
            this.configuration = configuration;
            this.keyAllocator = keyAllocator;
        }

        public PreparedCommand PrepareInsert(IReadOnlyList<object> documents, InsertOptions options = null)
        {
            options ??= InsertOptions.Default;
            var mapping = GetMapping(documents);

            if (mapping.IdColumn?.Direction == ColumnDirection.FromDatabase && options.CustomAssignedId != null)
                throw new InvalidOperationException($"{nameof(InsertOptions)}.{nameof(InsertOptions.CustomAssignedId)} is not supported for identity Id columns.");

            var sb = new StringBuilder();
            AppendInsertStatement(sb, mapping, options.TableName, options.SchemaName, options.Hint, documents.Count, options.IncludeDefaultModelColumns);
            var parameters = GetDocumentParameters(m => keyAllocator(m), options.CustomAssignedId, documents, mapping, DataModification.Insert);

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
                case JsonStorageFormat.NoJson:
                    // Nothing to set
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var rowVersionCheckStatement = mapping.IsRowVersioningEnabled
                ? $" AND [{mapping.RowVersionColumn.ColumnName}] = @{mapping.RowVersionColumn.ColumnName}"
                : string.Empty;
            var returnRowVersionStatement = mapping.IsRowVersioningEnabled
                ? $" OUTPUT inserted.{mapping.RowVersionColumn.ColumnName}"
                : string.Empty;

            var updates = string.Join(", ", updateStatements);

            var statement = $"UPDATE [{configuration.GetSchemaNameOrDefault(mapping)}].[{mapping.TableName}] {options.Hint ?? ""} SET {updates}{returnRowVersionStatement} WHERE [{mapping.IdColumn.ColumnName}] = @{mapping.IdColumn.ColumnName}{rowVersionCheckStatement}";

            var parameters = GetDocumentParameters(
                m => throw new Exception("Cannot update a document if it does not have an ID"),
                null,
                null,
                document,
                mapping,
                DataModification.Update
            );

            statement = AppendRelatedDocumentStatementsForUpdate(statement, parameters, mapping, document);
            return new PreparedCommand(statement, parameters, RetriableOperation.Update, mapping, options.CommandTimeout);
        }

        public PreparedCommand PrepareDelete(object document, DeleteOptions options = null)
        {
            var mapping = mappings.Resolve(document.GetType());
            var id = mapping.IdColumn.PropertyHandler.Read(document);
            return PrepareDelete(mapping, id, options);
        }

        public PreparedCommand PrepareDelete<TDocument>(object id, DeleteOptions options = null) where TDocument : class
        {
            var mapping = mappings.Resolve(typeof(TDocument));

            var idType = id.GetType();
            if (mapping.IdColumn.Type != idType)
                throw new ArgumentException($"Provided Id of type '{idType.FullName}' does not match configured type of '{mapping.IdColumn.Type.FullName}'.");

            return PrepareDelete(mapping, id, options);
        }

        private PreparedCommand PrepareDelete(DocumentMap mapping, object id, DeleteOptions options = null)
        {
            options ??= DeleteOptions.Default;

            var actualTableName = options.TableName ?? mapping.TableName;
            var actualSchemaName = options.SchemaName ?? configuration.GetSchemaNameOrDefault(mapping);

            var statement = new StringBuilder();
            statement.AppendLine($"DELETE FROM [{actualSchemaName}].[{actualTableName}] WITH (ROWLOCK) WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}");

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName,
                schema: configuration.GetSchemaNameOrDefault(m), idColumnName: m.IdColumnName)).Distinct())
                statement.AppendLine($"DELETE FROM [{relMap.schema}].[{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] = @{IdVariableName}");

            var parameters = new CommandParameterValues {{IdVariableName, id}};
            return new PreparedCommand(statement.ToString(), parameters, RetriableOperation.Delete, mapping,
                options.CommandTimeout);
        }

        public PreparedCommand PrepareDelete(Type documentType, Where where, CommandParameterValues parameters, DeleteOptions options = null)
        {
            return PrepareDelete(mappings.Resolve(documentType), where, parameters, options);
        }

        public PreparedCommand PrepareDelete(DocumentMap mapping, Where where, CommandParameterValues parameters, DeleteOptions options = null)
        {
            options ??= DeleteOptions.Default;

            var actualTableName = options.TableName ?? mapping.TableName;
            var actualSchemaName = options.SchemaName ?? configuration.GetSchemaNameOrDefault(mapping);

            if (!mapping.RelatedDocumentsMappings.Any())
                return new PreparedCommand($"DELETE FROM [{actualSchemaName}].[{actualTableName}]{options.Hint ?? ""} {where.GenerateSql()}", parameters, RetriableOperation.Delete, mapping, options.CommandTimeout);

            var statement = new StringBuilder();
            statement.AppendLine("DECLARE @Ids as TABLE (Id nvarchar(400))");
            statement.AppendLine();
            statement.AppendLine("INSERT INTO @Ids");
            statement.AppendLine($"SELECT [{mapping.IdColumn.ColumnName}]");
            statement.AppendLine($"FROM [{actualSchemaName}].[{actualTableName}] WITH (ROWLOCK)");
            statement.AppendLine(where.GenerateSql());
            statement.AppendLine();

            statement.AppendLine($"DELETE FROM [{actualSchemaName}].[{actualTableName}] WITH (ROWLOCK) WHERE [{mapping.IdColumn.ColumnName}] in (SELECT Id FROM @Ids)");

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName, schema: configuration.GetSchemaNameOrDefault(m), idColumnName: m.IdColumnName)).Distinct())
                statement.AppendLine($"DELETE FROM [{relMap.schema}].[{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] in (SELECT Id FROM @Ids)");

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
        void AppendInsertStatement(StringBuilder sb, DocumentMap mapping, string tableName, string schemaName, string tableHint, int numberOfInstances, bool includeDefaultModelColumns)
        {
            var columns = new List<string>();

            if (includeDefaultModelColumns && !(mapping.IdColumn is null) && !mapping.IsIdentityId)
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
            var actualSchemaName = schemaName ?? configuration.GetSchemaNameOrDefault(mapping);

            //do we have any
            string outputStatement = null;
            string outputVariable = null;
            string outputSelect = null;
            if (mapping.HasModificationOutputs)
            {
                var outputColumns = new Dictionary<string, string>();

                if (mapping.IsRowVersioningEnabled)
                    outputColumns.Add(mapping.RowVersionColumn.ColumnName, "binary(8)");

                if (mapping.IsIdentityId)
                    outputColumns.Add(mapping.IdColumn.ColumnName, mapping.IdColumn.Type.GetIdentityIdTypeName());

                outputStatement = $"OUTPUT {string.Join(",", outputColumns.Select(kvp => $"inserted.[{kvp.Key}]"))} INTO @InsertedRows";
                outputVariable = $"DECLARE @InsertedRows TABLE ({string.Join(", ", outputColumns.Select(kvp => $"[{kvp.Key}] {kvp.Value}"))})";
                outputSelect = $"SELECT {string.Join(",", outputColumns.Select(kvp => $"[{kvp.Key}]"))} FROM @InsertedRows";
            }

            if (outputVariable != null)
                sb.AppendLine(outputVariable);

            sb.AppendLine($"INSERT INTO [{actualSchemaName}].[{actualTableName}] {tableHint} ({columnNames}) {outputStatement} VALUES ");

            void Append(string prefix)
            {
                var columnVariableNames = string.Join(", ", columns.Select(c => $"@{prefix}{c}"));
                sb.AppendLine($"({columnVariableNames})");
            }

            if (numberOfInstances == 1)
            {
                Append("");
            }
            else
            {
                for (var x = 0; x < numberOfInstances; x++)
                {
                    if (x > 0)
                        sb.Append(",");

                    Append($"{x}__");
                }
            }

            if (outputSelect != null)
                sb.AppendLine(outputSelect);
        }

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, object> allocateId, object customAssignedId, IReadOnlyList<object> documents, DocumentMap mapping, DataModification dataModification)
        {
            if (documents.Count == 1)
                return GetDocumentParameters(allocateId, customAssignedId, CustomIdAssignmentBehavior.ThrowIfIdAlreadySetToDifferentValue, documents[0], mapping, dataModification, "");

            var parameters = new CommandParameterValues();
            for (var x = 0; x < documents.Count; x++)
            {
                var instanceParameters = GetDocumentParameters(allocateId, customAssignedId, CustomIdAssignmentBehavior.IgnoreCustomIdIfIdAlreadySet, documents[x], mapping, dataModification, $"{x}__");
                parameters.AddRange(instanceParameters);
            }

            return parameters;
        }

        enum CustomIdAssignmentBehavior
        {
            ThrowIfIdAlreadySetToDifferentValue,
            IgnoreCustomIdIfIdAlreadySet
        }

        enum DataModification
        {
            Insert,
            Update
        }

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, object> allocateId, object customAssignedId, CustomIdAssignmentBehavior? customIdAssignmentBehavior, object document, DocumentMap mapping, DataModification dataModification, string prefix = null)
        {
            if (mapping.IdColumn is null)
                throw new InvalidOperationException($"Map for {mapping.Type.Name} do not specify an Id column");

            var id = mapping.IdColumn.PropertyHandler.Read(document);

            if (customAssignedId != null && customAssignedId.GetType() != mapping.IdColumn.Type)
                throw new ArgumentException($"The given custom Id '{customAssignedId}' must be of type ({mapping.IdColumn.Type.Name}), to match the model's Id property");

            if (customIdAssignmentBehavior == CustomIdAssignmentBehavior.ThrowIfIdAlreadySetToDifferentValue &&
                customAssignedId != null && id != null && !customAssignedId.Equals(id))
                throw new ArgumentException("Do not pass a different Id when one is already set on the document");

            var result = new CommandParameterValues();

            // we never want to allocate id's if the Id column is an Identity
            if (!mapping.IdColumn.IsIdentity)
            {
                // check whether the object's Id has already been provided, if not then we'll either use the one from the InsertOptions or we'll generate one
                if (id == null)
                {
                    id = customAssignedId == null || (customAssignedId is string assignedId && string.IsNullOrWhiteSpace(assignedId)) ? allocateId(mapping) : customAssignedId;
                    mapping.IdColumn.PropertyHandler.Write(document, id);
                }
            }

            var keyHandler = mapping.IdColumn.PrimaryKeyHandler;
            var primitiveValue = keyHandler.ConvertToPrimitiveValue(id);
            result[$"{prefix}{mapping.IdColumn.ColumnName}"] = primitiveValue;

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
                case JsonStorageFormat.NoJson:
                    // Nothing to serialize
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var c in mapping.WritableIndexedColumns())
            {
                var value = c.PropertyHandler.Read(document);
                if (value != null && value != DBNull.Value && value is string && c.MaxLength != null && c.MaxLength.Value > 0)
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

            if (dataModification == DataModification.Update && mapping.IsRowVersioningEnabled)
            {
                var value = mapping.RowVersionColumn.PropertyHandler.Read(document);
                result[$"{prefix}{mapping.RowVersionColumn.ColumnName}"] = value ?? throw new InvalidDataException($"'{mapping.RowVersionColumn.Property.Name}' property is declared as RowVersion() and can't be set to null. Refresh the data and try again.");
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

            int index = 0;
            foreach (var data in relatedDocumentData.Where(g => g.Related.Length > 0))
            {
                var tableVariableName = $"{index++}__relatedDocumentTableValuedParameter";
                parameters.AddTable(tableVariableName, CreateRelatedDocumentTableValuedParameter(mapping, data));

                sb.AppendLine($"INSERT INTO [{data.SchemaName}].[{data.TableName}] ([{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}])");
                sb.AppendLine($"SELECT [{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}] FROM @{tableVariableName}");
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

            var index = 0;
            foreach (var data in relatedDocumentData)
            {
                if (data.Related.Any())
                {
                    //Feature flag
                    var tableVariableName = $"{index++}__relatedDocumentTableValuedParameter";
                    parameters.AddTable(tableVariableName, CreateRelatedDocumentTableValuedParameter(mapping, data));

                    sb.AppendLine($"DELETE FROM [{data.SchemaName}].[{data.TableName}] WHERE [{data.IdColumnName}] = @{IdVariableName}");
                    sb.AppendLine($"    AND [{data.RelatedDocumentIdColumnName}] not in (SELECT [{data.RelatedDocumentIdColumnName}] FROM @{tableVariableName})");
                    sb.AppendLine();

                    sb.AppendLine($"INSERT INTO [{data.SchemaName}].[{data.TableName}] ([{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}])");
                    sb.AppendLine($"SELECT [{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}] FROM @{tableVariableName} t");
                    sb.AppendLine($"WHERE NOT EXISTS (SELECT null FROM [{data.SchemaName}].[{data.TableName}] r WHERE r.[{data.IdColumnName}] = @{IdVariableName} AND r.[{data.RelatedDocumentIdColumnName}] = t.[{data.RelatedDocumentIdColumnName}] )");
                }
                else
                {
                    sb.AppendLine($"DELETE FROM [{data.SchemaName}].[{data.TableName}] WHERE [{data.IdColumnName}] = @Id");
                }
            }

            return sb.ToString();
        }

        static TableValuedParameter CreateRelatedDocumentTableValuedParameter(DocumentMap mapping,
            RelatedDocumentTableData data)
        {
            var idMetaData = new SqlMetaData(data.IdColumnName, SqlDbType.NVarChar, 400);
            var tableMetadata = new SqlMetaData(data.IdTableColumnName, SqlDbType.NVarChar, 400);
            var relatedIdMetadata = new SqlMetaData(data.RelatedDocumentIdColumnName, SqlDbType.NVarChar, 400);
            var relatedTableMetadata = new SqlMetaData(data.RelatedDocumentTableColumnName, SqlDbType.NVarChar, 400);

            var records = new List<SqlDataRecord>();
            foreach (var related in data.Related)
            {
                var record = new SqlDataRecord(idMetaData, tableMetadata, relatedIdMetadata, relatedTableMetadata);
                record.SetString(0, related.parentId);
                record.SetString(1, mapping.TableName);
                record.SetString(2, related.relatedDocumentId);
                record.SetString(3, related.relatedTableName);
                records.Add(record);
            }

            return new TableValuedParameter("dbo.RelatedDocumentTableValuedParameter", records);
        }

        IReadOnlyList<RelatedDocumentTableData> GetRelatedDocumentTableData(DocumentMap mapping, IReadOnlyList<object> documents)
        {
            var documentAndIds = documents.Select(i => (id: (string)mapping.IdColumn.PropertyHandler.Read(i), document: i));

            var groupedByTable = from m in mapping.RelatedDocumentsMappings
                group m by new {Table = m.TableName, Schema = configuration.GetSchemaNameOrDefault(m)}
                into g
                let related = (
                    from m in g
                    from i in documentAndIds
                    from relId in (m.Handler.Read(i.document) as IEnumerable<(string id, Type type)>) ?? new (string id, Type type)[0]
                    let relatedTableName = mappings.Resolve(relId.type).TableName
                    select (parentId: i.id, relatedDocumentId: relId.id, relatedTableName)
                ).Distinct().ToArray()
                select new RelatedDocumentTableData
                {
                    TableName = g.Key.Table,
                    SchemaName = g.Key.Schema,
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
            public string SchemaName { get; set; }
            public string IdColumnName { get; set; }
            public string RelatedDocumentIdColumnName { get; set; }
            public (string parentId, string relatedDocumentId, string relatedTableName)[] Related { get; set; }
            public string IdTableColumnName { get; set; }
            public string RelatedDocumentTableColumnName { get; set; }
        }
    }

    internal static class DataModificationQueryBuilderExtensions
    {
        static readonly Dictionary<Type, string> IdentityIdTypeMap = new Dictionary<Type, string>
        {
            [typeof(short)] = "smallint",
            [typeof(int)] = "int",
            [typeof(long)] = "bigint"
        };

        public static IEnumerable<ColumnMapping> WritableIndexedColumns(this DocumentMap doc) =>
            doc.Columns.Where(c => c.Direction == ColumnDirection.Both || c.Direction == ColumnDirection.ToDatabase);

        public static string GetIdentityIdTypeName(this Type type)
        {
            return IdentityIdTypeMap[type];
        }
    }
}