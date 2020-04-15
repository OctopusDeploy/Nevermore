using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Nevermore.Advanced.ReaderStrategies;

namespace Nevermore.Advanced
{
    internal class ProjectionMapper : IProjectionMapper
    {
        readonly PreparedCommand command;
        readonly DbDataReader reader;
        readonly IReaderStrategyRegistry readerStrategies;
        readonly Dictionary<string, object> readers = new Dictionary<string, object>();
            
        public ProjectionMapper(PreparedCommand command, DbDataReader reader, IReaderStrategyRegistry readerStrategies)
        {
            this.command = command;
            this.reader = reader;
            this.readerStrategies = readerStrategies;
        }

        public TResult Map<TResult>(string prefix)
        {
            if (!readers.ContainsKey(prefix))
            {
                var prefixedReader = new PrefixedDataReader(prefix + "_", reader);
                var func = readerStrategies.Resolve<TResult>(command);
                readers.Add(prefix, new ProjectingReader<TResult>(func, prefixedReader));
            }

            return ((ProjectingReader<TResult>) readers[prefix]).Map().Result;
        }

        public void Read(Action<IDataReader> callback)
        {
            callback(reader);
        }

        public TColumn Read<TColumn>(Func<IDataReader, TColumn> callback)
        {
            return callback(reader);
        }

        class ProjectingReader<TResult>
        {
            readonly Func<DbDataReader, (TResult Result, bool Success)> func;
            readonly PrefixedDataReader prefixedDataReader;

            public ProjectingReader(Func<DbDataReader, (TResult Result, bool Success)> func, PrefixedDataReader prefixedDataReader)
            {
                this.func = func;
                this.prefixedDataReader = prefixedDataReader;
            }

            public (TResult Result, bool Success) Map()
            {
                return func(prefixedDataReader);
            }
        }
    }
}