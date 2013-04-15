namespace VideoStore.Messages.Commands
{
    public class SubmitOrder
    {
        public int OrderNumber { get; set; }
        public string[] VideoIds { get; set; }
        public string ClientId { get; set; }
    }
}
