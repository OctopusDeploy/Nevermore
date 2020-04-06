using System;
using System.Data.Common;
using Nevermore.Mapping;

namespace Nevermore
{
    public interface ISqlCommandFactory
    {
        DbCommand CreateCommand(DbConnection connection, DbTransaction transaction, string statement, CommandParameterValues args, DocumentMap mapping = null, TimeSpan? commandTimeout = null);
    }
}