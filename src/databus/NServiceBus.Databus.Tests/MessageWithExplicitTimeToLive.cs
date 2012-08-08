namespace NServiceBus.DataBus.Tests
{
    [TimeToBeReceived("00:01:00")]
    public class MessageWithExplicitTimeToLive : IMessage
    {
        public DataBusProperty<string> DataBusProperty { get; set; }
    }
}