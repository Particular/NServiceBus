namespace NServiceBus.DataBus.Tests
{
    public class MessageWithDataBusProperty : IMessage
    {
        public DataBusProperty<string> DataBusProperty { get; set; }
    }
}