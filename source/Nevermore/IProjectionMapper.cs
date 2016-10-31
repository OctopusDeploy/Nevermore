using System;
using System.Data;

namespace Nevermore
{
    public interface IProjectionMapper
    {
        TResult Map<TResult>(string prefix);
        void Read(Action<IDataReader> callback);
        TColumn Read<TColumn>(Func<IDataReader, TColumn> callback);
    }
}