namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    public interface IMessageTypeInterpreter
    {
        string GetAssemblyQualifiedName(string nmsType);
    }
}