using System;
using System.Collections.Concurrent;
using System.Data.Common;
using Nevermore.Advanced.ReaderStrategies.Documents;

namespace Nevermore.Advanced.ReaderStrategies
{
    /// <summary>
    /// The Document reader strategy is how we map query results from documents in Nevermore. It's probably the most
    /// commonly used strategy, and it's called when you use Load, Stream, Query, and so on. 
    /// </summary>
    internal class DocumentReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;
        readonly ConcurrentDictionary<int, ICompiledDocumentReaderPlan> plans = new ConcurrentDictionary<int, ICompiledDocumentReaderPlan>();
        
        public DocumentReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return configuration.Mappings.ResolveOptional(type, out _);
        }
        
        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            // This is executed once per type that we query. Keep in mind that there might be multiple concrete types, 
            // and they could all have a base that is mapped.
            
            var mapping = configuration.Mappings.Resolve<TRecord>();
            
            return command =>
            {
                if (command.Mapping != null && command.Mapping != mapping)
                    throw new InvalidOperationException("You cannot use a different DocumentMap when reading documents except for the map defined for the type.");

                IDocumentReader documentReader = null;

                var rowNumber = 0;
                return dbDataReader =>
                {
                    rowNumber++;
                    
                    try
                    {
                        if (documentReader == null)
                        {
                            // For each query, we create a plan. This involves looking at the columns returned from the query, 
                            // matching them to the document map columns, then generating and compiling an expression optimized
                            // for handling this query. It means that the first query will be slow, but every subsequent query
                            // will be faster. 
                            // Our cache uses the SQL statement, as that tells us what columns to expect ("select *..."),
                            // and the field count (in case the schema or something else changes). The plans are also per type 
                            // being queried.
                            var cacheKey = HashCode.Combine(mapping, command.Statement, dbDataReader.FieldCount);
                            var plan = plans.GetOrAdd(cacheKey, _ => DocumentReaderCompiler.CompilePlan(mapping, dbDataReader, configuration));
                            documentReader = plan.CreateReader();
                        }

                        var result = documentReader.Read(dbDataReader);
                        if (result is TRecord record)
                            return (record, true);
                        return (default, false);
                    }
                    catch (ReaderException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new ReaderException($"Error reading row {rowNumber}: " + ex.Message, ex);
                    }
                };
            };
        }
    }
}