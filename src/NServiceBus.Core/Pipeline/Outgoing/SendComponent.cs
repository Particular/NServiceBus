namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageInterfaces;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    class SendComponent
    {
        SendComponent(IMessageMapper messageMapper, TransportInfrastructure transportInfrastructure)
        {
            this.messageMapper = messageMapper;
            this.transportInfrastructure = transportInfrastructure;
        }

        public static SendComponent Initialize(PipelineSettings pipelineSettings, HostingComponent.Configuration hostingConfiguration, RoutingComponent routingComponent, IMessageMapper messageMapper, TransportSeam transportSeam)
        {
            pipelineSettings.Register(new AttachSenderRelatedInfoOnMessageBehavior(), "Makes sure that outgoing messages contains relevant info on the sending endpoint.");
            pipelineSettings.Register("AuditHostInformation", new AuditHostInformationBehavior(hostingConfiguration.HostInformation, hostingConfiguration.EndpointName), "Adds audit host information");
            pipelineSettings.Register("AddHostInfoHeaders", new AddHostInfoHeadersBehavior(hostingConfiguration.HostInformation, hostingConfiguration.EndpointName), "Adds host info headers to outgoing headers");

            pipelineSettings.Register("UnicastSendRouterConnector", new SendConnector(routingComponent.UnicastSendRouter), "Determines how the message being sent should be routed");
            pipelineSettings.Register("UnicastReplyRouterConnector", new ReplyConnector(), "Determines how replies should be routed");


            pipelineSettings.Register(new OutgoingPhysicalToRoutingConnector(), "Starts the message dispatch pipeline");
            pipelineSettings.Register(new RoutingToDispatchConnector(), "Decides if the current message should be batched or immediately be dispatched to the transport");
            pipelineSettings.Register(new BatchToDispatchConnector(), "Passes batched messages over to the immediate dispatch part of the pipeline");
            pipelineSettings.Register(b => new ImmediateDispatchTerminator(b.Build<IDispatchMessages>()), "Hands the outgoing messages over to the transport for immediate delivery");

            var sendComponent = new SendComponent(messageMapper, transportSeam.TransportInfrastructure);

            hostingConfiguration.Container.ConfigureComponent(() => sendComponent.GetDispatcher(), DependencyLifecycle.SingleInstance);

            return sendComponent;
        }

        [ObsoleteEx(
            Message = "Change transport infrastructure to configure the send infrastructure at component initialization time",
            RemoveInVersion = "8")]
        public void ConfigureSendInfrastructureForBackwardsCompatibility()
        {
            transportSendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
        }

        [ObsoleteEx(
            Message = "Change transport infrastructure to run send pre-startup checks on component.Start",
            RemoveInVersion = "8")]
        public async Task InvokeSendPreStartupChecksForBackwardsCompatibility()
        {
            var sendResult = await transportSendInfrastructure.PreStartupCheck().ConfigureAwait(false);
            if (!sendResult.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {sendResult.ErrorMessage}");
            }
        }

        public MessageOperations CreateMessageOperations(IBuilder builder, PipelineComponent pipelineComponent)
        {
            return new MessageOperations(
                messageMapper,
                pipelineComponent.CreatePipeline<IOutgoingPublishContext>(builder),
                pipelineComponent.CreatePipeline<IOutgoingSendContext>(builder),
                pipelineComponent.CreatePipeline<IOutgoingReplyContext>(builder),
                pipelineComponent.CreatePipeline<ISubscribeContext>(builder),
                pipelineComponent.CreatePipeline<IUnsubscribeContext>(builder));
        }

        IDispatchMessages GetDispatcher()
        {
            return transportSendInfrastructure.DispatcherFactory();
        }



        TransportSendInfrastructure transportSendInfrastructure;
        readonly IMessageMapper messageMapper;
        readonly TransportInfrastructure transportInfrastructure;
    }
}