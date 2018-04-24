using System;

namespace Nevermore
{
    public interface IRelationalTransaction : IQueryExecutor, IDisposable
    {
    }
}