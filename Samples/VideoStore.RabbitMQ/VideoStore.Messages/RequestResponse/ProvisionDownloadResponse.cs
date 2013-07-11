namespace VideoStore.Messages.RequestResponse
{
    public class ProvisionDownloadResponse
    {
        public int OrderNumber { get; set; }
        public string[] VideoIds { get; set; }
        public string ClientId { get; set; }
    }
}
