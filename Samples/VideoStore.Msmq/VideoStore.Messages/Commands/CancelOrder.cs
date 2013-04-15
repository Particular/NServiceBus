namespace VideoStore.Messages.Commands
{
    public class CancelOrder 
    {
        public int OrderNumber { get; set; }
        public string ClientId { get; set; }
    }
}