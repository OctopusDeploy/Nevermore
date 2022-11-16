using System;
using System.Data.Common;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal class DocumentReaderContext
    {
        readonly IRelationalStoreConfiguration configuration;
        readonly DocumentMap map;

        public DocumentReaderContext(IRelationalStoreConfiguration configuration, DocumentMap map)
        {
            this.configuration = configuration;
            this.map = map;
            Column = -1;
        }

        public int Column;
        
        public Type ResolveType(object typeColumnValue)
        {
            // The type column can be null, which tells us to use the base type. If the base type is abstract or otherwise
            // can't be created, the serializer can deal with that (maybe a contract resolver or something will handle it). 
            if (typeColumnValue == null || typeColumnValue == DBNull.Value) 
                return map.Type;

            // But if there IS a value in the Type column, then we expect a type resolver to know how to deal with it. 
            var resolved = configuration.InstanceTypeResolvers.ResolveTypeFromValue(map.Type, typeColumnValue);
            if (resolved == null)
                throw new InvalidOperationException($"The 'Type' column has a value of '{typeColumnValue}' ({typeColumnValue.GetType().Name}), but no type resolver was able to map it to a concrete type to deserialize. Either register an instance type resolver that knows how to interpret the value, or consider fixing the data.");

            return resolved;
        }

        public TDocument DeserializeText<TDocument>(DbDataReader reader, int index, Type concreteType) where TDocument : class
        {
            if (reader.IsDBNull(index))
                return default;
            
            // Type handlers can return "void" which means to hide this result from the result set.
            if (concreteType == ITypeHandler.HideType)
                return default;
            
            // The JSON column is typically the largest column on a document, and contains one big string.
            // We have two approaches for reading the JSON. We start by simply reading it to a string with
            // GetString(). This is typically faster for small objects, and the values are GC'd quickly. 
            // Alternatively, we can also stream the data, which saves memory and prevents large strings going
            // to the LOH. But, that's slower for small documents. 
            // We initially assume that most documents are small, but if in our plan we encounter a large
            // document, we remember that and use streaming next time. This will apply from row 2 onwards for
            // the first time the query is run.
            // As a worst case, we'll send the first large document we encounter per SQL query to the LOH, then
            // stream the rest.
            // The 1K cutoff was determined by querying generating different sizes of document, then using
            // different cutoff values, and comparing the results to balance speed and memory allocations. 
            // In Octofront, roughly 75% of all documents fall into the <1K category. 
            // In a large Octopus database, roughly 80% of all documents fall into the <1K category. 
            // Documents that are larger than 1K tend to be clustered in specific tables. 
            if (!map.ExpectLargeDocuments)
            {
                var text = reader.GetString(index);
                if (text.Length >= NevermoreDefaults.LargeDocumentCutoffSize) 
                    // We'll know for next time!
                    map.ExpectLargeDocuments = true;

                return AsDocument<TDocument>(concreteType, configuration.DocumentSerializer.DeserializeSmallText(text, concreteType));
            }

            // Large documents will be streamed. Since it's a text column, we have to call GetChars.
            // DataReaderTextStream exposes that character stream as a stream that our serializer can read
            // (as serializers typically prefer byte[] streams)
            using var dataStream = new DataTextReader(reader, index);
            return AsDocument<TDocument>(concreteType, configuration.DocumentSerializer.DeserializeLargeText(dataStream, concreteType));
        }

        public TDocument DeserializeCompressed<TDocument>(DbDataReader reader, int index, Type concreteType) where TDocument : class
        {
            if (reader.IsDBNull(index))
                return default;
            
            // Type handlers can return "void" which means to hide this result from the result set.
            if (concreteType == ITypeHandler.HideType)
                return default;
            
            using var stream = reader.GetStream(index);
            var result = configuration.DocumentSerializer.DeserializeCompressed(stream, concreteType);
            return AsDocument<TDocument>(concreteType, result);
        }

        static TDocument AsDocument<TDocument>(Type concreteType, object result) where TDocument : class
        {
            if (result == null)
                return default;
            
            if (result is TDocument doc)
                return doc;

            throw new InvalidOperationException($"The type resolver returned type '{concreteType?.FullName}', and the serializer returned type '{result?.GetType().FullName}', but it cannot be converted to type '{typeof(TDocument).FullName}' which is the type of document being queried. This may mean there is type resolver returning an incorrect type.");
        }

        public TDocument SelectPreferredResult<TDocument>(TDocument fromJson, TDocument fromJsonBlob)
        {
            // No matter what the document map says, if we've got one result, take it! Maybe the document map is just wrong.
            if (fromJsonBlob == null)
                return fromJson;
            
            if (fromJson == null)
                return fromJsonBlob;

            // However if there are two results, go with the preference in the document map
            if (map.JsonStorageFormat == JsonStorageFormat.MixedPreferText || map.JsonStorageFormat == JsonStorageFormat.TextOnly)
            {
                return fromJson;
            }

            if (map.JsonStorageFormat == JsonStorageFormat.MixedPreferCompressed || map.JsonStorageFormat == JsonStorageFormat.CompressedOnly)
            {
                return fromJsonBlob;
            }
            
            return default;
        }
    }
}