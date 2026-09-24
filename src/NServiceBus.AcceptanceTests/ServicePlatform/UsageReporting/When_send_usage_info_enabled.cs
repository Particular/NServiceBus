namespace NServiceBus.AcceptanceTests.ServicePlatform.UsageReporting;

using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_send_usage_info_enabled : NServiceBusAcceptanceTest
{
    static readonly TimeSpan ReportingInterval = TimeSpan.FromSeconds(1);

    [Test]
    public async Task Reports_usage_info_to_platform()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ServiceControl>()
            .WithEndpoint<BusinessEndpoint>(b => b
                .When(async msg =>
                {
                    await msg.SendLocal(new BusinessMessage());
                    await msg.SendLocal(new BusinessMessage());
                    await Task.Delay(ReportingInterval);
                    await msg.SendLocal(new BusinessMessage());
                })
            )
            .Done(ctx => ctx.UsageReportedToServiceControl >= 3)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.UsageReportedToServiceControl, Is.EqualTo(3), "Endpoint should have processed 3 messages");
            Assert.That(context.MessagesReceived, Is.AtLeast(2), "Waited an entire reporting interval and should have got more than one update");
        }
    }

    [Test]
    public async Task Does_not_report_failed_messages()
    {
        var context = await Scenario.Define<Context>(ctx => ctx.FailOnNextMessage = true)
            .WithEndpoint<ServiceControl>()
            .WithEndpoint<BusinessEndpoint>(b => b
                            .When(async msg =>
                            {
                                await msg.SendLocal(new BusinessMessage());
                                await msg.SendLocal(new BusinessMessage());
                                await Task.Delay(ReportingInterval);
                                await msg.SendLocal(new BusinessMessage());
                            })
            )
            .Done(ctx => ctx.UsageReportedToServiceControl >= 3)
            .Run();

        Assert.That(context.UsageReportedToServiceControl, Is.EqualTo(context.MessagesProcessedByEndpoint));
    }


    public class Context : ScenarioContext
    {
        public bool FailOnNextMessage { get; set; }
        public int MessagesReceived { get; set; }
        public long UsageReportedToServiceControl { get; set; }
        public long MessagesProcessedByEndpoint { get; set; }
    }

    public class BusinessMessage : IMessage
    {
    }

    public class EndpointUsageReport
    {
        public required string EndpointName { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public long MessagesSuccessfullyProcessed { get; set; }

    }

    public class BusinessEndpoint : EndpointConfigurationBuilder
    {
        public BusinessEndpoint() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
            endpointConfiguration.Recoverability().Immediate(x => x.NumberOfRetries(1));

            endpointConfiguration.EnableServicePlatform().SendUsageInformation();
            // Override the default interval for the test
            endpointConfiguration.GetSettings().Set("UsageReporting.ReportingInterval", ReportingInterval);

            // HINT: The platform expects Json so this is ensuring that the feature is not using the configured serializer
            endpointConfiguration.UseSerialization<XmlSerializer>();
        });

        [Handler]
        public class BusinessMessageHandler(Context scenarioContext) : IHandleMessages<BusinessMessage>
        {
            public Task Handle(BusinessMessage message, IMessageHandlerContext context)
            {
                if (scenarioContext.FailOnNextMessage)
                {
                    scenarioContext.FailOnNextMessage = false;
                    throw new Exception("Boom");
                }

                scenarioContext.MessagesProcessedByEndpoint += 1;

                return Task.CompletedTask;
            }
        }
    }

    public class ServiceControl : EndpointConfigurationBuilder
    {
        public ServiceControl() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.Pipeline.Register(
                new FixEnclosedMessageTypeBehavior(),
                "Fixes the enclosed message type header to match the message type in the test"
            );

        }).CustomEndpointName("Particular.ServiceControl");

        class FixEnclosedMessageTypeBehavior : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                if (context.Message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageType))
                {
                    if (messageType.Contains(nameof(EndpointUsageReport)))
                    {
                        context.Message.Headers[Headers.EnclosedMessageTypes] = typeof(EndpointUsageReport).FullName;
                    }
                }
                return next();
            }
        }

        [Handler]
        public class EndpointUsageReportHandler(Context scenarioContext) : IHandleMessages<EndpointUsageReport>
        {
            public Task Handle(EndpointUsageReport message, IMessageHandlerContext context)
            {
                scenarioContext.MessagesReceived += 1;
                scenarioContext.UsageReportedToServiceControl += message.MessagesSuccessfullyProcessed;

                return Task.CompletedTask;
            }
        }
    }
}
