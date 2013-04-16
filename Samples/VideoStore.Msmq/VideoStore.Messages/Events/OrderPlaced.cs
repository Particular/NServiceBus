namespace VideoStore.Messages.Events
{
    public interface OrderPlaced
    {
        int OrderNumber { get; set; }
        string[] VideoIds { get; set; }
        string ClientId { get; set; }
    }
}