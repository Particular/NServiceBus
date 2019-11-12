namespace NServiceBus.Pipeline.Outgoing
{
    using System;
    using ObjectBuilder;
    using Transport;

    class SendComponent
    {
        SendComponent(PipelineComponent pipelineComponent)
        {
            this.pipelineComponent = pipelineComponent;
        }

        public static SendComponent Initialize(Configuration configuration, PipelineComponent pipelineComponent)
        {
            var pipelineSettings = pipelineComponent.PipelineSettings;

            pipelineSettings.Register(new AttachSenderRelatedInfoOnMessageBehavior(), "Makes sure that outgoing messages contains relevant info on the sending endpoint.");

            var newIdGenerator = GetIdStrategy(configuration);
            pipelineSettings.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(newIdGenerator), "Adds related to and conversation id headers to outgoing messages");

            if (configuration.StaticHeaders != null)
            {
                pipelineSettings.Register("ApplyStaticHeaders", new ApplyStaticHeadersBehavior(configuration.StaticHeaders), "Applies static headers to outgoing messages");
            }

            pipelineSettings.Register(new OutgoingPhysicalToRoutingConnector(), "Starts the message dispatch pipeline");
            pipelineSettings.Register(new RoutingToDispatchConnector(), "Decides if the current message should be batched or immediately be dispatched to the transport");
            pipelineSettings.Register(new BatchToDispatchConnector(), "Passes batched messages over to the immediate dispatch part of the pipeline");
            pipelineSettings.Register(b => new ImmediateDispatchTerminator(b.Build<IDispatchMessages>()), "Hands the outgoing messages over to the transport for immediate delivery");

            return new SendComponent(pipelineComponent);
        }

        public IMessageSession CreateMessageSession(IBuilder builder)
        {
            messageSession = new MessageSession(pipelineComponent.CreateRootContext(builder));
            return messageSession;
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

        PipelineComponent pipelineComponent;
        IMessageSession messageSession;

        internal class Configuration
        {
            public Func<ConversationIdStrategyContext, ConversationId> CustomConversationIdStrategy { get; set; }
            public CurrentStaticHeaders StaticHeaders { get; set; }
        }
    }
}