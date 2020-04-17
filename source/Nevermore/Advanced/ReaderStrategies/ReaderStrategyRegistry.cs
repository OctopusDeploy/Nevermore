using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using Nevermore.Util;

namespace Nevermore.Advanced.ReaderStrategies
{
    public class ReaderStrategyRegistry : IReaderStrategyRegistry
    {
        readonly List<IReaderStrategy> strategies = new List<IReaderStrategy>();
        readonly ConcurrentDictionary<Type, object> cache = new ConcurrentDictionary<Type, object>();

        public void Register(IReaderStrategy strategy)
        {
            strategies.Add(strategy);
        }

        public Func<DbDataReader, (TRecord, bool)> Resolve<TRecord>(PreparedCommand command)
        {
            var cached = (Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>>)cache.GetOrAdd(typeof(TRecord), t => (object)Find<TRecord>());

            return cached(command);
        }

        Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> Find<TRecord>()
        {
            foreach (var strategy in strategies)
            {
                if (strategy.CanRead(typeof(TRecord)))
                {
                    return strategy.CreateReader<TRecord>();
                }
            }
            
            throw new InvalidOperationException($"No strategy in Nevermore knows how to map the type {typeof(TRecord).Name}. Consider using a different type or using an ITypeHandler.");
        }
    }
}