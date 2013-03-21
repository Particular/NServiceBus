namespace MyMessages.Events
{
    using System.Collections.Generic;
    using NServiceBus;

    public interface DownloadIsReady : IEvent
    {
        int OrderNumber { get; set; }
        Dictionary<string,string> VideoUrls { get; set; }
        string ClientId { get; set; }
    }
}