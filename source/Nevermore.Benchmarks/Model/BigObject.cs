using System;
using System.Collections.Generic;

namespace Nevermore.Benchmarks.Model
{
    public class BigObject
    {
        public string Id { get; set; }

        public string Name { get; set; }
        
        public List<object> History { get; set; }
    }

    public class BigObjectHistoryEntry
    {
        public Guid Id { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
    }
}