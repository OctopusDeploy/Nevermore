using System;
using System.Diagnostics;

namespace Nevermore.Diagnostics
{
    internal class TimedSection : IDisposable
    {
        readonly Action<long> callback;
        readonly Stopwatch stopwatch;

        internal TimedSection(Action<long> callback)
        {
            this.callback = callback;
            stopwatch = Stopwatch.StartNew();
        }

        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

        public void Dispose()
        {
            stopwatch.Stop();
            var ms = stopwatch.ElapsedMilliseconds;
            callback(ms);
        }
    }
}