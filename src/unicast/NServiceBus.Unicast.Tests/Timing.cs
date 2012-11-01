namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using Monitoring;
    using NUnit.Framework;
    using Rhino.Mocks;
    using UnitOfWork;

    [TestFixture]
    public class When_processing_a_message_with_timing_turned_on : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_processing_headers()
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => new ProcessingStatistics{Bus = bus});

            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();

            ReceiveMessage(receivedMessage);

            Assert.True(bus.CurrentMessageContext.Headers.ContainsKey("NServiceBus.ProcessingStarted"));
            Assert.True(bus.CurrentMessageContext.Headers.ContainsKey("NServiceBus.ProcessingEnded"));
        }
    }

    [TestFixture]
    public class When_sending_a_message_with_timing_turned_on : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_time_sent_header()
        {
            RegisterMessageType<CommandMessage>();

            bus.Send(new CommandMessage());
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey("NServiceBus.TimeSent")), Arg<Address>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_processing_messages_and_a_endpoint_sla_is_set : using_the_unicastbus
    {
        [Test,Explicit("Timing related test")]
        public void Should_calculate_the_time_to_breach_sla()
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => new ProcessingStatistics
                                                               {
                                                                   Bus = bus,
                                                                   EstimatedTimeToSLABreachCalculator = SLABreachCalculator
                                                               });
            var endpointSLA = TimeSpan.FromSeconds(60);

            SLABreachCalculator.Initialize(endpointSLA);
            var now = DateTime.UtcNow;

            RegisterMessageType<EventMessage>();
            
            double secondsUntilSlaIsBreached = 0;


            SLABreachCalculator.SetCounterAction = d => secondsUntilSlaIsBreached = d;

            
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            receivedMessage.Headers[Headers.TimeSent] = now.ToWireFormattedString();

            
            ReceiveMessage(receivedMessage);

            //need at least 2 messages to enable us to count
            Assert.AreEqual(0,secondsUntilSlaIsBreached);

            receivedMessage.Headers[Headers.TimeSent] = now.AddSeconds(-0.5).ToWireFormattedString();
            ReceiveMessage(receivedMessage);

            //this should be rougly 2.1 since it takes 0.02 second to process both messages and the CT delta is 0.5 (since we fake a 0.5 delay)
            // 0,5/0,02 = 25 => means that the SLA will be busted in aprox 2 seconds
            var secondsUntilSlaIsBreached_2 = secondsUntilSlaIsBreached;

            Assert.Greater(secondsUntilSlaIsBreached_2, 1.5);
            Assert.Less(secondsUntilSlaIsBreached_2, 3.0);


            receivedMessage.Headers[Headers.TimeSent] = now.AddSeconds(-0.5).ToWireFormattedString();
            ReceiveMessage(receivedMessage);
            var secondsUntilSlaIsBreached_3 = secondsUntilSlaIsBreached;

            Assert.Less(secondsUntilSlaIsBreached_2,secondsUntilSlaIsBreached_3);
        }
    }
}