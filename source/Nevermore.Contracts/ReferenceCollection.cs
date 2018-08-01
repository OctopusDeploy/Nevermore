using System;
using System.Collections.Generic;

namespace Nevermore.Contracts
{
    /// <summary>
    /// A case-insensitive collection of unique strings used for holding document ID's.
    /// </summary>
    public class ReferenceCollection : HashSet<string>, IReadOnlyCollection<string>
    {
        public ReferenceCollection()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public ReferenceCollection(string singleValue)
            : this(new[] { singleValue })
        {
        }

        public ReferenceCollection(IEnumerable<string> values)
            : base(values, StringComparer.OrdinalIgnoreCase)
        {
        }

        public void ReplaceAll(IEnumerable<string> newItems)
        {
            Clear();

            if (newItems == null) return;

            foreach (var item in newItems)
            {
                Add(item);
            }
        }

        public ReferenceCollection Clone()
        {
            return new ReferenceCollection(this);
        }

        public override string ToString()
        {
            return string.Join(", ", this);
        }

        public static ReferenceCollection One(string item)
        {
            return new ReferenceCollection { item };
        }
    }
}