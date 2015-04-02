namespace NServiceBus.Pipeline
{
    using NServiceBus.Logging;

    class HardcodedPipelineSteps
    {
      
        public static void RegisterIncomingCoreBehaviors(PipelineSettings pipeline)
        {
            pipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            pipeline.RegisterConnector<DeserializeLogicalMessagesConnector>("Deserializes the physical message body into logical messages");
            pipeline.RegisterConnector<ExecuteLogicalMessagesConnector>("Starts the execution of each logical message");
            pipeline.RegisterConnector<LoadHandlersConnector>("Gets all the handlers to invoke from the MessageHandler registry based on the message type.");
      
            
            pipeline
                .Register("ReceivePerformanceDiagnosticsBehavior", typeof(ReceivePerformanceDiagnosticsBehavior), "Add ProcessingStarted and ProcessingEnded headers")
                .Register(WellKnownStep.ProcessingStatistics, typeof(ProcessingStatisticsBehavior), "Add ProcessingStarted and ProcessingEnded headers")
                .Register(WellKnownStep.CreateChildContainer, typeof(ChildContainerBehavior), "Creates the child container")
                .Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW")
                .Register("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.")
                .Register(WellKnownStep.MutateIncomingTransportMessage, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes IMutateIncomingTransportMessages")
                .Register("InvokeRegisteredCallbacks", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary")
                .Register(WellKnownStep.MutateIncomingMessages, typeof(ApplyIncomingMessageMutatorsBehavior), "Executes IMutateIncomingMessages")
                .Register("PrepareCommandContext", typeof(PrepareCommandContextBehavior), "If the current handler handles commands a command context is added as invocation context.")
                .Register("PrepareEventContext", typeof(PrepareEventContextBehavior), "If the current handler handles events a event context is added as invocation context.")
                .Register("PrepareResponseContext", typeof(PrepareResponseContextBehavior), "If the current handler handles responses a response context is added as invocation context.")
                .Register("PrepareTimeoutContext", typeof(PrepareTimoutContextBehavior), "If the current handler handles timeouts a timeout context is added as invocation context.")
                .Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T), IHandle<T>.Handle(T,IHandleContext), ISubscribe<T>.Handle(T,ISubscribeContext) or ITimeout<T>.Timeout(T,ITimeoutContext) method");
        }

        public static void RegisterOutgoingCoreBehaviors(PipelineSettings pipeline)
        {

            pipeline.RegisterConnector<CreatePhysicalMessageConnector>("Converts a logical message into a physical message");

            var seq = pipeline.Register(WellKnownStep.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices")
                .Register(WellKnownStep.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages")
                .Register("PopulateAutoCorrelationHeadersForReplies", typeof(PopulateAutoCorrelationHeadersForRepliesBehavior), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.")
                .Register(WellKnownStep.SerializeMessage, typeof(SerializeMessagesBehavior), "Serializes the message to be sent out on the wire")
                .Register(WellKnownStep.MutateOutgoingTransportMessage, typeof(MutateOutgoingPhysicalMessageBehavior), "Executes IMutateOutgoingTransportMessages");

            if (LogManager.GetLogger("LogOutgoingMessage").IsDebugEnabled)
            {
                seq = seq.Register("LogOutgoingMessage", typeof(LogOutgoingMessageBehavior), "Logs the message contents before it is sent.");
            }
            seq.Register(WellKnownStep.DispatchMessageToTransport, typeof(DispatchMessageToTransportBehavior), "Dispatches messages to the transport");
        }
    }
}