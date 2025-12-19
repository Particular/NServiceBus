// See https://aka.ms/new-console-template for more information

using NServiceBus;

var endpointConfiguration = new EndpointConfiguration("LoanBroker");
endpointConfiguration.UsePersistence<LearningPersistence>();
endpointConfiguration.UseTransport<LearningTransport>();

var handlersRegistry = endpointConfiguration.Handlers;
handlersRegistry.LoanBroker.AddAll();
handlersRegistry.Client.AddAll();
handlersRegistry.Ubs_Switzerland_Central_Lucerne_Kriens_Division_Loan.AddAll();
