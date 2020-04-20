using System.Data.Common;

namespace Nevermore.Advanced.ReaderStrategies.ArbitraryClasses
{
    delegate TRecord ArbitraryClassReaderFunc<TRecord>(DbDataReader reader, ArbitraryClassReaderContext context);
}