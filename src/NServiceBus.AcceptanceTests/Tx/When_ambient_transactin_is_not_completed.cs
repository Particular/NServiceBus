namespace NServiceBus.AcceptanceTests.Tx;

using System;
using System.Threading.Tasks;
using System.Transactions;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_ambient_transactin_is_not_completed : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_deliver_enlisted_message()
    {
        Requires.DtcSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<TransactionalEndpoint>(b => b.When(async session =>
            {
                using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    await session.Send(new MessageThatIsEnlisted());

                    // scope is not completed 
                }

                await session.Send(new MessageThatIsNotEnlisted());
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageThatIsEnlistedHandlerWasCalled, Is.False, "The transactional handler should not be called");
            Assert.That(context.MessageThatIsNotEnlistedHandlerWasCalled, Is.True, "The non-transactional handler should be called");
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageThatIsEnlistedHandlerWasCalled { get; set; }
        public bool MessageThatIsNotEnlistedHandlerWasCalled { get; set; }
    }

    public class TransactionalEndpoint : EndpointConfigurationBuilder
    {
        public TransactionalEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.LimitMessageProcessingConcurrencyTo(1);
                var routing = c.ConfigureRouting();
                routing.RouteToEndpoint(typeof(MessageThatIsEnlisted), typeof(TransactionalEndpoint));
                routing.RouteToEndpoint(typeof(MessageThatIsNotEnlisted), typeof(TransactionalEndpoint));
            });

        [Handler]
        public class MessageThatIsEnlistedHandler(Context testContext) : IHandleMessages<MessageThatIsEnlisted>
        {
            public Task Handle(MessageThatIsEnlisted messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.MessageThatIsEnlistedHandlerWasCalled = true;
                testContext.MarkAsFailed(new InvalidOperationException($"'{nameof(MessageThatIsEnlistedHandler)}' should not be called because the surrounding transaction was rolled back."));
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class MessageThatIsNotEnlistedHandler(Context testContext) : IHandleMessages<MessageThatIsNotEnlisted>
        {
            public Task Handle(MessageThatIsNotEnlisted messageThatIsNotEnlisted, IMessageHandlerContext context)
            {
                testContext.MessageThatIsNotEnlistedHandlerWasCalled = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageThatIsEnlisted : ICommand;

    public class MessageThatIsNotEnlisted : ICommand;
}