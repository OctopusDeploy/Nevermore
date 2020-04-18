using System;
using System.Data.Common;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal interface IDocumentReaderContext<TDocument> where TDocument : class
    {
        Type ResolveType(object typeLocal);
        TDocument DeserializeText(DbDataReader reader, int index, Type concreteType);
        TDocument DeserializeCompressed(DbDataReader reader, int index, Type concreteType);
        TDocument SelectPreferredResult(TDocument fromJson, TDocument fromJsonBlob);
    }
}