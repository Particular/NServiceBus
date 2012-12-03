namespace NServiceBus.ActiveMQ
{
    public interface IMessageTypeInterpreter
    {
        string GetAssemblyQualifiedName(string nmsType);
    }
}