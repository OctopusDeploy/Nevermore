using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Tests
{
    public class TestTableColumnsCache<TResult> : TableColumnsCache
    {
        
        public TestTableColumnsCache(IRelationalStore store) : base(store)
        {
        }

        protected override List<string> GetColumnNames(string tableName)
        {
            var memberInfos = Activator.CreateInstance<TResult>().GetType().GetProperties();
            return memberInfos.Select(x => x.Name).ToList();
        }
    }
}