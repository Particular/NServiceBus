namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class MessingAround : NServiceBusAcceptanceTest
    {
        [Test, Ignore("Just added for convenience")]
        public void CanExecuteEntirePipelineInRealisticScenario()
        {
            Scenario.Define(() => new When_sending_a_message_to_another_endpoint.Context { Id = Guid.NewGuid() })
                    .WithEndpoint<When_sending_a_message_to_another_endpoint.Sender>(b => b.Given((bus, context) => bus.Send(new When_sending_a_message_to_another_endpoint.MyMessage { Id = context.Id })))
                    .WithEndpoint<When_sending_a_message_to_another_endpoint.Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Serializers.Xml)
                                  .For(Builders.Windsor)
                    )
                    .Should(c =>
                    {
                        Assert.True(c.WasCalled, "The message handler should be called");
                        Assert.AreEqual(1, c.TimesCalled, "The message handler should only be invoked once");
                        Assert.AreEqual(Environment.MachineName, c.ReceivedHeaders[Headers.OriginatingMachine], "The sender should attach the machine name as a header");
                        Assert.True(c.ReceivedHeaders[Headers.OriginatingEndpoint].Contains("Sender"), "The sender should attach its endpoint name as a header");
                        Assert.AreEqual(Environment.MachineName, c.ReceivedHeaders[Headers.ProcessingMachine], "The receiver should attach the machine name as a header");
                        Assert.True(c.ReceivedHeaders[Headers.ProcessingEndpoint].Contains("Receiver"), "The receiver should attach its endpoint name as a header");
                    })
                    .Run();
        }
    }
}