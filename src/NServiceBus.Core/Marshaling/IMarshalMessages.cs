namespace NServiceBus;

using Transport;

public interface IMarshalMessages
{
    public IncomingMessage CreateIncomingMessage(MessageContext messageContext);
    public bool IsValidMessage(MessageContext messageContext);
}