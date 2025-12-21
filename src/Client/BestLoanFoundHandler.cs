namespace LoanBroker.Client.Handlers;

using LoanBroker.Messages.Messages;
using NServiceBus;

[Handler]
public class BestLoanFoundHandler : IHandleMessages<BestLoanFound>
{
    public Task Handle(BestLoanFound message, IMessageHandlerContext context) => throw new NotImplementedException();
}