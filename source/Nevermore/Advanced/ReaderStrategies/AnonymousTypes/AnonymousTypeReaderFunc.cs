using System.Data.Common;

namespace Nevermore.Advanced.ReaderStrategies.AnonymousTypes
{
    internal delegate TRecord AnonymousTypeReaderFunc<TRecord>(DbDataReader reader, AnonymousTypeReaderContext context);
}