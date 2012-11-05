namespace NServiceBus.DataBus.Tests
{
    public class MessageWithDataBusProperty : IMessage
    {
        public DataBusProperty<string> DataBusProperty { get; set; }
    }

    public class MessageWithNullDataBusProperty : IMessage
    {
        public DataBusProperty<string> DataBusProperty { get; set; }
    }
}