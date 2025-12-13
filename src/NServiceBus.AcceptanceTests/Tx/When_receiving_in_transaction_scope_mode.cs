namespace NServiceBus.AcceptanceTests.Tx;

using System;
using System.Threading.Tasks;
using System.Transactions;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_receiving_in_transaction_scope_mode : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_enlist_in_dtc_transaction()
    {
        Requires.DtcSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<DTCEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
            .Run();

        Assert.That(context.DtcTransactionPresent, Is.True, "There should exists a DTC tx");
    }


    public class Context : ScenarioContext
    {

        public bool DtcTransactionPresent { get; set; }
    }

    public class DTCEndpoint : EndpointConfigurationBuilder
    {
        public DTCEndpoint() => EndpointSetup<DefaultServer>();

        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage messageThatIsEnlisted, IMessageHandlerContext context)
            {
                Transaction.Current.EnlistDurable(FakePromotableResourceManager.ResourceManagerId, new FakePromotableResourceManager(), EnlistmentOptions.None);
                testContext.DtcTransactionPresent = Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : ICommand;
}
