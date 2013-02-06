namespace NServiceBus.Transport.ActiveMQ
{
    public interface IMessageTypeInterpreter
    {
        string GetAssemblyQualifiedName(string nmsType);
    }
}