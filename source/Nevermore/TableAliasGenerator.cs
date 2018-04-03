using System;
using System.Linq;

namespace Nevermore
{
    public interface ITableAliasGenerator
    {
        string GenerateTableAlias(string tableName = null);
    }

    public class TableAliasGenerator : ITableAliasGenerator
    {
        int tableCount = 0;

        public string GenerateTableAlias(string tableName = null)
        {
            // Return a predictable alias so we don't blow out SQL's query-plan cache.
            tableCount++;
            return $"ALIAS_{AliasLabel(tableName)}_{tableCount}";
        }

        private string AliasLabel(string tableName = null) => !string.IsNullOrEmpty(tableName) ? tableName : "GENERATED";
    }
}