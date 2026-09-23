namespace NServiceBus.AcceptanceTests.ServicePlatform;

using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NServiceBus.ServicePlatform;
using NUnit.Framework;

public partial class When_serviceplatform_enabled : NServiceBusAcceptanceTest
{
    static readonly Guid BusinessId = Guid.NewGuid();
    const string CustomServiceControlName = "My.ServiceControl";

    static readonly TestCaseData[] TestCases = [
        .. from sendOnly in new[] { true, false }
           from customServiceControlName in new[] { true, false }
           let sendOnlyParam = sendOnly ? "Send only endpoint" : "Full endpoint"
           let customServiceControlParam = customServiceControlName ? "Custom ServiceControl" : "Default ServiceControl"
           select new TestCaseData(sendOnly, customServiceControlName)
            .SetName($"{{m}} {sendOnlyParam} -> {customServiceControlParam}")
    ];

    [Test, TestCaseSource(nameof(TestCases))]
    public async Task Can_send_message_to_platform(
        bool sendOnly,
        bool customServiceControlName
    )
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint(customServiceControlName ? new PrimaryServiceControl(CustomServiceControlName) : new PrimaryServiceControl(), _ => { })
            .WithEndpoint<BusinessEndpoint>(b => b.CustomConfig(endpointConfig =>
            {
                endpointConfig.EnableFeature<SendMessageToPlatformFeature>();
                // HINT: The ServicePlatform channel should always use the Json Serializer and ignore this
                _ = endpointConfig.UseSerialization<XmlSerializer>();

                if (sendOnly)
                {
                    endpointConfig.SendOnly();
                }

                var servicePlatform = endpointConfig.EnableServicePlatform();
                if (customServiceControlName)
                {
                    _ = servicePlatform.PrimaryInstanceQueue(CustomServiceControlName);
                }
            }))
            .Done(ctx => ctx.Done)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.BusinessId, Is.EqualTo(BusinessId));
            if (!sendOnly)
            {
                var expectedName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(BusinessEndpoint));
                Assert.That(context.ReplyToAddress, Is.EqualTo(expectedName));
            }
        }
    }

    public class Context : ScenarioContext
    {
        public Guid BusinessId { get; set; }
        public string ReplyToAddress { get; set; }
        public bool Done { get; set; }
    }

    public class PlatformMessage : IMessage
    {
        public required Guid BusinessId { get; init; }
    }

    [JsonSerializable(typeof(PlatformMessage))]
    partial class PlatformMessageJsonContext : JsonSerializerContext;

    class SendMessageToPlatformFeature : Feature
    {
        public SendMessageToPlatformFeature()
        {
            DependsOn("NServiceBus.ServicePlatformFeature");
        }

        protected override void Setup(FeatureConfigurationContext context)
            => context.RegisterStartupTask(serviceProvider =>
            {
                var servicePlatform = serviceProvider.GetRequiredService<ServicePlatformConnection>();
                return new SendMessageToPlatformFeatureStartupTask(servicePlatform.PrimaryInstance);
            });

        class SendMessageToPlatformFeatureStartupTask(ServicePlatformChannel channel) : FeatureStartupTask
        {
            readonly ServicePlatformSender<PlatformMessage> sender = channel.CreateSender(PlatformMessageJsonContext.Default.PlatformMessage);

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                => sender.Send(new PlatformMessage { BusinessId = BusinessId }, cancellationToken);
            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }

    public class BusinessEndpoint : EndpointConfigurationBuilder
    {
        public BusinessEndpoint() => EndpointSetup<DefaultServer>();
    }

    public class PrimaryServiceControl : EndpointConfigurationBuilder
    {
        public PrimaryServiceControl(string instanceName = "Particular.ServiceControl")
            => EndpointSetup<DefaultServer>(endpointConfiguration => endpointConfiguration.UseSerialization<SystemJsonSerializer>())
                .CustomEndpointName(instanceName);

        [Handler]
        public class PlatformMessageHandler(Context scenarioContext) : IHandleMessages<PlatformMessage>
        {
            public Task Handle(PlatformMessage message, IMessageHandlerContext context)
            {
                scenarioContext.BusinessId = message.BusinessId;
                if (context.MessageHeaders.TryGetValue(Headers.ReplyToAddress, out var replyToAddress))
                {
                    scenarioContext.ReplyToAddress = replyToAddress;
                }
                scenarioContext.Done = true;
                return Task.CompletedTask;
            }
        }
    }
}
