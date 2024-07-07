#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced.Concurrency;
using Nevermore.Advanced.Queryable;
using Nevermore.Advanced.QueryBuilders;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Querying.AST;
using Nevermore.TableColumnNameResolvers;
using Nevermore.Transient;
using Nito.AsyncEx;
using Nito.Disposables;
using NotSupportedException = System.NotSupportedException;

namespace Nevermore.Advanced
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class ReadTransaction : IReadTransaction, ITransactionDiagnostic
    {
        static readonly ILog Log = LogProvider.For<ReadTransaction>();
        protected static (DbConnection connection, bool ownsConnection) DefaultConnectionFactory(string connectionString) => (new SqlConnection(connectionString), true);

        readonly IRelationalTransactionRegistry registry;
        readonly RetriableOperation operationsToRetry;
        readonly IRelationalStoreConfiguration configuration;
        readonly ITableAliasGenerator tableAliasGenerator = new TableAliasGenerator();

        readonly Func<string, (DbConnection connection, bool ownsConnection)> connectionFactory;
        readonly Action<string>? customCommandTrace;
        readonly string name;

        protected bool OwnsSqlTransaction { get; } = true;

        DbConnection? connection;

        protected IUniqueParameterNameGenerator ParameterNameGenerator { get; } = new UniqueParameterNameGenerator();
        protected ITransactionConcurrencyHandler TransactionConcurrencyHandler { get; }

        // To help track deadlocks
        readonly List<string> commandTrace;
        readonly ITableColumnNameResolver columnNameResolver;

        public DateTimeOffset CreatedTime { get; } = DateTimeOffset.Now;

        public IDictionary<string, object> State { get; }

        public ReadTransaction(
            IRelationalStore store,
            IRelationalTransactionRegistry registry,
            RetriableOperation operationsToRetry,
            IRelationalStoreConfiguration configuration,
            Action<string>? customCommandTrace = null,
            string? name = null)
            : this(
                store,
                registry,
                operationsToRetry,
                configuration,
                DefaultConnectionFactory,
                customCommandTrace,
                name)
        {
        }

        public ReadTransaction(
            IRelationalStore store,
            IRelationalTransactionRegistry registry,
            RetriableOperation operationsToRetry,
            IRelationalStoreConfiguration configuration,
            DbConnection existingConnection,
            DbTransaction existingTransaction,
            Action<string>? customCommandTrace = null,
            string? name = null)
            : this(store, registry, operationsToRetry, configuration, DefaultConnectionFactory, customCommandTrace, name)
        {
            connection = existingConnection;
            Transaction = existingTransaction;
            OwnsSqlTransaction = false;
        }

        public ReadTransaction(
            IRelationalStore store,
            IRelationalTransactionRegistry registry,
            RetriableOperation operationsToRetry,
            IRelationalStoreConfiguration configuration,
            Func<string, (DbConnection connection, bool ownsConnection)> connectionFactory,
            Action<string>? customCommandTrace = null,
            string? name = null)
        {
            State = new Dictionary<string, object>();
            this.connectionFactory = connectionFactory;
            this.registry = registry;
            this.operationsToRetry = operationsToRetry;
            this.configuration = configuration;
            this.customCommandTrace = customCommandTrace;
            commandTrace = new List<string>();

            var transactionName = name ?? Thread.CurrentThread.Name;

            // TODO: Transaction name should be mandatory.
            if (string.IsNullOrWhiteSpace(transactionName)) transactionName = "<unknown>";
            this.name = transactionName;

            registry.Add(this);

            columnNameResolver = configuration.TableColumnNameResolver(store);
            TransactionConcurrencyHandler = configuration.ConcurrencyMode switch
            {
                ConcurrencyMode.NoLock => new NoOpConcurrencyHandler(),
                ConcurrencyMode.LockOnly => new LockOnlyConcurrencyHandler(),
                ConcurrencyMode.LogOnly => new LogOnlyConcurrencyHandler(),
                ConcurrencyMode.LockWithLogging => new LockWithLoggingConcurrencyHandler(),
                _ => throw new NotSupportedException($"Concurrency mode {this.configuration.ConcurrencyMode} not supported")
            };
        }

        protected DbTransaction? Transaction { get; private set; }

        TimedSection? TransactionTimer { get; set; }

        public void Open()
        {
            if (!configuration.AllowSynchronousOperations)
                throw new SynchronousOperationsDisabledException();

            if (!OwnsSqlTransaction)
                throw new InvalidOperationException("An existing connection and transaction were provided, they should have been opened externally");

            var (connectionFactoryConnection, ownsConnection) = connectionFactory(configuration.ConnectionString);
            connection = connectionFactoryConnection;

            if (ownsConnection)
            {
                connection.OpenWithRetry();
            }

            TransactionTimer = new TimedSection(ms => configuration.TransactionLogger.Write(ms, name));
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (!OwnsSqlTransaction)
                throw new InvalidOperationException("An existing connection and transaction were provided, they should have been opened externally");

            var (connectionFactoryConnection, ownsConnection) = connectionFactory(configuration.ConnectionString);
            connection = connectionFactoryConnection;

            if (ownsConnection)
            {
                await connection.OpenWithRetryAsync(cancellationToken).ConfigureAwait(false);
            }

            TransactionTimer = new TimedSection(ms => configuration.TransactionLogger.Write(ms, name));
        }

        public void Open(IsolationLevel isolationLevel)
        {
            Open();
            Transaction = BeginTransactionWithRetry(isolationLevel, SqlServerTransactionName, RetryManager.Instance.GetDefaultSqlTransactionRetryPolicy());
        }

        public async Task OpenAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            await OpenAsync(cancellationToken).ConfigureAwait(false);
            Transaction = await BeginTransactionWithRetryAsync(isolationLevel, SqlServerTransactionName, RetryManager.Instance.GetDefaultSqlTransactionRetryPolicy(), cancellationToken).ConfigureAwait(false);
        }

        [Pure]
        public TDocument? Load<TDocument, TKey>(TKey id) where TDocument : class
        {
            return Stream<TDocument>(PrepareLoad<TDocument, TKey>(id)).FirstOrDefault();
        }

        [Pure]
        public TDocument? Load<TDocument>(string id) where TDocument : class => Load<TDocument, string>(id);

        [Pure]
        public TDocument? Load<TDocument>(int id) where TDocument : class => Load<TDocument, int>(id);

        [Pure]
        public TDocument? Load<TDocument>(long id) where TDocument : class => Load<TDocument, long>(id);

        [Pure]
        public TDocument? Load<TDocument>(Guid id) where TDocument : class => Load<TDocument, Guid>(id);

        [Pure]
        public async Task<TDocument?> LoadAsync<TDocument, TKey>(TKey id, CancellationToken cancellationToken = default) where TDocument : class
        {
            var results = StreamAsync<TDocument>(PrepareLoad<TDocument, TKey>(id), cancellationToken);
            await foreach (var row in results.WithCancellation(cancellationToken).ConfigureAwait(false))
                return row;
            return null;
        }

        [Pure]
        public Task<TDocument?> LoadAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, string>(id, cancellationToken);

        [Pure]
        public Task<TDocument?> LoadAsync<TDocument>(int id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, int>(id, cancellationToken);

        [Pure]
        public Task<TDocument?> LoadAsync<TDocument>(long id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, long>(id, cancellationToken);

        [Pure]
        public Task<TDocument?> LoadAsync<TDocument>(Guid id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, Guid>(id, cancellationToken);

        [Pure]
        public List<TDocument> LoadMany<TDocument, TKey>(params TKey[] ids) where TDocument : class
            => LoadStream<TDocument, TKey>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument, TKey>(IEnumerable<TKey> ids) where TDocument : class
            => LoadStream<TDocument, TKey>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(params string[] ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(params int[] ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(params long[] ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(params Guid[] ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(IEnumerable<string> ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(IEnumerable<int> ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(IEnumerable<long> ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public List<TDocument> LoadMany<TDocument>(IEnumerable<Guid> ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public async Task<List<TDocument>> LoadManyAsync<TDocument, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) where TDocument : class
        {
            var results = new List<TDocument>();
            await foreach (var item in LoadStreamAsync<TDocument, TKey>(ids, cancellationToken).ConfigureAwait(false))
            {
                results.Add(item);
            }

            return results;
        }

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, string>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<int> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, int>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<long> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, long>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, Guid>(ids, cancellationToken);

        [Pure]
        public TDocument LoadRequired<TDocument, TKey>(TKey id) where TDocument : class
        {
            var result = Load<TDocument, TKey>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        [Pure]
        public TDocument LoadRequired<TDocument>(string id) where TDocument : class
            => LoadRequired<TDocument, string>(id);

        [Pure]
        public TDocument LoadRequired<TDocument>(int id) where TDocument : class
            => LoadRequired<TDocument, int>(id);

        [Pure]
        public TDocument LoadRequired<TDocument>(long id) where TDocument : class
            => LoadRequired<TDocument, long>(id);

        [Pure]
        public TDocument LoadRequired<TDocument>(Guid id) where TDocument : class
            => LoadRequired<TDocument, Guid>(id);

        [Pure]
        public async Task<TDocument> LoadRequiredAsync<TDocument, TKey>(TKey id, CancellationToken cancellationToken = default) where TDocument : class
        {
            var result = await LoadAsync<TDocument, TKey>(id, cancellationToken).ConfigureAwait(false);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, string>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(int id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, int>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(long id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, long>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(Guid id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, Guid>(id, cancellationToken);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument, TKey>(params TKey[] ids) where TDocument : class
            => LoadManyRequired<TDocument, TKey>(ids.AsEnumerable());

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument, TKey>(IEnumerable<TKey> ids) where TDocument : class
        {
            var idList = ids.Distinct().ToList();
            var results = LoadMany<TDocument, TKey>(idList);
            if (results.Count != idList.Count)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(record => !((TKey)configuration.DocumentMaps.GetId(record)).Equals(id)));
                throw new ResourceNotFoundException(firstMissing);
            }

            return results;
        }

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(params string[] ids) where TDocument : class
            => LoadManyRequired<TDocument, string>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(params int[] ids) where TDocument : class
            => LoadManyRequired<TDocument, int>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(params long[] ids) where TDocument : class
            => LoadManyRequired<TDocument, long>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(params Guid[] ids) where TDocument : class
            => LoadManyRequired<TDocument, Guid>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<string> ids) where TDocument : class
            => LoadManyRequired<TDocument, string>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<int> ids) where TDocument : class
            => LoadManyRequired<TDocument, int>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<long> ids) where TDocument : class
            => LoadManyRequired<TDocument, long>(ids);

        [Pure]
        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<Guid> ids) where TDocument : class
            => LoadManyRequired<TDocument, Guid>(ids);

        [Pure]
        public async Task<List<TDocument>> LoadManyRequiredAsync<TDocument, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) where TDocument : class
        {
            var idList = ids.Distinct().ToArray();
            var results = await LoadManyAsync<TDocument, TKey>(idList, cancellationToken).ConfigureAwait(false);
            if (results.Count != idList.Length)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(record => !((TKey)configuration.DocumentMaps.GetId(record)).Equals(id)));
                throw new ResourceNotFoundException(firstMissing);
            }

            return results;
        }

        [Pure]
        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, string>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<int> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, int>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<long> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, long>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, Guid>(ids, cancellationToken);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument, TKey>(params TKey[] ids) where TDocument : class
            => LoadStream<TDocument, TKey>(ids.AsEnumerable());

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument, TKey>(IEnumerable<TKey> ids) where TDocument : class
        {
            var idList = ids.Where(id => id != null).Distinct().ToList();

            return idList.Count == 0 ? new List<TDocument>() : Stream<TDocument>(PrepareLoadMany<TDocument, TKey>(idList));
        }

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<string> ids) where TDocument : class
            => LoadStream<TDocument, string>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<int> ids) where TDocument : class
            => LoadStream<TDocument, int>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<long> ids) where TDocument : class
            => LoadStream<TDocument, long>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<Guid> ids) where TDocument : class
            => LoadStream<TDocument, Guid>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params string[] ids) where TDocument : class
            => LoadStream<TDocument, string>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params int[] ids) where TDocument : class
            => LoadStream<TDocument, int>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params long[] ids) where TDocument : class
            => LoadStream<TDocument, long>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params Guid[] ids) where TDocument : class
            => LoadStream<TDocument, Guid>(ids);

        [Pure]
        public async IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument, TKey>(IEnumerable<TKey> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TDocument : class
        {
            var idList = ids.Where(id => id != null).Distinct().ToList();
            if (idList.Count == 0)
                yield break;

            await foreach (var item in StreamAsync<TDocument>(PrepareLoadMany<TDocument, TKey>(idList), cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, string>(ids, cancellationToken);

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<int> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, int>(ids, cancellationToken);

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<long> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, long>(ids, cancellationToken);

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, Guid>(ids, cancellationToken);

        public ITableSourceQueryBuilder<TRecord> Query<TRecord>() where TRecord : class
        {
            var map = configuration.DocumentMaps.Resolve(typeof(TRecord));
            var schemaName = configuration.GetSchemaNameOrDefault(map);
            var typeColumnName = map.TypeResolutionColumn?.ColumnName;
            var typeColumnValue = configuration.InstanceTypeResolvers.ResolveValueFromType(typeof(TRecord));

            return new TableSourceQueryBuilder<TRecord>(
                map.TableName,
                schemaName,
                map.IdColumn?.ColumnName,
                typeColumnName,
                typeColumnValue,
                this,
                tableAliasGenerator,
                ParameterNameGenerator,
                new CommandParameterValues(),
                new Parameters(),
                new ParameterDefaults());
        }

        public IQueryable<TDocument> Queryable<TDocument>()
        {
            return new Query<TDocument>(new QueryProvider(this, configuration));
        }

        public ISubquerySourceBuilder<TRecord> RawSqlQuery<TRecord>(string query) where TRecord : class
        {
            return new SubquerySourceBuilder<TRecord>(new RawSql(query), this, tableAliasGenerator, ParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        public IEnumerable<TRecord> Stream<TRecord>(string query, CommandParameterValues? args = null, TimeSpan? commandTimeout = null)
        {
            return Stream<TRecord>(new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, commandTimeout: commandTimeout));
        }

        public IAsyncEnumerable<TRecord> StreamAsync<TRecord>(string query, CommandParameterValues? args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return StreamAsync<TRecord>(new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, commandTimeout: commandTimeout), cancellationToken);
        }

        public IEnumerable<TRecord> Stream<TRecord>(PreparedCommand command)
        {
            IEnumerable<TRecord> Execute()
            {
                using var reader = ExecuteReader(command);
                foreach (var item in ProcessReader<TRecord>(reader, command))
                    yield return item;
            }

            return new EnumerableWithConcurrencyHandling<TRecord>(Execute, TransactionConcurrencyHandler);
        }

        public IAsyncEnumerable<TRecord> StreamAsync<TRecord>(PreparedCommand command, CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<TRecord> Execute()
            {
                var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    await foreach (var result in ProcessReaderAsync<TRecord>(reader, command, cancellationToken).ConfigureAwait(false))
                        yield return result;
                }
            }

            return new AsyncEnumerableWithConcurrencyHandling<TRecord>(Execute, TransactionConcurrencyHandler);
        }

        IEnumerable<TRecord> ProcessReader<TRecord>(DbDataReader reader, PreparedCommand command)
        {
            using var timed = new TimedSection(ms => configuration.QueryLogger.ProcessReader(ms, name, command.Statement));
            var correlationId = Guid.NewGuid().ToString();
            Log.DebugFormat("[{0}] Txn {1} Cmd {2}", correlationId, name, command.Statement);
            var strategy = configuration.ReaderStrategies.Resolve<TRecord>(command);
            var rowCounter = 0;

            while (reader.Read())
            {
                rowCounter++;
                var (instance, success) = strategy(reader);
                if (success)
                {
                    yield return instance;
                }
                else
                {
                    Log.DebugFormat("[{0}] Row {1} failed to be read and will be discarded", correlationId, rowCounter);
                }
            }
        }

        async IAsyncEnumerable<TRecord> ProcessReaderAsync<TRecord>(DbDataReader reader, PreparedCommand command, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var timed = new TimedSection(ms => configuration.QueryLogger.ProcessReader(ms, name, command.Statement));
            var correlationId = Guid.NewGuid().ToString();
            Log.DebugFormat("[{0}] Txn {1} Cmd {2}", correlationId, name, command.Statement);
            var strategy = configuration.ReaderStrategies.Resolve<TRecord>(command);
            var rowCounter = 0;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rowCounter++;
                var (instance, success) = strategy(reader);
                if (success)
                {
                    yield return instance;
                }
                else
                {
                    Log.DebugFormat("[{0}] Row {1} failed to be read and will be discarded", correlationId, rowCounter);
                }
            }
        }

        public IEnumerable<TResult> Stream<TResult>(string query, CommandParameterValues args, Func<IProjectionMapper, TResult> projectionMapper, TimeSpan? commandTimeout = null)
        {
            // Todo: Use CommandBehavior.SequentialAccess here.
            //  This would be a breaking change, since the projectionMapper func could read columns out of order.
            var command = new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout);
            using var reader = ExecuteReader(command);
            var mapper = new ProjectionMapper(command, reader, configuration.ReaderStrategies);
            while (reader.Read())
            {
                yield return projectionMapper(mapper);
            }
        }

        public IAsyncEnumerable<TResult> StreamAsync<TResult>(string query, CommandParameterValues args, Func<IProjectionMapper, TResult> projectionMapper, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<TResult> Execute()
            {
                // Todo: Use CommandBehavior.SequentialAccess here.
                //  This would be a breaking change, since the projectionMapper func could read columns out of order.
                var command = new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout);
                var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    var mapper = new ProjectionMapper(command, reader, configuration.ReaderStrategies);
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        yield return projectionMapper(mapper);
                    }
                }
            }

            return new AsyncEnumerableWithConcurrencyHandling<TResult>(Execute, TransactionConcurrencyHandler);
        }

        void AddCommandTrace(string commandText)
        {
            lock (commandTrace)
            {
                if (commandTrace.Count == 100)
                    Log.DebugFormat("A possible N+1 or long running transaction detected, this is a diagnostic message only does not require end-user action.\r\nStarted: {0:s}\r\nStack: {1}\r\n\r\n{2}", CreatedTime, Environment.StackTrace, string.Join("\r\n", commandTrace));

                if (commandTrace.Count <= 200)
                    commandTrace.Add(DateTime.Now.ToString("s") + " " + commandText);
            }

            customCommandTrace?.Invoke(commandText);
        }

        public int ExecuteNonQuery(string query, CommandParameterValues? args = null, TimeSpan? commandTimeout = null)
        {
            return ExecuteNonQuery(new PreparedCommand(query, args, RetriableOperation.None, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout));
        }

        public Task<int> ExecuteNonQueryAsync(string query, CommandParameterValues? args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return ExecuteNonQueryAsync(new PreparedCommand(query, args, RetriableOperation.None, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout), cancellationToken);
        }

        public int ExecuteNonQuery(PreparedCommand preparedCommand)
        {
            using var mutex = TransactionConcurrencyHandler.Lock();
            using var command = CreateCommand(preparedCommand);
            return command.ExecuteNonQuery();
        }

        public async Task<int> ExecuteNonQueryAsync(PreparedCommand preparedCommand, CancellationToken cancellationToken = default)
        {
            using var mutex = await TransactionConcurrencyHandler.LockAsync(cancellationToken).ConfigureAwait(false);
            using var command = CreateCommand(preparedCommand);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public TResult ExecuteScalar<TResult>(string query, CommandParameterValues? args = null, RetriableOperation retriableOperation = RetriableOperation.Select, TimeSpan? commandTimeout = null)
        {
            return ExecuteScalar<TResult>(new PreparedCommand(query, args, retriableOperation, commandTimeout: commandTimeout, commandBehavior: CommandBehavior.SingleRow));
        }

        public Task<TResult> ExecuteScalarAsync<TResult>(string query, CommandParameterValues? args = null, RetriableOperation retriableOperation = RetriableOperation.Select, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return ExecuteScalarAsync<TResult>(new PreparedCommand(query, args, retriableOperation, null, commandTimeout, commandBehavior: CommandBehavior.SingleRow), cancellationToken);
        }

        public TResult ExecuteScalar<TResult>(PreparedCommand preparedCommand)
        {
            using var mutex = TransactionConcurrencyHandler.Lock();
            using var command = CreateCommand(preparedCommand);
            var result = command.ExecuteScalar();
            if (result == DBNull.Value)
                return default!;
            return (TResult)result;
        }

        public async Task<TResult> ExecuteScalarAsync<TResult>(PreparedCommand preparedCommand, CancellationToken cancellationToken = default)
        {
            using var mutex = await TransactionConcurrencyHandler.LockAsync(cancellationToken).ConfigureAwait(false);
            using var command = CreateCommand(preparedCommand);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result == DBNull.Value)
                return default!;
            return (TResult)result;
        }

        public DbDataReader ExecuteReader(string query, CommandParameterValues? args = null, TimeSpan? commandTimeout = null)
        {
            return ExecuteReader(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout));
        }

        public Task<DbDataReader> ExecuteReaderAsync(string query, CommandParameterValues? args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return ExecuteReaderAsync(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout), cancellationToken);
        }

        public DbDataReader ExecuteReader(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return command.ExecuteReader();
        }

        public async Task<DbDataReader> ExecuteReaderAsync(PreparedCommand preparedCommand, CancellationToken cancellationToken = default)
        {
            using var command = CreateCommand(preparedCommand);
            return await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }

        protected TResult[] ReadResults<TResult>(PreparedCommand preparedCommand, Func<DbDataReader, TResult> mapper)
        {
            using var mutex = TransactionConcurrencyHandler.Lock();
            using var command = CreateCommand(preparedCommand);
            return command.ReadResults(mapper);
        }

        protected async Task<TResult[]> ReadResultsAsync<TResult>(PreparedCommand preparedCommand, Func<DbDataReader, Task<TResult>> mapper, CancellationToken cancellationToken)
        {
            using var mutex = await TransactionConcurrencyHandler.LockAsync(cancellationToken).ConfigureAwait(false);
            using var command = CreateCommand(preparedCommand);
            return await command.ReadResultsAsync(mapper, cancellationToken).ConfigureAwait(false);
        }

        async Task<DbTransaction> BeginTransactionWithRetryAsync(IsolationLevel isolationLevel, string sqlServerTransactionName, RetryPolicy retryPolicy, CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new InvalidOperationException("Must create a DbConnection before attempting to begin a transaction");

            if (!OwnsSqlTransaction)
                throw new InvalidOperationException("An existing transaction was provided, it should be opened externally");

            return await retryPolicy.LoggingRetries("Beginning Database Transaction").ExecuteActionAsync(
                async ct =>
                {
                    // A connection can exist, but be in a broken state.
                    // E.g. a valid connection is returned to the pool, we then acquire it, but on the SQL server end it's been killed perhaps due to Azure SQL resource limits.
                    // We re-open the same connection, following the logic in `DbCommandExtensions.EnsureValidConnection`
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

                    // We use the synchronous overload here even though there is an async one, because BeginTransactionAsync calls
                    // the synchronous version anyway, and the async overload doesn't accept a name parameter.
                    return BeginTransaction(isolationLevel, sqlServerTransactionName);
                },
                cancellationToken).ConfigureAwait(false);
        }

        DbTransaction BeginTransactionWithRetry(IsolationLevel isolationLevel, string sqlServerTransactionName, RetryPolicy retryPolicy)
        {
            if (connection == null) throw new InvalidOperationException("Must create a DbConnection before attempting to begin a transaction");

            if (!OwnsSqlTransaction)
                throw new InvalidOperationException("An existing transaction was provided, it should be opened externally");

            return retryPolicy.LoggingRetries("Beginning Database Transaction").ExecuteAction(() =>
            {
                if (connection.State != ConnectionState.Open) connection.Open();

                return BeginTransaction(isolationLevel, sqlServerTransactionName);
            });
        }

        PreparedCommand PrepareLoad<TDocument, TKey>(TKey id)
        {
            var mapping = configuration.DocumentMaps.Resolve(typeof(TDocument));

            if (mapping.IdColumn is null)
                throw new InvalidOperationException($"Cannot load {mapping.Type.Name} by Id, as no Id column has been mapped.");

            if (mapping.IdColumn.Type != typeof(TKey))
                throw new ArgumentException($"Provided Id of type '{id?.GetType().FullName}' does not match configured type of '{mapping.IdColumn?.Type.FullName}'.");

            var schema = configuration.GetSchemaNameOrDefault(mapping);
            var columnNames = GetColumnNames(schema, mapping.TableName);
            var tableName = mapping.TableName;
            var args = new CommandParameterValues { { "Id", mapping.IdColumn.PrimaryKeyHandler.ConvertToPrimitiveValue(id) } };

            return new PreparedCommand($"SELECT TOP 1 {string.Join(',', columnNames)} FROM [{schema}].[{tableName}] WHERE [{mapping.IdColumn.ColumnName}] = @Id", args, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        }

        PreparedCommand PrepareLoadMany<TDocument, TKey>(IEnumerable<TKey> idList)
        {
            var mapping = configuration.DocumentMaps.Resolve(typeof(TDocument));

            if (mapping.IdColumn?.Type != typeof(TKey))
                throw new ArgumentException($"Provided Id of type '{typeof(TKey).FullName}' does not match configured type of '{mapping.IdColumn?.Type.FullName}'.");

            var schema = configuration.GetSchemaNameOrDefault(mapping);
            var columnNames = GetColumnNames(schema, mapping.TableName);
            var param = new CommandParameterValues();
            param.AddTable("criteriaTable", idList.ToList(), mapping.IdColumn.PrimaryKeyHandler);
            var statement = $"SELECT s.{string.Join(',', columnNames)} FROM [{schema}].[{mapping.TableName}] s INNER JOIN @criteriaTable t on t.[ParameterValue] = s.[{mapping.IdColumn.ColumnName}] order by s.[{mapping.IdColumn.ColumnName}]";
            return new PreparedCommand(statement, param, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
        }

        public string[] GetColumnNames(string schemaName, string tableName)
        {
            return columnNameResolver.GetColumnNames(schemaName, tableName);
        }

        CommandExecutor CreateCommand(PreparedCommand command)
        {
            var timedSection = new TimedSection(ms => LogBasedOnCommand(ms, command));
            var sqlCommand = configuration.CommandFactory.CreateCommand(connection, Transaction, command.Statement, command.ParameterValues, configuration.TypeHandlers, command.Mapping, command.CommandTimeout);
            AddCommandTrace(command.Statement);
            if (command.ParameterValues != null)
            {
                var keys = command.ParameterValues.Keys.ToArray();
                ParameterNameGenerator.Return(keys);
            }

            if (configuration.DetectQueryPlanThrashing)
            {
                QueryPlanThrashingDetector.Detect(command.Statement);
            }

            return new CommandExecutor(sqlCommand, command, GetRetryPolicy(command.Operation), timedSection, this, configuration.AllowSynchronousOperations);
        }

        RetryPolicy GetRetryPolicy(RetriableOperation operation)
        {
            if (operationsToRetry == RetriableOperation.None)
                return RetryPolicy.NoRetry;

            return operationsToRetry.HasFlag(operation) ? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy() : RetryPolicy.NoRetry;
        }

        internal void WriteDebugInfoTo(StringBuilder sb)
        {
            string[] copy;
            lock (commandTrace)
                copy = commandTrace.ToArray();

            sb.AppendLine($"Transaction '{name}' {connection?.State} with {copy.Length} commands started at {CreatedTime:s} ({(DateTime.Now - CreatedTime).TotalSeconds:n2} seconds ago)");
            foreach (var command in copy)
                sb.AppendLine(command);
        }

        void LogBasedOnCommand(long duration, PreparedCommand command)
        {
            switch (command.Operation)
            {
                case RetriableOperation.Insert:
                    configuration.QueryLogger.Insert(duration, name, command.Statement);
                    break;
                case RetriableOperation.Update:
                    configuration.QueryLogger.Update(duration, name, command.Statement);
                    break;
                case RetriableOperation.Delete:
                    configuration.QueryLogger.Delete(duration, name, command.Statement);
                    break;
                case RetriableOperation.Select:
                    configuration.QueryLogger.ExecuteReader(duration, name, command.Statement);
                    break;
                case RetriableOperation.All:
                case RetriableOperation.None:
                default:
                    configuration.QueryLogger.NonQuery(duration, name, command.Statement);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{CreatedTime} - {connection?.State} - {name}";
        }

        string ITransactionDiagnostic.Name => name;

        string SqlServerTransactionName => name.Substring(0, Math.Min(name.Length, 32));

        void ITransactionDiagnostic.WriteCurrentTransactions(StringBuilder output)
        {
            registry.WriteCurrentTransactions(output);
        }

        DbTransaction BeginTransaction(IsolationLevel isolationLevel, string sqlServerTransactionName)
        {
            if (connection is SqlConnection sqlConnection)
            {
                return sqlConnection.BeginTransaction(isolationLevel, sqlServerTransactionName);
            }

            return connection!.BeginTransaction(isolationLevel);
        }

        public void Dispose()
        {
            var exceptions = new List<ExceptionDispatchInfo>();

            if (OwnsSqlTransaction)
            {
                TryDispose(Transaction);
            }
            
            TryDispose(TransactionTimer);
            TryDispose(TransactionConcurrencyHandler);

            if (OwnsSqlTransaction)
            {
                TryDispose(connection);
            }

            registry.Remove(this);

            if (exceptions.Count == 0) return;
            if (exceptions.Count == 1) exceptions.Single().Throw();
            throw new AggregateException(exceptions.Select(x => x.SourceException));

            void TryDispose(IDisposable? disposable)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ExceptionDispatchInfo.Capture(ex));
                }
            }
        }
    }
}