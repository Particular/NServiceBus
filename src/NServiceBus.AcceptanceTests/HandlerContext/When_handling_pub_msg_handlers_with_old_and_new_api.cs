namespace NServiceBus.AcceptanceTests.HandlerContext
{
    namespace NServiceBus.AcceptanceTests.HandlerContext
    {
        using System;
        using System.Collections.Generic;
        using global::NServiceBus.AcceptanceTesting;
        using global::NServiceBus.AcceptanceTests.EndpointTemplates;
        using global::NServiceBus.AcceptanceTests.PubSub;
        using global::NServiceBus.Features;
        using NUnit.Framework;

        public class When_handling_pub_msg_handlers_with_old_and_new_api : NServiceBusAcceptanceTest
        {
            [Test]
            public void Should_call_old_and_new_style_according_to_defined_order()
            {
                var context = new Context { Id = Guid.NewGuid() };

                Scenario.Define(context)
                    .WithEndpoint<Publisher>(b =>
                        b.When(c => c.SubscriberSubscribed, (bus, c) =>
                        {
                            c.AddTrace("Subscriber is subscribed, going to publish MyEvent");
                            bus.Publish(new MyEvent { Id = c.Id });
                        })
                     )
                    .WithEndpoint<Subscriber>(b => b.Given((bus, c) =>
                    {
                        bus.Subscribe<IMyEvent>();
                        if (context.HasNativePubSubSupport)
                        {
                            context.SubscriberSubscribed = true;
                            context.AddTrace("Subscriber is now subscribed (at least we have asked the broker to be subscribed)");
                        }
                        else
                        {
                            context.AddTrace("Subscriber has now asked to be subscribed to MyEvent");
                        }
                    }))
                    .Done(c => c.HandlersExecuted.Count == 2)
                    .Run();

                Verify.AssertOldAndNewStyleHandlersAreInvoked(context);
                Verify.AssertOldStyleHandlerIsInvokedFirst(context);
                Verify.AssertNewStyleHandlerIsInvokedSecond(context);
            }

            static class Verify
            {
                public static void AssertNewStyleHandlerIsInvokedSecond(Context context)
                {
                    Assert.AreEqual("NewStyle", context.HandlersExecuted[1]);
                }

                public static void AssertOldStyleHandlerIsInvokedFirst(Context context)
                {
                    Assert.AreEqual("OldStyle", context.HandlersExecuted[0]);
                }

                public static void AssertOldAndNewStyleHandlersAreInvoked(Context context)
                {
                    Assert.AreEqual(2, context.HandlersExecuted.Count, "Old and new style handlers should be invoked");
                }
            }

            public class Context : ScenarioContext
            {
                public Context()
                {
                    HandlersExecuted = new List<string>();
                }

                public List<string> HandlersExecuted { get; private set; }
                public Guid Id { get; set; }
                public bool SubscriberSubscribed { get; set; }
            }

            public class Publisher : EndpointConfigurationBuilder
            {
                public Publisher()
                {
                    EndpointSetup<DefaultPublisher>(b =>
                    {
                        b.OnEndpointSubscribed<Context>((s, context) =>
                        {
                            if (s.SubscriberReturnAddress.Contains("Subscriber"))
                            {
                                context.SubscriberSubscribed = true;
                                context.AddTrace("Subscriber is now subscribed");
                            }
                        });
                        b.DisableFeature<AutoSubscribe>();
                    });
                }
            }

            public class Subscriber : EndpointConfigurationBuilder
            {
                public Subscriber()
                {
                    EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                        .AddMapping<IMyEvent>(typeof(Publisher));
                }

                public class OldHandlerApiHandler : IHandleMessages<IMyEvent>
                {
                    public Context Context { get; set; }

                    public void Handle(IMyEvent message)
                    {
                        if (Context.Id != message.Id)
                            return;

                        Context.HandlersExecuted.Add("OldStyle");
                    }
                }

                public class NewStyleHandlerApiHandler : IProcessEvents<IMyEvent>
                {
                    // TODO: Could we maybe also leverage the context object to pass in that dependency?
                    public Context Context { get; set; }

                    public void Handle(IMyEvent message, IEventContext context)
                    {
                        if (Context.Id != message.Id)
                            return;

                        Context.HandlersExecuted.Add("NewStyle");
                    }
                }
            }

            public interface IMyEvent : IEvent
            {
                Guid Id { get; set; }
            }

            [Serializable]
            public class MyEvent : IMyEvent
            {
                public Guid Id { get; set; }
            }
        }
    }
}