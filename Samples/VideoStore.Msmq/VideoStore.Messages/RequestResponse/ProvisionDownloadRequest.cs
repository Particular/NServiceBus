namespace VideoStore.Messages.RequestResponse
{
    public class ProvisionDownloadRequest 
    {
        public int OrderNumber { get; set; }
        public string[] VideoIds { get; set; }
        public string ClientId { get; set; }
    }
}
