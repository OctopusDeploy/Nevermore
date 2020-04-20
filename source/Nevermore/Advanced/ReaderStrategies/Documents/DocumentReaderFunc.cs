using System.Data.Common;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal delegate object DocumentReaderFunc(DbDataReader reader, DocumentReaderContext context);
}