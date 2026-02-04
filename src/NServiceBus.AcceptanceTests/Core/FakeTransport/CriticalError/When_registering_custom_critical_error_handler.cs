namespace NServiceBus.AcceptanceTests.Core.FakeTransport.CriticalError;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NUnit.Framework;

public class When_registering_custom_critical_error_handler : NServiceBusAcceptanceTest
{
    [Test]
    public void Critical_error_should_be_raised_inside_delegate()
    {
        Context context = null;
        var exception = Assert.CatchAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>(c => context = c)
                .WithEndpoint<EndpointWithLocalCallback>(b => b.When(session => session.SendLocal(new MyRequest())))
                .Run();
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Message, Does.StartWith("Startup task failed to complete."));
            Assert.That(exception.Message, Is.EqualTo("ExceptionInBusStarts"));
        }
    }

    public class Context : ScenarioContext
    {
        public string Message { get; set; }
    }

    public class EndpointWithLocalCallback : EndpointConfigurationBuilder
    {
        public EndpointWithLocalCallback() =>
            EndpointSetup<DefaultServer>(builder =>
            {
                var fakeTransport = new FakeTransport();
                fakeTransport.RaiseCriticalErrorOnReceiverStart(new AggregateException("Startup task failed to complete.", new InvalidOperationException("ExceptionInBusStarts")));
                builder.UseTransport(fakeTransport);

                builder.DefineCriticalErrorAction((errorContext, _) =>
                {
                    var aggregateException = (AggregateException)errorContext.Exception;
                    var context = builder.GetSettings().Get<Context>();
                    context.Message = errorContext.Error;

                    context.MarkAsFailed(aggregateException.GetBaseException());
                    return Task.CompletedTask;
                });
            });

        [Handler]
        public class MyRequestHandler : IHandleMessages<MyRequest>
        {
            public Task Handle(MyRequest request, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class MyRequest : IMessage;
}