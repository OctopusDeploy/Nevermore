using System;

namespace Nevermore
{
    public interface IDisposableRelationalTransaction : IRelationalTransaction, IDisposable
    {
    }
}