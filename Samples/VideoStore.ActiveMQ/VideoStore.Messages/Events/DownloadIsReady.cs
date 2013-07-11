namespace VideoStore.Messages.Events
{
    using System.Collections.Generic;

    public interface DownloadIsReady 
    {
        int OrderNumber { get; set; }
        Dictionary<string,string> VideoUrls { get; set; }
        string ClientId { get; set; }
    }
}