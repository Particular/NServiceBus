namespace NServiceBus.Gateway.Routing
{
    using Channels;

    public class Site
    {
        public Site()
        {
            DefaultLegacyModeToEnabled();
        }

        public Channel Channel { get; set; }
        public string Key { get; set; }
        public bool LegacyMode { get; set; }

        [ObsoleteEx(RemoveInVersion = "5.0", Message = "From v5 we need to set the legacy mode to false.")]
        void DefaultLegacyModeToEnabled()
        {
            LegacyMode = true;
        }
    }
}