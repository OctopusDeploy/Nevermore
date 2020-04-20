using System.Data.Common;

namespace Nevermore.Advanced.ReaderStrategies.ValueTuples
{
    delegate TTuple ValueTupleReaderFunc<TTuple>(DbDataReader reader, ValueTupleReaderContext context);
}