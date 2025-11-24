namespace NServiceBus;

using System.Collections.Generic;
using System.Linq;
using Transport;

class EnvelopesRouter(IEnumerable<IEnvelopeHandler> translators)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessage Translate(MessageContext messageContext)
    {
        // TODO: Is there any point in optimizing this to never hit the foreach if the translators list is empty.
        // https://stackoverflow.com/questions/45651325/performance-before-using-a-foreach-loop-check-if-the-list-is-empty
        foreach (var translator in translators)
        {
            if (translator.IsValidMessage(messageContext))
            {
                var translatedMessage = translator.CreateIncomingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions, messageContext.Body);
            }
        }

        return GetDefaultIncomingMessage(messageContext);
    }
}