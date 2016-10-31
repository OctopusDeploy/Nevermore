using System;

namespace Nevermore
{
    [Flags]
    public enum RetriableOperation
    {
        None = 0,
        Select = 1,
        Insert = 2,
        Update = 4,
        Delete = 8,
        All = Select | Insert | Update | Delete
    }
}