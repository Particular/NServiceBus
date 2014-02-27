namespace NServiceBus.Unicast.Tests.Contexts
{
    public class CommandWithDataBusPropertyMessage : ICommand
    {
        public DataBusProperty<byte[]> MyData { get; set; } 
    }
}