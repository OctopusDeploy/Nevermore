using System;
using System.Data.Common;
using System.Linq;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Util;
using Newtonsoft.Json;

namespace Nevermore.Advanced.ReaderStrategies
{
    /// <summary>
    /// The Document reader strategy is how we map query results from documents in Nevermore. It's probably the most
    /// commonly used strategy.
    /// </summary>
    public class DocumentReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;

        public DocumentReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return typeof(IId).IsAssignableFrom(type);
        }

        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            return command =>
            {
                var rowCount = 0;
                var idIndex = -1;
                var jsonIndex = -1;
                
                Func<DbDataReader, Type> typeResolver = null;
                var mapping = command.Mapping ?? configuration.Mappings.Resolve(typeof(TRecord));

                var expectedFieldCount = 0;
                
                return reader =>
                {
                    rowCount++;

                    if (rowCount == 1)
                    {
                        expectedFieldCount = reader.FieldCount;
                        
                        idIndex = GetOrdinal(reader, "Id");
                        jsonIndex = GetOrdinal(reader, "JSON");
                        typeResolver = mapping.InstanceTypeResolver.TypeResolverFromReader(s => GetOrdinal(reader, s));
                    }
                    
                    return MapSingleRecord<TRecord>(reader, mapping, typeResolver, jsonIndex, idIndex);
                };
            };
        }
        
        (TDocument, bool) MapSingleRecord<TDocument>(DbDataReader reader, DocumentMap mapping, Func<DbDataReader, Type> typeResolver, int jsonIndex, int idIndex)
        {
            TDocument instance;
            var instanceType = typeResolver(reader);

            if (jsonIndex >= 0)
            {
                var json = reader.GetString(jsonIndex);
                var deserialized = JsonConvert.DeserializeObject(json, instanceType, configuration.JsonSerializerSettings);
                // This is to handle polymorphic queries. e.g. Query<AzureAccount>()
                // If the deserialized object is not the desired type, then we are querying for a specific sub-type
                // and this record is a different sub-type, and should be excluded from the result-set.
                if (deserialized is TDocument document)
                {
                    instance = document;
                }
                else return (default, false);
            }
            else
            {
                instance = (TDocument) Activator.CreateInstance(instanceType, configuration.ObjectInitialisationOptions.HasFlag(ObjectInitialisationOptions.UseNonPublicConstructors));
            }

            var specificMapping = configuration.Mappings.Resolve(instance.GetType());
            var columnIndexes = specificMapping.IndexedColumns.ToDictionary(c => c, c => GetOrdinal(reader, c.ColumnName));

            foreach (var (columnMapping, i) in columnIndexes.Where(index => index.Value >= 0))
            {
                columnMapping.ReaderWriter.Write(instance, reader[i]);
            }

            if (idIndex >= 0)
            {
                mapping.IdColumn.ReaderWriter.Write(instance, reader[(int) idIndex]);
            }

            return (instance, true);
        }
        
        static int GetOrdinal(DbDataReader dr, string columnName)
        {
            for (var i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}