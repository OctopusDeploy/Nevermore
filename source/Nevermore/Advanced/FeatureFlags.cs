using System;

namespace Nevermore.Advanced
{
    internal static class FeatureFlags
    {
        static FeatureFlags()
        {
            UseCteBasedListWithCount = Environment.GetEnvironmentVariable("NEVERMORE__UseCteBasedListWithCount") == "true";
        }

        public static bool UseCteBasedListWithCount { get; set; }
    }
}