namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Messages;

    // Repro for #SB-191
    public class SkipSerialization : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_timeouts_properly()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<NonSerializingEndpoint>(
                        b => b.Given(bus => bus.SendLocal(new SomeMessage())))
                    .Done(c => c.GotTheRawMessage)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotTheRawMessage { get; set; }
        }

        public class NonSerializingEndpoint : EndpointConfigurationBuilder
        {
            public NonSerializingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }


#pragma warning disable 618
            //first we override the default deserialization behavior
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

        [Serializable]
        public class SomeMessage : ICommand
        {

        }
    }


}
