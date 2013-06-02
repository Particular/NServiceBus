namespace NServiceBus.Gateway.Routing
{
    using Channels;

    public class Site
    {
        public Channel Channel { get; set; }
        public string Key { get; set; }
        public bool LegacyMode { get; set; }
    }
}