using System;
using System.Collections.Generic;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nevermore.Transient.Throttling
{
    public class ThrottlingCondition
    {
        static readonly Regex SqlErrorCodeRegEx = new Regex("Code:\\s*(\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        readonly IList<Tuple<ThrottledResourceType, ThrottlingType>> throttledResources = new List<Tuple<ThrottledResourceType, ThrottlingType>>(9);
        internal const int ThrottlingErrorNumber = 40501;

        public static ThrottlingCondition Unknown
        {
            get
            {
                var throttlingCondition = new ThrottlingCondition { ThrottlingMode = ThrottlingMode.Unknown };
                throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.Unknown, ThrottlingType.Unknown));
                return throttlingCondition;
            }
        }

        public ThrottlingMode ThrottlingMode { get; set; }

        public IEnumerable<Tuple<ThrottledResourceType, ThrottlingType>> ThrottledResources
        {
            get
            {
                return throttledResources;
            }
        }

        public bool IsThrottledOnDataSpace { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.PhysicalDatabaseSpace); } }
        public bool IsThrottledOnLogSpace { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.PhysicalLogSpace); } }
        public bool IsThrottledOnLogWrite { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.LogWriteIoDelay); } }
        public bool IsThrottledOnDataRead { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.DataReadIoDelay); } }
        public bool IsThrottledOnCpu { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.Cpu); } }
        public bool IsThrottledOnDatabaseSize { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.DatabaseSize); } }
        public bool IsThrottledOnWorkerThreads { get { return throttledResources.Any(x => x.Item1 == ThrottledResourceType.WorkerThreads); } }
        public bool IsUnknown { get { return ThrottlingMode == ThrottlingMode.Unknown; } }

        public static ThrottlingCondition FromException(SqlException ex)
        {
            if (ex == null) return Unknown;

            var firstThrottlingError = ex.Errors.OfType<SqlError>().FirstOrDefault(error => error.Number == ThrottlingErrorNumber);
            return firstThrottlingError != null ? FromError(firstThrottlingError) : Unknown;
        }

        public static ThrottlingCondition FromError(SqlError error)
        {
            if (error == null) return Unknown;

            int capturedReasonCode;
            var match = SqlErrorCodeRegEx.Match(error.Message);
            return match.Success && int.TryParse(match.Groups[1].Value, out capturedReasonCode) ? FromReasonCode(capturedReasonCode) : Unknown;
        }

        public static ThrottlingCondition FromReasonCode(int reasonCode)
        {
            // https://msdn.microsoft.com/en-us/library/azure/dn338079.aspx

            if (reasonCode <= 0) return Unknown;

            var throttlingMode = (ThrottlingMode)(reasonCode & 3);
            var throttlingCondition = new ThrottlingCondition { ThrottlingMode = throttlingMode };
            var num1 = reasonCode >> 8;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.PhysicalDatabaseSpace, (ThrottlingType)(num1 & 3)));
            int num2;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.PhysicalLogSpace, (ThrottlingType)((num2 = num1 >> 2) & 3)));
            int num3;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.LogWriteIoDelay, (ThrottlingType)((num3 = num2 >> 2) & 3)));
            int num4;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.DataReadIoDelay, (ThrottlingType)((num4 = num3 >> 2) & 3)));
            int num5;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.Cpu, (ThrottlingType)((num5 = num4 >> 2) & 3)));
            int num6;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.DatabaseSize, (ThrottlingType)((num6 = num5 >> 2) & 3)));
            int num7;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.Internal, (ThrottlingType)((num7 = num6 >> 2) & 3)));
            int num8;
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.WorkerThreads, (ThrottlingType)((num8 = num7 >> 2) & 3)));
            throttlingCondition.throttledResources.Add(Tuple.Create(ThrottledResourceType.Internal, (ThrottlingType)(num8 >> 2 & 3)));
            return throttlingCondition;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("Mode: {0} | ", ThrottlingMode);

            var throttledResourceDescriptions = throttledResources
                .Where(x => x.Item1 != ThrottledResourceType.Internal)
                .Select(x => string.Format("{0}: {1}", x.Item1, x.Item2))
                .OrderBy(x => x)
                .ToArray();
            
            stringBuilder.Append(string.Join(", ", throttledResourceDescriptions));
            
            return stringBuilder.ToString();
        }
    }
}