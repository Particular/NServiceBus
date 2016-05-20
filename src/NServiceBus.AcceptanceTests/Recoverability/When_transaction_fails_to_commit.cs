namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_transaction_fails_to_commit : NServiceBusAcceptanceTest
    {
        [Ignore("enable when fixed")]
        [Test]
        public Task Should_move_message_to_error_queue()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b
                    .DoNotFailOnErrorMessages()
                    .CustomConfig((config, ctx) => config
                        .DefineCriticalErrorAction(criticalErrorContext =>
                        {
                            ctx.CriticalErrorOccured = true;
                            return criticalErrorContext.Stop();
                        }))
                    .When((session, context) => session.SendLocal(new SampleMessage())))
                .Done(c => c.FailedMessages.Any() || c.CriticalErrorOccured)
                .Repeat(r => r.For<AllDtcTransports>())
                .Should(c =>
                {
                    Assert.That(c.CriticalErrorOccured, Is.False);
                    Assert.That(c.FailedMessages, Has.Count.GreaterThanOrEqualTo(1));
                })
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool CriticalErrorOccured { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class FailingHandler : IHandleMessages<SampleMessage>
            {
                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    // handler enlists a failing transaction enlistment to the DTC transaction which will fail when commiting the transaction.
                    Transaction.Current.EnlistDurable(EnlistmentWhichEnforcesDtcEscalation.Id, new EnlistmentWhichEnforcesDtcEscalation(), EnlistmentOptions.None);

                    return Task.FromResult(0);
                }
            }
        }

        class EnlistmentWhichEnforcesDtcEscalation : IEnlistmentNotification
        {
            public static readonly Guid Id = Guid.NewGuid();

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                // fail during prepare
                preparingEnlistment.ForceRollback();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }

        class SampleMessage : ICommand
        {
        }
    }
}