namespace Headquarter.Messages
{
    using NServiceBus;

    public class PriceUpdateReceived : IMessage
    {
        public string BranchOffice { get; set; }
    }
}