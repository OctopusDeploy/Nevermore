using Nevermore.Advanced.Hooks;
using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.Advanced.ReaderStrategies;
using Nevermore.Advanced.Serialization;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;

namespace Nevermore
{
    public interface IRelationalStoreConfiguration
    {
        string ApplicationName { get; set; }
        string ConnectionString { get; }

        /// <summary>
        /// Gets or sets whether synchronous operations are allowed. The default is <value>true</value>. Set to
        /// <value>false</value> to have Nevermore throw a <see cref="SynchronousOperationsDisabledException"/> when
        /// calling a synchronous operation.
        /// </summary>
        public bool AllowSynchronousOperations { get; set; }

        /// <summary>
        /// Gets or sets the default schema name (e.g., 'dbo') that will be used as a prefix on all statements. Can be
        /// overridden on each document map.
        /// </summary>
        string DefaultSchema { get; set; }

        IDocumentMapRegistry DocumentMaps { get; }
        CacheTableColumnsBuilder CacheTableColumns { get; }
        IDocumentSerializer DocumentSerializer { get; set; }
        IReaderStrategyRegistry ReaderStrategies { get; }
        ITypeHandlerRegistry TypeHandlers { get; }
        IInstanceTypeRegistry InstanceTypeResolvers { get; }
        IPrimaryKeyHandlerRegistry PrimaryKeyHandlers { get; }
        IRelatedDocumentStore RelatedDocumentStore { get; set; }
        IQueryLogger QueryLogger { get; set; }

        /// <summary>
        /// Hooks can be used to apply general logic when documents are inserted, updated or deleted.
        /// </summary>
        IHookRegistry Hooks { get; }

        /// <summary>
        /// Gets or sets the factory that creates SQL commands. Set this if you want to control how commands are set up,
        /// or add a decorator to capture diagnostic information.
        /// </summary>
        ISqlCommandFactory CommandFactory { get; set; }

        /// <summary>
        /// Gets or sets the key block size that will be used for the key allocator. A higher number enables less
        /// SQL queries to get new blocks, but increases fragmentation.
        /// </summary>
        int KeyBlockSize { get; set; }

        /// <summary>
        /// Gets or sets whether Multiple Active Result Sets is enabled. On .NET Core 3.1 this has issues on Linux, so it
        /// defaults to false.
        /// </summary>
        /// <remarks>https://docs.microsoft.com/en-us/sql/relational-databases/native-client/features/using-multiple-active-result-sets-mars?view=sql-server-ver15</remarks>
        bool ForceMultipleActiveResultSets { get; set; }

        /// <summary>
        /// Gets or sets whether to actively detect similar queries being executed in a way that is slightly different,
        /// resulting in duplicate query plans being created.
        /// </summary>
        public bool DetectQueryPlanThrashing { get; set; }

        /// <summary>
        /// Gets or sets whether to support a larger number of related documents (currently Nevermore errors at 1000) by using table valued parameters
        /// This is a temporary feature switch, we will always be using table valued parameters once we're satisfied with the stability
        /// </summary>
        public bool SupportLargeNumberOfRelatedDocuments { get; set; }
    }
}