// See https://aka.ms/new-console-template for more information

using NServiceBus;

var endpointConfiguration = new EndpointConfiguration("LoanBroker");
endpointConfiguration.UsePersistence<LearningPersistence>();
endpointConfiguration.UseTransport<LearningTransport>();

var handlersRegistry = endpointConfiguration.Handlers;
handlersRegistry.LoanBrokerAssembly.LoanBroker.Handlers.AddAll();
handlersRegistry.ClientAssembly.AddAll();
handlersRegistry.UbsAssembly.Ubs.AddAll();
handlersRegistry.UbsSwitzerlandCentralLucerneKriensDivisionLoanAssembly.Ubs.Switzerland.Central.Lucerne.Kriens.Division.Loan.AddQuoteRequested();
