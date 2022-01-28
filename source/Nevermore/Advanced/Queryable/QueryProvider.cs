using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced.ReaderStrategies;
using Nevermore.Util;

namespace Nevermore.Advanced.Queryable
{
    internal class QueryProvider : IAsyncQueryProvider
    {
        static readonly MethodInfo GenericCreateQueryMethod = typeof(QueryProvider)
            .GetRuntimeMethods().Single(m => m.Name == nameof(CreateQuery) && m.IsGenericMethod);
        static readonly MethodInfo GenericExecuteMethod = typeof(QueryProvider)
            .GetRuntimeMethods().Single(m => m.Name == nameof(Execute) && m.IsGenericMethod);
        static readonly MethodInfo GenericExecuteScalarMethod = typeof(IReadQueryExecutor)
            .GetRuntimeMethod(nameof(IReadQueryExecutor.ExecuteScalar), new[] { typeof(PreparedCommand) });
        static readonly MethodInfo GenericExecuteScalarAsyncMethod = typeof(IReadQueryExecutor)
            .GetRuntimeMethod(nameof(IReadQueryExecutor.ExecuteScalarAsync), new[] { typeof(PreparedCommand), typeof(CancellationToken) });

        readonly IReadQueryExecutor queryExecutor;
        readonly IRelationalStoreConfiguration configuration;

        public QueryProvider(IReadQueryExecutor queryExecutor, IRelationalStoreConfiguration configuration)
        {
            this.queryExecutor = queryExecutor;
            this.configuration = configuration;
        }

        public IQueryable CreateQuery(Expression expression) =>
            (IQueryable)GenericCreateQueryMethod
                .MakeGenericMethod(expression.Type.GetSequenceType())
                .Invoke(this, new object[] { expression });

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new Query<TElement>(new QueryProvider(queryExecutor, configuration), expression);

        public object Execute(Expression expression)
        {
            return GenericExecuteMethod.MakeGenericMethod(expression.Type)
                .Invoke(this, new object[] { expression });
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var (command, queryType) = Translate(expression);

            if (queryType == QueryType.SelectMany)
            {
                var sequenceType = expression.Type.GetSequenceType();
                return (TResult)ReadList(command, sequenceType);
            }

            if (queryType == QueryType.SelectSingle)
            {
                return (TResult)ReadSingle(command, expression.Type);
            }

            return (TResult)GenericExecuteScalarMethod.MakeGenericMethod(expression.Type)
                .Invoke(queryExecutor, new object[] { command });
        }


        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var (command, queryType) = Translate(expression);
            if (queryType == QueryType.SelectMany)
            {
                var sequenceType = expression.Type.GetSequenceType();
                return (TResult)await ReadList(command, sequenceType, cancellationToken).ConfigureAwait(false);
            }

            if (queryType == QueryType.SelectSingle)
            {
                return (TResult)await ReadSingle(command, expression.Type, cancellationToken).ConfigureAwait(false);
            }

            return await ((Task<TResult>)GenericExecuteScalarAsyncMethod.MakeGenericMethod(expression.Type)
                    .Invoke(queryExecutor, new object[] { command, cancellationToken }))
                .ConfigureAwait(false);
        }

        public IAsyncEnumerable<TResult> StreamAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var (command, queryType) = Translate(expression);
            if (queryType is not QueryType.SelectMany)
            {
                throw new InvalidOperationException("Cannot stream the results of a query that selects only a single result.");
            }

            var sequenceType = expression.Type.GetSequenceType();
            return (IAsyncEnumerable<TResult>)GenericStreamAsyncMethod.MakeGenericMethod(sequenceType)
                .Invoke(queryExecutor, new object[] { command, cancellationToken });
        }

        public (PreparedCommand, QueryType) Translate(Expression expression)
        {
            return new QueryTranslator(configuration).Translate(expression);
        }

        IList ReadList(PreparedCommand command, Type elementType)
        {
            var list = CreateList(elementType);
            var readerStrategy = GetReaderStrategy(elementType, command);
            using var reader = queryExecutor.ExecuteReader(command);
            while (reader.Read())
            {
                var success = TryProcessRow(readerStrategy, elementType, reader, out var item);
                if (success)
                {
                    list.Add(item);
                }
            }

            return list;
        }
        
        async Task<IList> ReadList(PreparedCommand command, Type elementType, CancellationToken cancellationToken)
        {
            var list = CreateList(elementType);
            var readerStrategy = GetReaderStrategy(elementType, command);
            using var reader = await queryExecutor.ExecuteReaderAsync(command, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var success = TryProcessRow(readerStrategy, elementType, reader, out var item);
                if (success)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        object ReadSingle(PreparedCommand command, Type elementType)
        {
            var readerStrategy = GetReaderStrategy(elementType, command);
            using var reader = queryExecutor.ExecuteReader(command);
            if (reader.Read())
            {
                var success = TryProcessRow(readerStrategy, elementType, reader, out var item);
                if (success)
                {
                    return item;
                }
            }

            return elementType.GetDefaultValue();
        }

        async Task<object> ReadSingle(PreparedCommand command, Type elementType, CancellationToken cancellationToken)
        {
            var readerStrategy = GetReaderStrategy(elementType, command);
            await using var reader = await queryExecutor.ExecuteReaderAsync(command, cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var success = TryProcessRow(readerStrategy, elementType, reader, out var item);
                if (success)
                {
                    return item;
                }
            }

            return elementType.GetDefaultValue();
        }

        static bool TryProcessRow(Delegate readerStrategy, Type elementType, DbDataReader reader, out object item)
        {
            var readResult = readerStrategy.DynamicInvoke(reader);
            var tupleType = typeof(ValueTuple<,>).MakeGenericType(elementType, typeof(bool));
            // ReSharper disable once PossibleNullReferenceException
            var success = (bool)tupleType.GetField("Item2").GetValue(readResult);
            // ReSharper disable once PossibleNullReferenceException
            item = success
                ? tupleType.GetField("Item1").GetValue(readResult)
                : null;

            return success;
        }

        Delegate GetReaderStrategy(Type elementType, PreparedCommand command)
        {
            // ReSharper disable once PossibleNullReferenceException
            return (Delegate)typeof(IReaderStrategyRegistry)
                .GetMethod(nameof(IReaderStrategyRegistry.Resolve), 1, new[] { typeof(PreparedCommand) })
                .MakeGenericMethod(elementType)
                .Invoke(configuration.ReaderStrategies, new object[] { command });
        }

        static IList CreateList(Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);
            return list;
        }
    }
}
