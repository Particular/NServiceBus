namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    public class When_disabling_message_processing_feature : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_process_the_raw_message_via_spcialized_behavior()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocalAsync(new MyMessage())))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.IsTrue(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.DisableMainProcessingPipeline();
                    config.Pipeline.Register("RawMessageProcessingBehavior", typeof(RawMessageProcessingBehavior), "RawMessageProcessingBehavior");
                });
            }

            public class RawMessageProcessingBehavior : PipelineTerminator<TransportReceiveContext>
            {
                public Context Context { get; set; }

                protected override Task Terminate(TransportReceiveContext context)
                {
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
        
        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}
