namespace VideoStore.Messages.Events
{
    public interface OrderCancelled 
    {
        int OrderNumber { get; set; }
        string ClientId { get; set; }
    }
}