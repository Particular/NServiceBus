namespace NServiceBus.DataBus.Tests
{
    public class MessageWithNonSerializableDataBusProperty : IMessage
    {
        public NonSerializable PropertyDataBus { get; set; }
    }

    public class NonSerializable
    {

    }
}