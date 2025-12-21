namespace LoanBroker.Client.Handlers;

using LoanBroker.Messages.Messages;
using NServiceBus;

[Handler]
public class QuoteRequestRefusedHandler : IHandleMessages<QuoteRequestRefused>
{
    public Task Handle(QuoteRequestRefused message, IMessageHandlerContext context) => throw new NotImplementedException();
}