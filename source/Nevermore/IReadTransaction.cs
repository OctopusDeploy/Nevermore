using System;

namespace Nevermore
{
    public interface IReadTransaction : IReadQueryExecutor, IDisposable { }
}