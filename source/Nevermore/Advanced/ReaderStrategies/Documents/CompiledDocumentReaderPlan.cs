using System;
using System.Data.Common;
using System.Diagnostics;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal class CompiledDocumentReaderPlan<TRecord> : ICompiledDocumentReaderPlan where TRecord : class
    {
        readonly IRelationalStoreConfiguration configuration;
        readonly DocumentMap map;
        readonly Func<DbDataReader, IDocumentReaderContext<TRecord>, TRecord> func;

        public CompiledDocumentReaderPlan(IRelationalStoreConfiguration configuration, DocumentMap map, Func<DbDataReader, IDocumentReaderContext<TRecord>,TRecord> func)
        {
            this.configuration = configuration;
            this.map = map;
            this.func = func;
        }

        public IDocumentReader CreateReader()
        {
            return new DocumentReader<TRecord>(configuration, map, func);
        }
    }
}