namespace LoanBroker.Handlers;

using Messages.Messages;
using NServiceBus;

[Handler]
public class CreditScoreEnricher : IHandleMessages<FindBestLoanWithScore>
{

    public Task Handle(FindBestLoanWithScore message, IMessageHandlerContext context) => throw new NotImplementedException();
}