using System;
using System.Data.Common;
using Nevermore.Util;

namespace Nevermore.Advanced.ReaderStrategies
{
    public interface IReaderStrategy
    {
        bool CanRead(Type type);
        Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>();
    }
}