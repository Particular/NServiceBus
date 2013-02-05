namespace MyMessages.Events
{
    using System.Collections.Generic;
    using NServiceBus;

    public interface OrderIsReady : IEvent
    {
        int OrderNumber { get; set; }
        Dictionary<string,string> VideoUrls { get; set; }
    }
}