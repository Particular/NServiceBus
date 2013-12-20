namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Messages;

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


#pragma warning disable 618
            //first we override the default "extraction" behavior
            class MyOverride : PipelineOverride
            {
                public override void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
                {
                    behaviorList.Replace<ExtractLogicalMessagesBehavior, MyRawMessageHandler>();
                }
            }

            //and then we handle the physical message our self
            class MyRawMessageHandler:IBehavior<ReceivePhysicalMessageContext>
            {
                public Context Context { get; set; }

                public void Invoke(ReceivePhysicalMessageContext context, Action next)
                {
                    var transportMessage = context.PhysicalMessage;

                    Assert.True(transportMessage.Headers[Headers.EnclosedMessageTypes].Contains(typeof(SomeMessage).Name));

                    Context.GotTheRawMessage = true;
                }
            }
#pragma warning restore 618

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
