namespace LoanBroker.Client.Handlers;

using LoanBroker.Messages.Messages;
using NServiceBus;

[Handler]
public class NoQuotesReceivedHandler : IHandleMessages<NoQuotesReceived>
{
    public Task Handle(NoQuotesReceived message, IMessageHandlerContext context) => throw new NotImplementedException();
}