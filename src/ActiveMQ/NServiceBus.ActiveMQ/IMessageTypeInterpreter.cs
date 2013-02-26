namespace NServiceBus.Transports.ActiveMQ
{
    public interface IMessageTypeInterpreter
    {
        string GetAssemblyQualifiedName(string nmsType);
    }
}