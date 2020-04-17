using System;
using System.Diagnostics;
using Nevermore.Diagnositcs;

namespace Nevermore.Diagnostics
{
    internal class TimedSection : IDisposable
    {
        readonly Stopwatch stopwatch;
        readonly ILog log;
        readonly long infoThreshold;
        readonly long warningThreshold;
        readonly Func<long, string> formatMessage;

        internal TimedSection(ILog log, Func<long, string> formatMessage, long infoThreshold, long warningThreshold = long.MaxValue)
        {
            this.log = log;
            this.infoThreshold = infoThreshold;
            this.formatMessage = formatMessage;
            this.warningThreshold = warningThreshold;
            stopwatch = Stopwatch.StartNew();
        }

        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

        public void Dispose()
        {
            stopwatch.Stop();
            var ms = stopwatch.ElapsedMilliseconds;
            var level = ms >= warningThreshold
                ? LogLevel.Warn
                : ms >= infoThreshold
                    ? LogLevel.Info
                    : LogLevel.Debug;

            var message = formatMessage(ms);
            log.Log(level, () => message);
        }
    }
}