namespace NServiceBus
{
    public static class ConfigureSecondLevelRetriesExtensions
    {
        [ObsoleteEx(Replacement = "Configure.Features.Disable<SecondLevelRetries>()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableSecondLevelRetries(this Configure config)
        {
            Configure.Features.Disable<Features.SecondLevelRetries>();

            return config;
        }
    }
}