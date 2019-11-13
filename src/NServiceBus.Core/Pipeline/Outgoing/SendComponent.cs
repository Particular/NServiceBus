namespace NServiceBus
{
    using System;
    using Pipeline;
    using Transport;

    class SendComponent
    {
        SendComponent()
        {
        }

        public static SendComponent Initialize(Configuration configuration, PipelineSettings pipelineSettings, HostingComponent hostingComponent, RoutingComponent routingComponent)
        {
            pipelineSettings.Register(new AttachSenderRelatedInfoOnMessageBehavior(), "Makes sure that outgoing messages contains relevant info on the sending endpoint.");
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
            public CurrentStaticHeaders StaticHeaders { get; set; }
        }
    }
}