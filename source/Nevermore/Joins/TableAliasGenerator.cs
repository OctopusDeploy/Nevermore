using System;
using System.Linq;

namespace Nevermore.Joins
{
    public interface ITableAliasGenerator
    {
        string GenerateTableAlias(string tableName = null);
    }

    public class TableAliasGenerator : ITableAliasGenerator
    {
        static readonly Random Random = new Random();
        const int AliasRandomLength = 5;
        int tableJoinCount = 0;


        public string GenerateTableAlias(string tableName = null)
        {
            // If a tableName is specified, return a predictable alias so we don't blow out SQL's query-plan cache.
            string alias;
            if (!string.IsNullOrEmpty(tableName))
            {
                alias = $"ALIAS_{tableName}_{tableJoinCount}";
                tableJoinCount++;
            }
            else
            {
                alias = $"ALIAS_{RandomString()}"; // todo: make this deterministic, not random. Maybe just increment number instead
            }
            return alias;
        }

        static string RandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(chars, AliasRandomLength)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

    }
}