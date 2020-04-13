using System;
using System.Data.Common;

namespace Nevermore.Advanced.TypeHandlers
{
    public interface ITypeHandler
    {
        bool CanConvert(Type objectType);
        object ReadDatabase(DbDataReader reader, int columnIndex);
        void WriteDatabase(DbParameter parameter, object value);
    }
}