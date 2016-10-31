using System;
using System.Diagnostics;
using Nevermore.Diagnositcs;

namespace Nevermore.Diagnostics
{
    internal class TimedSection : IDisposable
    {
        private readonly Stopwatch stopwatch;
        private readonly ILog log;
        private readonly long infoThreashold;
        private readonly long warningThreashold;
        private readonly Func<long, string> formatMessage;

        internal TimedSection(ILog log, Func<long, string> formatMessage, long infoThreashold, long warningThreashold = long.MaxValue)
        {
            this.log = log;
            this.infoThreashold = infoThreashold;
            this.formatMessage = formatMessage;
            this.warningThreashold = warningThreashold;
            stopwatch = Stopwatch.StartNew();
        }

        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

        public void Dispose()
        {
            stopwatch.Stop();
            var ms = stopwatch.ElapsedMilliseconds;
            var level = ms >= warningThreashold
                ? LogLevel.Warn
                : ms >= infoThreashold
                    ? LogLevel.Info
                    : LogLevel.Debug;

            var message = formatMessage(ms);
            log.Log(level, () => message);
        }
    }
}