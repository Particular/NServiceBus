namespace NServiceBus.AcceptanceTests.PipelineExt
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    //This is a demo on how the pipeline overrides can be used to create endpoints that doesn't deserialize incoming messages and there by
    // allows the user to handle the raw transport message. This replaces the old feature on the UnicastBus where SkipDeserialization could be set to tru
    public class SkipDeserialization : NServiceBusAcceptanceTest
    {
        [Test]
        public void RunDemo()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<NonSerializingEndpoint>(
                        b => b.Given(bus => bus.SendLocal(new SomeMessage())))
                    .Done(c => c.GotTheRawMessage)
                    .Run();
        }

        public class NonSerializingEndpoint : EndpointConfigurationBuilder
        {
            public NonSerializingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            //first we override the default "extraction" behavior
            class MyOverride : INeedInitialization
            {
                public void Customize(BusConfiguration configuration)
                {
                    configuration.Pipeline.Replace(WellKnownStep.DeserializeMessages, typeof(MyRawMessageHandler));
                }
            }

            //and then we handle the physical message our self
            class MyRawMessageHandler:IBehavior<IncomingContext>
            {
                public Context Context { get; set; }

                public void Invoke(IncomingContext context, Action next)
                {
                    var transportMessage = context.PhysicalMessage;

                    Assert.True(transportMessage.Headers[Headers.EnclosedMessageTypes].Contains(typeof(SomeMessage).Name));

                    Context.GotTheRawMessage = true;
                }
            }

            class ThisHandlerWontGetInvoked:IHandleMessages<SomeMessage>
            {
                public void Handle(SomeMessage message)
                {
                    Assert.Fail();
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool GotTheRawMessage { get; set; }
        }

        [Serializable]
        public class SomeMessage : ICommand
        {

        }
    }


}
