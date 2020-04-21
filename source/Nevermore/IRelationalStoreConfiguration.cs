using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.Advanced.ReaderStrategies;
using Nevermore.Advanced.Serialization;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;

namespace Nevermore
{
    public interface IRelationalStoreConfiguration
    {
        string ApplicationName { get; set; }
        string ConnectionString { get; }
        IDocumentMapRegistry DocumentMaps { get; }
        IDocumentSerializer DocumentSerializer { get; set; }
        IReaderStrategyRegistry ReaderStrategies { get; }
        ITypeHandlerRegistry TypeHandlers { get; }
        IInstanceTypeRegistry InstanceTypeResolvers { get; }
        IRelatedDocumentStore RelatedDocumentStore { get; set; }
        
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
        /// Gets or sets the factory that creates SQL commands. Set this if you want to control how commands are set up,
        /// or add a decorator to capture diagnostic information. 
        /// </summary>
        ISqlCommandFactory CommandFactory { get; set; }
    }
}