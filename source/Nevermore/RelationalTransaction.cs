using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Data.Common;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Nevermore.Transient;
using Newtonsoft.Json;

namespace Nevermore
{
    [DebuggerDisplay("{ToString()}")]
    public class RelationalTransaction : ReadRelationalTransaction, IRelationalTransaction
    {
        readonly IKeyAllocator keyAllocator;
        
        public RelationalTransaction(
            RelationalTransactionRegistry registry,
            RetriableOperation retriableOperation,
            ISqlCommandFactory sqlCommandFactory,
            JsonSerializerSettings jsonSerializerSettings,
            RelationalMappings mappings,
            IKeyAllocator keyAllocator,
            IRelatedDocumentStore relatedDocumentStore,
            string name = null,
            ObjectInitialisationOptions objectInitialisationOptions = ObjectInitialisationOptions.None
        ) : base(registry, retriableOperation, sqlCommandFactory, jsonSerializerSettings, mappings, relatedDocumentStore, name, objectInitialisationOptions)
        {
            this.keyAllocator = keyAllocator;
        }

        public IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class, IId
        {
            return new DeleteQueryBuilder<TDocument>(
                uniqueParameterNameGenerator,
                (documentType, where, parameterValues, commandTimeout) => DeleteInternal(
                    dataModificationQueryBuilder.CreateDelete(documentType, where),
                    parameterValues, 
                    commandTimeout
                ), 
                Enumerable.Empty<IWhereClause>(), 
                new CommandParameterValues()
            );
        }

        public void Insert<TDocument>(TDocument instance, TimeSpan? commandTimeout = null)
            where TDocument : class, IId
        {
            Insert(null, instance, null, commandTimeout: commandTimeout);
        }

        public Task InsertAsync<TDocument>(TDocument instance, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            return InsertAsync(null, instance, null, commandTimeout: commandTimeout);
        }

        public void Insert<TDocument>(string tableName, TDocument instance, TimeSpan? commandTimeout = null)
            where TDocument : class, IId
        {
            Insert(tableName, instance, null, commandTimeout: commandTimeout);
        }

        public void Insert<TDocument>(TDocument instance, string customAssignedId, TimeSpan? commandTimeout = null)
            where TDocument : class, IId
        {
            Insert(null, instance, customAssignedId, commandTimeout: commandTimeout);
        }

        public Task InsertAsync<TDocument>(TDocument instance, string customAssignedId, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            return InsertAsync(null, instance, customAssignedId, commandTimeout: commandTimeout);
        }

        public void InsertWithHint<TDocument>(TDocument instance, string tableHint, TimeSpan? commandTimeout = null)
            where TDocument : class, IId
        {
            Insert(null, instance, null, tableHint, commandTimeout);
        }

        public void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId, string tableHint = null, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            if (customAssignedId != null && instance.Id != null && customAssignedId != instance.Id)
                throw new ArgumentException("Do not pass a different Id when one is already set on the document");
            
            var (mapping, statement, parameters) = dataModificationQueryBuilder.CreateInsert(
                new[] {instance}, 
                tableName, 
                tableHint,
                m => string.IsNullOrEmpty(customAssignedId) ? AllocateId(m) : customAssignedId,
                true
             );
            
            using (new TimedSection(Log, ms => $"Insert took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping, commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Insert));

                    // Copy the assigned Id back onto the document
                    mapping.IdColumn.ReaderWriter.Write(instance, (string) parameters["Id"]);

                    relatedDocumentStore.PopulateRelatedDocuments(this, instance);
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }
        
        public async Task InsertAsync<TDocument>(string tableName, TDocument instance, string customAssignedId, string tableHint = null, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            if (customAssignedId != null && instance.Id != null && customAssignedId != instance.Id)
                throw new ArgumentException("Do not pass a different Id when one is already set on the document");
            
            var (mapping, statement, parameters) = dataModificationQueryBuilder.CreateInsert(
                new[] {instance}, 
                tableName, 
                tableHint,
                m => string.IsNullOrEmpty(customAssignedId) ? AllocateId(m) : customAssignedId,
                true
            );
            
            using (new TimedSection(Log, ms => $"Insert took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping, commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    await command.ExecuteNonQueryWithRetryAsync(GetRetryPolicy(RetriableOperation.Insert));

                    // Copy the assigned Id back onto the document
                    mapping.IdColumn.ReaderWriter.Write(instance, (string) parameters["Id"]);

                    relatedDocumentStore.PopulateRelatedDocuments(this, instance);
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        public void InsertMany<TDocument>(string tableName, IReadOnlyCollection<TDocument> instances,
            bool includeDefaultModelColumns = true, string tableHint = null, TimeSpan? commandTimeout = null)
            where TDocument : class, IId
        {
            if (!instances.Any())
                return;

            IReadOnlyList<IId> instanceList = instances.ToArray();
            var (mapping, statement, parameters) = dataModificationQueryBuilder.CreateInsert(
                instanceList, 
                tableName, 
                tableHint,
                AllocateId,
                includeDefaultModelColumns);

            using (new TimedSection(Log, ms => $"Insert took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping, commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Insert));
                    for(var x = 0; x < instanceList.Count; x++)
                    {
                        var idVariableName = instanceList.Count == 1 ? "Id" : $"{x}__Id";

                        // Copy the assigned Id back onto the document
                        mapping.IdColumn.ReaderWriter.Write(instanceList[x], (string) parameters[idVariableName]);

                        relatedDocumentStore.PopulateRelatedDocuments(this, instanceList[x]);
                    }
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        public string AllocateId(Type documentType)
        {
            var mapping = mappings.Get(documentType);
            return AllocateId(mapping);
        }

        string AllocateId(DocumentMap mapping)
        {
            if (!string.IsNullOrEmpty(mapping.SingletonId))
                return mapping.SingletonId;

            return AllocateId(mapping.TableName, mapping.IdPrefix);
        }

        public string AllocateId(string tableName, string idPrefix)
        {
            var key = keyAllocator.NextId(tableName);
            return $"{idPrefix}-{key}";
        }

        public void Update<TDocument>(TDocument instance, string tableHint = null, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            var (mapping, statement, parameters) = dataModificationQueryBuilder.CreateUpdate(instance, tableHint);
            
            using (new TimedSection(Log, ms => $"Update took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping, commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    // Explicitly no retries on mutating database operations
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Update));
                    relatedDocumentStore.PopulateRelatedDocuments(this, instance);
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        // Delete does not require TDocument to implement IId because during recursive document delete we have only objects
        public void Delete<TDocument>(TDocument instance, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            var (statement, parameterValues) = dataModificationQueryBuilder.CreateDelete(instance);
            DeleteInternal(statement, parameterValues, commandTimeout);
        }

        public void DeleteById<TDocument>(string id, TimeSpan? commandTimeout = null) where TDocument : class, IId
        {
            var (statement, parameterValues) = dataModificationQueryBuilder.CreateDelete<TDocument>(id);
            DeleteInternal(statement, parameterValues, commandTimeout);
        }

        void DeleteInternal(string statement, CommandParameterValues parameterValues, TimeSpan? commandTimeout = null)
        {
            using (new TimedSection(Log, ms => $"Delete took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameterValues, commandTimeout: commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    // We can retry deletes because deleting something that doesn't exist will silently do nothing
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Delete), "ExecuteDeleteQuery " + statement);
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }
        
        public void Commit()
        {
            transaction.Commit();
        }
    }
}
