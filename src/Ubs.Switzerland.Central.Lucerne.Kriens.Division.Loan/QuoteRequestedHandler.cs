namespace Ubs.Switzerland.Central.Lucerne.Kriens.Division.Loan;

using LoanBroker.Messages.Messages;
using NServiceBus;

[Handler]
public class QuoteRequestedHandler : IHandleMessages<QuoteRequested>
{
    public Task Handle(QuoteRequested message, IMessageHandlerContext context) => throw new NotImplementedException();
}