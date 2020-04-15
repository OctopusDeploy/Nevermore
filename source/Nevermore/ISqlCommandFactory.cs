using System;
using System.Data.Common;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore
{
    public interface ISqlCommandFactory
    {
        DbCommand CreateCommand(DbConnection connection, DbTransaction transaction, string statement, CommandParameterValues args, ITypeHandlerRegistry typeHandlers, DocumentMap mapping = null, TimeSpan? commandTimeout = null);
    }
}