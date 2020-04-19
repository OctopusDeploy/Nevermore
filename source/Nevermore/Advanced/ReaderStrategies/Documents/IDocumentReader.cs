using System.Data.Common;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    interface IDocumentReader
    {
        object Read(DbDataReader dataReader);
    }
}