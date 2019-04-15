using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
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

        public (DocumentMap mapping, string statement, CommandParameterValues parameterValues) CreateInsert(
            IReadOnlyList<IId> documents,
            string tableName,
            string tableHint,
            Func<DocumentMap, string> allocateId,
            bool includeDefaultModelColumns)
        {
            var mapping = GetMapping(documents);

            var sb = new StringBuilder();
            AppendInsertStatement(sb, mapping, tableName, tableHint, documents.Count, includeDefaultModelColumns);
            var parameters = GetDocumentParameters(allocateId, documents, mapping);

            AppendRelatedDocumentStatementsForInsert(sb, parameters, mapping, documents);
            return (mapping, sb.ToString(), parameters);
        }

        public (DocumentMap, string, CommandParameterValues) CreateUpdate(IId document, string tableHint)
        {
            var mapping = mappings.Get(document.GetType());

            var updates = string.Join(", ", mapping.WritableIndexedColumns()
                .Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName).Union(new[] {$"[JSON] = @{JsonVariableName}"}));
            var statement = $"UPDATE dbo.[{mapping.TableName}] {tableHint ?? ""} SET {updates} WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}";

            var parameters = GetDocumentParameters(
                m => throw new Exception("Cannot update a document if it does not have an ID"),
                document,
                mapping,
                null
            );

            statement = AppendRelatedDocumentStatementsForUpdate(statement, parameters, mapping, document);

            return (mapping, statement, parameters);
        }

        public (string statement, CommandParameterValues parameterValues) CreateDelete(IId document)
        {
            var mapping = mappings.Get(document.GetType());
            var id = (string) mapping.IdColumn.ReaderWriter.Read(document);
            return CreateDeleteById(mapping, id);
        }

        public (string statement, CommandParameterValues parameterValues) CreateDelete<TDocument>(string id)
            where TDocument : class, IId
        {
            var mapping = mappings.Get(typeof(TDocument));
            return CreateDeleteById(mapping, id);
        }

        static (string statement, CommandParameterValues parameterValues) CreateDeleteById(DocumentMap mapping, string id)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"DELETE FROM [{mapping.TableName}] WITH (ROWLOCK) WHERE [{mapping.IdColumn.ColumnName}] = @{IdVariableName}");

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName, idColumnName: m.IdColumnName)).Distinct())
                sb.AppendLine($"DELETE FROM [{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] = @{IdVariableName}");

            var parameters = new CommandParameterValues {{IdVariableName, id}};
            return (sb.ToString(), parameters);
        }

        public string CreateDelete(Type documentType, Where where)
            => CreateDelete(mappings.Get(documentType), where.GenerateSql());

        string CreateDelete(DocumentMap mapping, string whereClause)
        {
            if (!mapping.RelatedDocumentsMappings.Any())
                return $"DELETE FROM [{mapping.TableName}] {whereClause}";
            ;

            var sb = new StringBuilder();
            sb.AppendLine("DECLARE @Ids as TABLE (Id nvarchar(400))");
            sb.AppendLine();
            sb.AppendLine("INSERT INTO @Ids");
            sb.AppendLine($"SELECT [{mapping.IdColumn.ColumnName}]");
            sb.AppendLine($"FROM [{mapping.TableName}] WITH (ROWLOCK)");
            sb.AppendLine(whereClause);
            sb.AppendLine();

            sb.AppendLine($"DELETE FROM [{mapping.TableName}] WITH (ROWLOCK) WHERE [{mapping.IdColumn.ColumnName}] in (SELECT Id FROM @Ids)");

            foreach (var relMap in mapping.RelatedDocumentsMappings.Select(m => (tableName: m.TableName, idColumnName: m.IdColumnName)).Distinct())
                sb.AppendLine($"DELETE FROM [{relMap.tableName}] WITH (ROWLOCK) WHERE [{relMap.idColumnName}] in (SELECT Id FROM @Ids)");

            return sb.ToString();
        }

        DocumentMap GetMapping(IReadOnlyList<IId> documents)
        {
            var allMappings = documents.Select(i => this.mappings.Get(i)).Distinct().ToArray();
            if (allMappings.Length == 0)
                throw new Exception($"No mapping found for type {documents[0].GetType()}");

            if (allMappings.Length != 1)
                throw new Exception("InsertMany cannot be used with documents that have different mappings");
            return allMappings[0];
        }

        void AppendInsertStatement(StringBuilder sb, DocumentMap mapping, string tableName, string tableHint, int numberOfInstances, bool includeDefaultModelColumns)
        {
            var columns = mapping.WritableIndexedColumns().Select(c => c.ColumnName).ToList();
            if (includeDefaultModelColumns)
                columns = columns.Union(new[] {mapping.IdColumn.ColumnName, JsonVariableName}).ToList();
            var columnNames = string.Join(", ", columns);

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


        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, IReadOnlyList<IId> documents, DocumentMap mapping)
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

        CommandParameterValues GetDocumentParameters(Func<DocumentMap, string> allocateId, IId document, DocumentMap mapping, string prefix = null)
        {
            var id = (string) mapping.IdColumn.ReaderWriter.Read(document);
            if (string.IsNullOrWhiteSpace(id))
                id = allocateId(mapping);

            var result = new CommandParameterValues
            {
                [$"{prefix}{IdVariableName}"] = id
            };

            result[$"{prefix}{JsonVariableName}"] = JsonConvert.SerializeObject(document, mapping.Type, jsonSerializerSettings);

            foreach (var c in mapping.WritableIndexedColumns())
            {
                var value = c.ReaderWriter.Read(document);
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
            IReadOnlyList<IId> documents)
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
            IId document)
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

        IReadOnlyList<RelatedDocumentTableData> GetRelatedDocumentTableData(DocumentMap mapping, IReadOnlyList<IId> documents)
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
                    from relId in m.ReaderWriter.Read(i.document) ?? new (string id, Type type)[0]
                    let relatedTableName = mappings.Get(relId.type).TableName
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
            doc.IndexedColumns.Where(c => !c.IsReadOnly);
    }
}