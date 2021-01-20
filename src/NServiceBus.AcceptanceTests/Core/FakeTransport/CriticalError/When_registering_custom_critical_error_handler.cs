namespace NServiceBus.AcceptanceTests.Core.FakeTransport.CriticalError
{
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
        public async Task Critical_error_should_be_raised_inside_delegate()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithLocalCallback>(b => b.When(
                    session => session.SendLocal(new MyRequest())))
                .Done(c => c.ExceptionReceived)
                .Run();

            Assert.True(context.Message.StartsWith("Startup task failed to complete."));
            Assert.AreEqual("ExceptionInBusStarts", context.Exception.Message);
        }

        public class Context : ScenarioContext
        {
            public Exception Exception { get; set; }
            public string Message { get; set; }
            public bool ExceptionReceived { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    var fakeTransport = new FakeTransport();
                    fakeTransport.RaiseCriticalErrorOnReceiverStart(new AggregateException("Startup task failed to complete.", new InvalidOperationException("ExceptionInBusStarts")));
                    builder.UseTransport(fakeTransport);

                    builder.DefineCriticalErrorAction(errorContext =>
                    {
                        var aggregateException = (AggregateException)errorContext.Exception;
                        var context = builder.GetSettings().Get<Context>();
                        context.Exception = aggregateException.InnerExceptions.First();
                        context.Message = errorContext.Error;
                        context.ExceptionReceived = true;
                        return Task.FromResult(0);
                    });
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}