namespace MyMessages.Commands
{
    using NServiceBus;

    public class CancelOrder : ICommand
    {
        public int OrderNumber { get; set; }
        public string ClientId { get; set; }
    }
}