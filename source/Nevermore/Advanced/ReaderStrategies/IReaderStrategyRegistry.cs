using System;
using System.Data.Common;
using Nevermore.Util;

namespace Nevermore.Advanced.ReaderStrategies
{
    public interface IReaderStrategyRegistry
    {
        void Register(IReaderStrategy strategy);
        Func<DbDataReader, (TRecord, bool)> Resolve<TRecord>(PreparedCommand command);
    }
}