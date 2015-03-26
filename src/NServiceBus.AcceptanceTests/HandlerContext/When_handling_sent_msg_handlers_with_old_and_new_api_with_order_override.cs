namespace NServiceBus.AcceptanceTests.HandlerContext
{
    namespace NServiceBus.AcceptanceTests.HandlerContext
    {
        using System;
        using System.Collections.Generic;
        using global::NServiceBus.AcceptanceTesting;
        using global::NServiceBus.AcceptanceTests.EndpointTemplates;
        using NUnit.Framework;

        public class When_handling_sent_msg_handlers_with_old_and_new_api_with_order_override : NServiceBusAcceptanceTest
        {
            [Test]
            public void Should_call_old_and_new_style_according_to_defined_order()
            {
                var context = new Context { Id = Guid.NewGuid() };

                Scenario.Define(context)
                        .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage
                        {
                            Id = context.Id
                        })))
                        .Done(c => c.HandlersExecuted.Count == 2)
                        .Run();

                Verify.AssertOldAndNewStyleHandlersAreInvoked(context);
                Verify.AssertNewStyleHandlerIsInvokedFirst(context);
                Verify.AssertOldStyleHandlerIsInvokedSecond(context);
            }

            static class Verify
            {
                public static void AssertNewStyleHandlerIsInvokedFirst(Context context)
                {
                    Assert.AreEqual("NewStyle", context.HandlersExecuted[0]);
                }

                public static void AssertOldStyleHandlerIsInvokedSecond(Context context)
                {
                    Assert.AreEqual("OldStyle", context.HandlersExecuted[1]);
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
            }

            public class Endpoint : EndpointConfigurationBuilder
            {
                public Endpoint()
                {
                    EndpointSetup<DefaultServer>()
                        .AddMapping<MyMessage>(typeof(Endpoint));
                }
            }

            public class OrderOverride : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.SpecifyFirst<NewStyleHandlerApiHandler>();
                }
            }

            [Serializable]
            public class MyMessage : IMessage
            {
                public Guid Id { get; set; }
            }

            public class OldHandlerApiHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage message)
                {
                    if (Context.Id != message.Id)
                        return;

                    Context.HandlersExecuted.Add("OldStyle");
                }
            }

            public class NewStyleHandlerApiHandler : IConsumeMessage<MyMessage>
            {
                // TODO: Could we maybe also leverage the context object to pass in that dependency?
                public Context Context { get; set; }

                public void Handle(MyMessage message, IConsumeMessageContext messageContext)
                {
                    if (Context.Id != message.Id)
                        return;

                    Context.HandlersExecuted.Add("NewStyle");
                }
            }

        }
    }
}