using Nevermore.Diagnositcs.LogProviders;

namespace Nevermore.Diagnostics
{
    public static class EnabledProviders
    {
        public static bool Serilog
        {
            get => SerilogLogProvider.ProviderIsAvailableOverride;
            set => SerilogLogProvider.ProviderIsAvailableOverride = value;
        }

        public static bool NLog
        {
            get => NLogLogProvider.ProviderIsAvailableOverride;
            set => NLogLogProvider.ProviderIsAvailableOverride = value;
        }

        public static bool Log4Net
        {
            get => Log4NetLogProvider.ProviderIsAvailableOverride;
            set => Log4NetLogProvider.ProviderIsAvailableOverride = value;
        }

        public static bool Loupe
        {
            get => LoupeLogProvider.ProviderIsAvailableOverride;
            set => LoupeLogProvider.ProviderIsAvailableOverride = value;
        }

        public static bool EntLib
        {
            get => EntLibLogProvider.ProviderIsAvailableOverride;
            set => EntLibLogProvider.ProviderIsAvailableOverride = value;
        }
    }
}
