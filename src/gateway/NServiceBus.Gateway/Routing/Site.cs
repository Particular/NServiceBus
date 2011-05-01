namespace NServiceBus.Gateway.Routing
{
    using System;
  
    public class Site
    {
        public Type ChannelType { get; set; }

        public string Address { get; set; }

        public string Key { get; set; }
    }
}