namespace NServiceBus.Pipeline.Outgoing
{
    using System;
    using Transport;

    class SendComponent
    {
        SendComponent()
        {
        }

        public static SendComponent Initialize(Configuration configuration, PipelineSettings pipelineSettings, HostingComponent hostingComponent, RoutingComponent routingComponent)
        {
            pipelineSettings.Register(new AttachSenderRelatedInfoOnMessageBehavior(), "Makes sure that outgoing messages contains relevant info on the sending endpoint.");

            var newIdGenerator = GetIdStrategy(configuration);
            pipelineSettings.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(newIdGenerator), "Adds related to and conversation id headers to outgoing messages");
            pipelineSettings.Register("AuditHostInformation", new AuditHostInformationBehavior(hostingComponent.HostInformation, hostingComponent.EndpointName), "Adds audit host information");
            pipelineSettings.Register("AddHostInfoHeaders", new AddHostInfoHeadersBehavior(hostingComponent.HostInformation, hostingComponent.EndpointName), "Adds host info headers to outgoing headers");

            if (configuration.StaticHeaders != null)
            {
                pipelineSettings.Register("ApplyStaticHeaders", new ApplyStaticHeadersBehavior(configuration.StaticHeaders), "Applies static headers to outgoing messages");
            }

            pipelineSettings.Register("UnicastSendRouterConnector", new SendConnector(routingComponent.UnicastSendRouter), "Determines how the message being sent should be routed");
            pipelineSettings.Register("UnicastReplyRouterConnector", new ReplyConnector(), "Determines how replies should be routed");

            if (routingComponent.EnforceBestPractices)
            {
                EnableBestPracticeEnforcement(pipelineSettings, routingComponent.MessageValidator);
            }

            pipelineSettings.Register(new OutgoingPhysicalToRoutingConnector(), "Starts the message dispatch pipeline");
            pipelineSettings.Register(new RoutingToDispatchConnector(), "Decides if the current message should be batched or immediately be dispatched to the transport");
            pipelineSettings.Register(new BatchToDispatchConnector(), "Passes batched messages over to the immediate dispatch part of the pipeline");
            pipelineSettings.Register(b => new ImmediateDispatchTerminator(b.Build<IDispatchMessages>()), "Hands the outgoing messages over to the transport for immediate delivery");

            return new SendComponent();
        }

        static Func<IOutgoingLogicalMessageContext, string> GetIdStrategy(Configuration configuration)
        {
            if (configuration.CustomConversationIdStrategy != null)
            {
                return WrapUserDefinedInvocation(configuration.CustomConversationIdStrategy);
            }

            return _ => CombGuid.Generate().ToString();
        }

        static internal Func<IOutgoingLogicalMessageContext, string> WrapUserDefinedInvocation(Func<ConversationIdStrategyContext, ConversationId> userDefinedIdGenerator)
        {
            return context =>
            {
                ConversationId customConversationId;

                try
                {
                    customConversationId = userDefinedIdGenerator(new ConversationIdStrategyContext(context.Message, context.Headers));
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed to execute the custom conversation ID strategy defined using '{nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdStrategy)}'.", exception);
                }

                if (customConversationId == null)
                {
                    throw new Exception($"The custom conversation ID strategy defined using '{nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdStrategy)}' returned null. The custom strategy must return either '{nameof(ConversationId)}.{nameof(ConversationId.Custom)}(customValue)' or '{nameof(ConversationId)}.{nameof(ConversationId.Default)}'");
                }

                return customConversationId.Value;
            };
        }

        static void EnableBestPracticeEnforcement(PipelineSettings pipeline, Validations validations)
        {
            pipeline.Register(
                "EnforceSendBestPractices",
                new EnforceSendBestPracticesBehavior(validations),
                "Enforces send messaging best practices");

            pipeline.Register(
                "EnforceReplyBestPractices",
                new EnforceReplyBestPracticesBehavior(validations),
                "Enforces reply messaging best practices");

            pipeline.Register(
                "EnforcePublishBestPractices",
                new EnforcePublishBestPracticesBehavior(validations),
                "Enforces publish messaging best practices");

            pipeline.Register(
                "EnforceSubscribeBestPractices",
                new EnforceSubscribeBestPracticesBehavior(validations),
                "Enforces subscribe messaging best practices");

            pipeline.Register(
                "EnforceUnsubscribeBestPractices",
                new EnforceUnsubscribeBestPracticesBehavior(validations),
                "Enforces unsubscribe messaging best practices");
        }

        internal class Configuration
        {
            public Func<ConversationIdStrategyContext, ConversationId> CustomConversationIdStrategy { get; set; }
            public CurrentStaticHeaders StaticHeaders { get; set; }
        }
    }
}