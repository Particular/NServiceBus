namespace NServiceBus.MessageHeaders
{
    using System.Collections.Generic;
    using Config;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Messages;

    class HeaderBootstrapper :  IWantToRunWhenConfigurationIsComplete
    {
        public IBuilder Builder { get; set; }

        public void SetupHeaderActions()
        {
            ExtensionMethods.GetHeaderAction = (message, key) =>
            {
                var pipelineFactory = Builder.Build<PipelineExecutor>();

                if (message == ExtensionMethods.CurrentMessageBeingHandled)
                {
                    LogicalMessage messageBeingReceived;

                    //first try to get the header from the current logical message
                    if (pipelineFactory.CurrentContext.TryGet(out messageBeingReceived))
                    {
                        string value;

                        messageBeingReceived.Headers.TryGetValue(key, out value);

                        return value;
                    }

                    //falling back to get the headers from the physical message
                    // when we remove the multi message feature we can remove this and instead
                    // share the same header collection btw physical and logical message
                    var bus = Builder.Build<IBus>();
                    if (bus.CurrentMessageContext != null)
                    {
                        string value;
                        if (bus.CurrentMessageContext.Headers.TryGetValue(key, out value))
                        {
                            return value;
                        }
                    }
                    return null;
                }

                Dictionary<object, Dictionary<string, string>> outgoingHeaders;

                if (!pipelineFactory.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
                {
                    return null;
                }
                Dictionary<string, string> outgoingHeadersForThisMessage;

                if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
                {
                    return null;
                }

                string headerValue;

                outgoingHeadersForThisMessage.TryGetValue(key, out headerValue);

                return headerValue;
            };

            ExtensionMethods.SetHeaderAction = (message, key, value) =>
            {
                var pipelineFactory = Builder.Build<PipelineExecutor>();

                //are we in the process of sending a logical message
                var outgoingLogicalMessageContext = pipelineFactory.CurrentContext as OutgoingContext;

                if (outgoingLogicalMessageContext != null && outgoingLogicalMessageContext.OutgoingLogicalMessage.Instance == message)
                {
                    outgoingLogicalMessageContext.OutgoingLogicalMessage.Headers[key] = value;
                }

                Dictionary<object, Dictionary<string, string>> outgoingHeaders;

                if (!pipelineFactory.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
                {
                    outgoingHeaders = new Dictionary<object, Dictionary<string, string>>();

                    pipelineFactory.CurrentContext.Set("NServiceBus.OutgoingHeaders", outgoingHeaders);
                }

                Dictionary<string, string> outgoingHeadersForThisMessage;

                if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
                {
                    outgoingHeadersForThisMessage = new Dictionary<string, string>();
                    outgoingHeaders[message] = outgoingHeadersForThisMessage;
                }

                outgoingHeadersForThisMessage[key] = value;
            };

        }

        void IWantToRunWhenConfigurationIsComplete.Run(Configure config)
        {
            SetupHeaderActions();
        }
    }
}
