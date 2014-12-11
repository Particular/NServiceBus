namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;

    public class When_using_callbacks_in_a_scaleout_scenario : NServiceBusAcceptanceTest
    {
        [Test]
        public void Each_client_should_have_a_unique_input_queue_to_avoid_processing_each_others_callbacks()
        {
            Scenario.Define(() => new Context{Id = Guid.NewGuid()})
                    .WithEndpoint<Client>(b => b.CustomConfig(c=>RuntimeEnvironment.MachineNameAction = () => "ClientA")
                        .Given((bus, context) => bus.Send(new MyRequest { Id = context.Id, Client = RuntimeEnvironment.MachineName })
                                                        .Register(r => context.CallbackAFired = true)))
                    .WithEndpoint<Client>(b => b.CustomConfig(c=>RuntimeEnvironment.MachineNameAction = () => "ClientB")
                        .Given((bus, context) => bus.Send(new MyRequest { Id = context.Id, Client = RuntimeEnvironment.MachineName })
                         .Register(r => context.CallbackBFired = true)))
                    .WithEndpoint<Server>()
                    .Done(c => c.ClientAGotResponse && c.ClientBGotResponse)
                    .Repeat(r =>r.For<AllBrokerTransports>()
                    )
                    .Should(c =>
                        {
                            Assert.True(c.CallbackAFired, "Callback on ClientA should fire");
                            Assert.True(c.CallbackBFired, "Callback on ClientB should fire");
                            Assert.False(c.ResponseEndedUpAtTheWrongClient, "One of the responses ended up at the wrong client");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public bool ClientAGotResponse { get; set; }

            public bool ClientBGotResponse { get; set; }

            public bool ResponseEndedUpAtTheWrongClient { get; set; }

            public bool CallbackAFired { get; set; }

            public bool CallbackBFired { get; set; }
        }

        public class Client : EndpointConfigurationBuilder
        {
            public Client()
            {
                EndpointSetup<DefaultServer>(c => Configure.ScaleOut(s => s.UseUniqueBrokerQueuePerMachine()))
                    .AddMapping<MyRequest>(typeof(Server));
            }

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyResponse response)
                {
                    if (Context.Id != response.Id)
                        return;

                    if (RuntimeEnvironment.MachineName == "ClientA")
                        Context.ClientAGotResponse = true;
                    else
                    {
                        Context.ClientBGotResponse = true;
                    }

                    if (RuntimeEnvironment.MachineName != response.Client)
                        Context.ResponseEndedUpAtTheWrongClient = true;
                }
            }
        }

        public class Server : EndpointConfigurationBuilder
        {
            public Server()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    if (Context.Id != request.Id)
                        return;


                    Bus.Reply(new MyResponse { Id = request.Id,Client = request.Client });
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
            public Guid Id { get; set; }

            public string Client { get; set; }
        }

        [Serializable]
        public class MyResponse : IMessage
        {
            public Guid Id { get; set; }

            public string Client { get; set; }
        }


    
    }
}
