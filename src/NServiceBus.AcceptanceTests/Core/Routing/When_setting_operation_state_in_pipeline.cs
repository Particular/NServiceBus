namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_setting_operation_state_in_pipeline : NServiceBusAcceptanceTest
    {
        const string ExistingSettingKey = "NSB.Testing.ExistingSettingKey";
        const string NewSettingKey = "NSB.Testing.NewSettingKey";

        [Test]
        public async Task Should_not_leak_up_to_sendoptions()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderReusingSendOptions>(c => c.When(async s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.GetMessageOperationExtensions().Set(ExistingSettingKey, true);
                    await s.Send(new SimpleMessage(), sendOptions);
                    await s.Send(new SimpleMessage(), sendOptions);
                }))
                .Done(c => c.MessagesReceived == 2)
                .Run();

            Assert.AreEqual(2, context.ExistingSettingValues.Count);
            Assert.IsTrue(context.ExistingSettingValues.All(x => x));
            Assert.IsTrue(context.NewSettingValues.Any(x => x.HasValue));
        }

        public class Context : ScenarioContext
        {
            public int MessagesReceived { get; set; }

            public List<bool> ExistingSettingValues { get; } = new List<bool>();

            public List<bool?> NewSettingValues { get; } = new List<bool?>();
        }

        public class SenderReusingSendOptions : EndpointConfigurationBuilder
        {
            public SenderReusingSendOptions() => EndpointSetup<DefaultServer>((c, r) =>
                c.Pipeline.Register(new OperationContextModifyingBehavior((Context)r.ScenarioContext), "modifies message operations context values"));

            public class SimeMessageHandler : IHandleMessages<SimpleMessage>
            {
                Context testContext;

                public SimeMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SimpleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessagesReceived++;
                    return Task.FromResult(0);
                }
            }

            public class OperationContextModifyingBehavior : Behavior<IOutgoingLogicalMessageContext>
            {
                Context testContext;

                public OperationContextModifyingBehavior(Context testContext) => this.testContext = testContext;

                public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    ContextBag messageOperationContext = context.GetMessageOperationExtensions();

                    testContext.ExistingSettingValues.Add(messageOperationContext.Get<bool>(ExistingSettingKey)); // should exist and set to true
                    messageOperationContext.Set(ExistingSettingKey, false);

                    messageOperationContext.TryGet(NewSettingKey, out bool? value); // should not exist
                    testContext.NewSettingValues.Add(value);
                    messageOperationContext.Set(NewSettingKey, true);

                    return next();
                }
            }
        }

        public class SimpleMessage : IMessage
        {
        }
    }
}