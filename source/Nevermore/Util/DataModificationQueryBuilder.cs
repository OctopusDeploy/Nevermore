using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
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

        // The error from SQL Server says '2100' but because statements are converted to `exec sp_executesql` calls, 2 of these 2100 params are already taken
        // https://stackoverflow.com/a/8050474/2631967
        const int SqlServerParameterLimit = 2098;

        readonly IDocumentMapRegistry mappings;
        readonly IDocumentSerializer serializer;
        readonly IRelationalStoreConfiguration configuration;
        readonly Func<DocumentMap, string> keyAllocator;

        public DataModificationQueryBuilder(IRelationalStoreConfiguration configuration, Func<DocumentMap, string> keyAllocator)
        {
            this.mappings = configuration.DocumentMaps;
            this.serializer = configuration.DocumentSerializer;
            this.configuration = configuration;
            this.keyAllocator = keyAllocator;
        }

        public PreparedCommand[] PrepareInsert(IReadOnlyList<object> documents, InsertOptions options = null)
        {
            options ??= InsertOptions.Default;

            var mapping = GetMapping(documents);

            var insertStatementBuilder = CreateInsertStatementBuilder(documents, mapping, options);
            var insertCommands = CreateInsertCommandsWithRelatedDocuments(insertStatementBuilder, mapping, documents, options);

            return insertCommands.ToArray();
        }

        public PreparedCommand[] PrepareUpdate(object document, UpdateOptions options = null)
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

            var rowVersionCheckStatement = mapping.IsRowVersioningEnabled ? $" AND [{mapping.RowVersionColumn.ColumnName}] = @{mapping.RowVersionColumn.ColumnName}" : string.Empty;
            var returnRowVersionStatement = mapping.IsRowVersioningEnabled ? $" OUTPUT inserted.{mapping.RowVersionColumn.ColumnName}" : string.Empty;

            var updates = string.Join(", ", updateStatements);

            var statement = $"UPDATE [{configuration.GetSchemaNameOrDefault(mapping)}].[{mapping.TableName}] {options.Hint ?? ""} SET {updates}{returnRowVersionStatement} WHERE [{mapping.IdColumn.ColumnName}] = @{mapping.IdColumn.ColumnName}{rowVersionCheckStatement}";

            return CreateUpdateCommandsWithRelatedDocuments(statement, mapping, document, options);
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

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName, schema: configuration.GetSchemaNameOrDefault(m), idColumnName: m.IdColumnName)).Distinct())
                statement.AppendLine($"DELETE FROM [{relMap.schema}].[{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] = @{IdVariableName}");

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

            var actualTableName = options.TableName ?? mapping.TableName;
            var actualSchemaName = options.SchemaName ?? configuration.GetSchemaNameOrDefault(mapping);

            if (!mapping.RelatedDocumentsMappings.Any())
                return new PreparedCommand($"DELETE FROM [{actualSchemaName}].[{actualTableName}]{options.Hint??""} {where.GenerateSql()}", parameters, RetriableOperation.Delete, mapping, options.CommandTimeout);

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
        StringBuilder CreateInsertStatementBuilder(
            IReadOnlyList<object> documents,
            DocumentMap mapping,
            InsertOptions options)
        {
            var columns = new List<string>();

            if (options.IncludeDefaultModelColumns)
                columns.Add(mapping.IdColumn.ColumnName);

            columns.AddRange(mapping.WritableIndexedColumns().Select(c => c.ColumnName));

            if (options.IncludeDefaultModelColumns)
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

            var actualTableName = options.TableName ?? mapping.TableName;
            var actualSchemaName = options.SchemaName ?? configuration.GetSchemaNameOrDefault(mapping);

            var returnRowVersionStatement = mapping.IsRowVersioningEnabled ? $" OUTPUT inserted.{mapping.RowVersionColumn.ColumnName}" : string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO [{actualSchemaName}].[{actualTableName}] {options.Hint} ({columnNames}){returnRowVersionStatement} VALUES ");

            void Append(string prefix)
            {
                var columnVariableNames = string.Join(", ", columns.Select(c => $"@{prefix}{c}"));
                sb.AppendLine($"({columnVariableNames})");
            }

            if (documents.Count == 1)
            {
                Append("");
                return sb;
            }

            for (var x = 0; x < documents.Count; x++)
            {
                if (x > 0)
                    sb.Append(",");

                Append($"{x}__");
            }

            return sb;
        }

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, object customAssignedId, IReadOnlyList<object> documents, DocumentMap mapping, DataModification dataModification)
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

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, object customAssignedId, CustomIdAssignmentBehavior? customIdAssignmentBehavior, object document, DocumentMap mapping, DataModification dataModification, string prefix = null)
        {
            var id = mapping.IdColumn.PropertyHandler.Read(document);

            if (customIdAssignmentBehavior == CustomIdAssignmentBehavior.ThrowIfIdAlreadySetToDifferentValue &&
                customAssignedId != null && id != null && customAssignedId != id)
                throw new ArgumentException("Do not pass a different Id when one is already set on the document");

            if (mapping.IdColumn.Type == typeof(string) && string.IsNullOrWhiteSpace((string)id))
            {
                id = string.IsNullOrWhiteSpace(customAssignedId as string) ? allocateId(mapping) : customAssignedId;
                mapping.IdColumn.PropertyHandler.Write(document, id);
            }

            var result = new CommandParameterValues
            {
                [$"{prefix}{mapping.IdColumn.ColumnName}"] = id
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

        PreparedCommand[] CreateInsertCommandsWithRelatedDocuments(
            StringBuilder insertStatementBuilder,
            DocumentMap mapping,
            IReadOnlyList<object> documents,
            InsertOptions options)
        {
            var commands = new List<PreparedCommand>();
            var parameters = new CommandParameterValues();
            // we need to create these document parameters for each command - parameters are disposed at the end of execution in 'CommandExecutor'
            Func<CommandParameterValues> documentParametersCreator = () => GetDocumentParameters(m => keyAllocator(m), options.CustomAssignedId, documents, mapping, DataModification.Insert);
            parameters.AddRange(documentParametersCreator());

            var relatedDocumentData = GetRelatedDocumentTableData(mapping, documents);
            if (!relatedDocumentData.Any(x => x.Related.Any()))
            {
                commands.Add(new PreparedCommand(insertStatementBuilder.ToString(), parameters, RetriableOperation.Insert, mapping, options.CommandTimeout));
                return commands.ToArray();
            }

            foreach (var data in relatedDocumentData.Where(g => g.Related.Length > 0))
            {
                var relatedVariablePrefix = $"{data.TableName.ToLower()}_";

                var remaining = data.Related.Length;
                while (remaining > 0)
                {
                    var reachedParameterLimit = false;

                    while (!reachedParameterLimit && remaining > 0)
                    {
                        insertStatementBuilder.AppendLine(
                            $"INSERT INTO [{data.SchemaName}].[{data.TableName}] ([{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}]) VALUES");
                        var related = data.Related;
                        var batchSize = Math.Min(1000, Math.Min(SqlServerParameterLimit - parameters.Count, remaining));

                        for (var x = 0; x < batchSize; x++)
                        {
                            int index = data.Related.Length - remaining;
                            var parentIdVariable = related[index].parentIdVariable;
                            var relatedDocumentId = related[index].relatedDocumentId;
                            var relatedTableName = related[index].relatedTableName;

                            var relatedVariableName = relatedVariablePrefix + index;
                            parameters.Add(relatedVariableName, relatedDocumentId);
                            if (x > 0)
                                insertStatementBuilder.Append(",");
                            insertStatementBuilder.AppendLine(
                                $"(@{parentIdVariable}, '{mapping.TableName}', @{relatedVariableName}, '{relatedTableName}')");
                            remaining--;
                        }

                        if (parameters.Count >= SqlServerParameterLimit && remaining > 0)
                        {
                            commands.Add(new PreparedCommand(insertStatementBuilder.ToString(), parameters, RetriableOperation.Insert, mapping, options.CommandTimeout));
                            insertStatementBuilder = new StringBuilder();
                            parameters = new CommandParameterValues();
                            parameters.AddRange(documentParametersCreator());
                        }
                    }
                }
            }

            commands.Add(new PreparedCommand(insertStatementBuilder.ToString(), parameters, RetriableOperation.Insert, mapping, options.CommandTimeout));
            return commands.ToArray();
        }

        PreparedCommand[] CreateUpdateCommandsWithRelatedDocuments(
            string updateStatement,
            DocumentMap mapping,
            object document,
            UpdateOptions updateOptions)
        {
            Func<CommandParameterValues> parametersCreator = () => GetDocumentParameters(
                m => throw new Exception("Cannot update a document if it does not have an ID"),
                null,
                null,
                document,
                mapping,
                DataModification.Update
            );
            var commands = new List<PreparedCommand>();
            var relatedDocumentData = GetRelatedDocumentTableData(mapping, new[] {document});
            var parameters = parametersCreator();

            if (relatedDocumentData.Count == 0)
            {
                commands.Add(new PreparedCommand(updateStatement, parameters, RetriableOperation.Update, mapping, updateOptions.CommandTimeout));
                return commands.ToArray();
            }

            var sb = new StringBuilder();
            sb.AppendLine(updateStatement);
            sb.AppendLine();

            string tableVariableName = "@references";
            if (parameters.Count + relatedDocumentData.Sum(x => x.Related.Length) > SqlServerParameterLimit) // we need multiple sql commands
            {
                // sql commands are executed inside sp_executesql which means table variables and local temp tables won't live between commands
                tableVariableName = $"##{configuration.RelatedDocumentsGlobalTempTableNameGenerator()}";
                sb.AppendLine($"CREATE TABLE {tableVariableName} (Reference nvarchar(400) COLLATE SQL_Latin1_General_CP1_CS_AS, ReferenceTable nvarchar(400) COLLATE SQL_Latin1_General_CP1_CS_AS)").AppendLine();
            }
            else if (relatedDocumentData.Any(x => x.Related.Any()))
            {
                sb.AppendLine($"DECLARE {tableVariableName} as TABLE (Reference nvarchar(400), ReferenceTable nvarchar(400))");
            }

            foreach (var data in relatedDocumentData)
            {
                if (data.Related.Any())
                {
                    var relatedVariablePrefix = $"{data.TableName.ToLower()}_";
                    var remaining = data.Related.Length;
                    var allValueBlocks = data.Related.Select((r, idx) => $"(@{relatedVariablePrefix}{idx}, '{r.relatedTableName}')").ToArray();

                    while (remaining > 0)
                    {
                        var batchSize = Math.Min(1000, Math.Min(SqlServerParameterLimit - parameters.Count, remaining));
                        var valueBlocks = allValueBlocks.Skip(data.Related.Length - remaining).Take(batchSize);

                        sb.Append($"INSERT INTO {tableVariableName} VALUES ");
                        sb.AppendLine(string.Join(", ", valueBlocks));
                        sb.AppendLine();
                        remaining -= batchSize;

                        for (int i = 0; i < batchSize; i++)
                        {
                            var paramIndex = i + data.Related.Length - remaining - batchSize;
                            parameters.Add(relatedVariablePrefix + paramIndex, data.Related[paramIndex].relatedDocumentId);
                        }

                        if (parameters.Count >= SqlServerParameterLimit && remaining > 0)
                        {
                            commands.Add(new PreparedCommand(sb.ToString(), parameters, RetriableOperation.Update, mapping, updateOptions.CommandTimeout));
                            sb = new StringBuilder();
                            parameters = parametersCreator();
                        }
                    }

                    sb.AppendLine($"DELETE FROM [{data.SchemaName}].[{data.TableName}] WHERE [{data.IdColumnName}] = @{IdVariableName}");
                    sb.AppendLine($"AND [{data.RelatedDocumentIdColumnName}] not in (SELECT Reference FROM {tableVariableName})");
                    sb.AppendLine();

                    sb.AppendLine($"INSERT INTO [{data.SchemaName}].[{data.TableName}] ([{data.IdColumnName}], [{data.IdTableColumnName}], [{data.RelatedDocumentIdColumnName}], [{data.RelatedDocumentTableColumnName}])");
                    sb.AppendLine($"SELECT @{IdVariableName}, '{mapping.TableName}', Reference, ReferenceTable FROM {tableVariableName} t");
                    sb.AppendLine($"WHERE NOT EXISTS (SELECT null FROM [{data.SchemaName}].[{data.TableName}] r WHERE r.[{data.IdColumnName}] = @{IdVariableName} AND r.[{data.RelatedDocumentIdColumnName}] = t.Reference )");
                }
                else
                {
                    sb.AppendLine($"DELETE FROM [{data.SchemaName}].[{data.TableName}] WHERE [{data.IdColumnName}] = @Id");
                }
            }

            commands.Add(new PreparedCommand(sb.ToString(), parameters, RetriableOperation.Update, mapping, updateOptions.CommandTimeout));
            return commands.ToArray();
        }

        IReadOnlyList<RelatedDocumentTableData> GetRelatedDocumentTableData(DocumentMap mapping, IReadOnlyList<object> documents)
        {
            var documentAndIds = documents.Count == 1
                ? new[] {(parentIdVariable: IdVariableName, document: documents[0])}
                : documents.Select((i, idx) => (idVariable: $"{idx}__{IdVariableName}", document: i));

            var groupedByTable = from m in mapping.RelatedDocumentsMappings
                group m by new { Table = m.TableName, Schema = configuration.GetSchemaNameOrDefault(m) }
                into g
                let related = (
                    from m in g
                    from i in documentAndIds
                    from relId in (m.Handler.Read(i.document) as IEnumerable<(string id, Type type)>) ?? new (string id, Type type)[0]
                    let relatedTableName = mappings.Resolve(relId.type).TableName
                    select (parentIdVariable: i.idVariable, relatedDocumentId: relId.id, relatedTableName)
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
            public (string parentIdVariable, string relatedDocumentId, string relatedTableName)[] Related { get; set; }
            public string IdTableColumnName { get; set; }
            public string RelatedDocumentTableColumnName { get; set; }
        }
    }

    internal static class DataModificationQueryBuilderExtensions
    {
        public static IEnumerable<ColumnMapping> WritableIndexedColumns(this DocumentMap doc) =>
            doc.Columns.Where(c => c.Direction == ColumnDirection.Both || c.Direction == ColumnDirection.ToDatabase);
    }
}