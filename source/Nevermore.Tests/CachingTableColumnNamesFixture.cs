using System;
using System.Collections.Generic;
using FluentAssertions;
using Nevermore.TableColumnNameResolvers;
using NUnit.Framework;

namespace Nevermore.Tests
{
    public class CachingTableColumnNamesFixture
    {
        class MockTableNameResolverForCaching : ITableColumnNameResolver
        {
            public static int TimesQueried;
            
            readonly Dictionary<string, string[]> tableToColumnNames;

            public MockTableNameResolverForCaching(Dictionary<string, string[]> tableToColumnNames)
            {
                this.tableToColumnNames = tableToColumnNames;
            }
            
            public string[] GetColumnNames(string schemaName, string tableName)
            {
                TimesQueried++;
                if (tableToColumnNames.ContainsKey(tableName))
                {
                    return tableToColumnNames[tableName];    
                }

                throw new Exception($"Column names for table {tableName} were not specified in creation");
            }
        }
        
        [Test]
        public void ShouldCacheColumnNameForSchema()
        {
            const string tableName = "VideoGame";
            var columnNames = new[] {"Title", "Genre", "ReleaseDate"};
            
            var tableCache = new TableColumnsCache();
            var map = new Dictionary<string, string[]>();
            map.Add(tableName, columnNames);
            var cachingColumnNameResolvers = new CachingTableColumnNameResolver(
                new MockTableNameResolverForCaching(map), tableCache);
            
            var columns = cachingColumnNameResolvers.GetColumnNames("", tableName);
            columns.Should().BeEquivalentTo(columnNames);
            
            // Query again to hit the caching
            cachingColumnNameResolvers.GetColumnNames("", tableName);
            columns.Should().BeEquivalentTo(columnNames);
            
            MockTableNameResolverForCaching.TimesQueried.Should().Be(1);
        }
    }
}