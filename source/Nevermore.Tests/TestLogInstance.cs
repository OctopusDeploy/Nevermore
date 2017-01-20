using System;
using System.Collections.Generic;
using Nevermore.Diagnositcs;

namespace Nevermore.Tests
{
    public class TestLogProvider : ILogProvider
    {
        public static TestLogProvider Setup()
        {
            var provider = new TestLogProvider();
            LogProvider.SetCurrentLogProvider(provider);
            return provider;
        }

        public List<LogEntry> Entries { get; } = new List<LogEntry>();

        public Logger GetLogger(string name)
        {
            return Log;
        }

        private bool Log(LogLevel loglevel, Func<string> messagefunc, Exception exception, object[] formatparameters)
        {
            Entries.Add(new LogEntry(loglevel, messagefunc()));
            return true;
        }

        public IDisposable OpenNestedContext(string message)
        {
            return null;
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            return null;
        }

        public class LogEntry
        {
            public LogEntry(LogLevel level, string message)
            {
                Level = level;
                Message = message;
            }

            public LogLevel Level { get; }
            public string Message { get; }
        }
    }
}