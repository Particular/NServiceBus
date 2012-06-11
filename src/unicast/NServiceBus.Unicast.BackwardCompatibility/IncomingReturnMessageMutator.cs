using System.Globalization;
using NServiceBus.Logging;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.BackwardCompatibility
{
    /// <summary>
    /// If this is a V26 message, extract completion message return error code and place it in the transport headers
    /// </summary>
    public class IncomingReturnMessageMutator : IMutateIncomingMessages, INeedInitialization
    {
        /// <summary>
        /// Reference to the BUS to get a hold of the current TransportMessage
        /// </summary>
        public IBus Bus { get; set; }
        
        /// <summary>
        /// If this is a completion message from a 2.6 sender, copy the error code.
        /// </summary>
        /// <param name="message">Message to copy ErrorCode from.</param>
        /// <returns>Same message as received.</returns>
        public object MutateIncoming(object message)
        {
            var completionMessage = message as CompletionMessage;
            if (completionMessage == null) 
                return message;

            if(!Bus.CurrentMessageContext.Headers.ContainsKey(Headers.ReturnMessageErrorCodeHeader))
                Bus.CurrentMessageContext.Headers.Add(Headers.ReturnMessageErrorCodeHeader,
                                                      completionMessage.ErrorCode.ToString(CultureInfo.InvariantCulture));
            
            //Change to Transport to be a Control Message so no need to find a handler for that.
            if(!Bus.CurrentMessageContext.Headers.ContainsKey(Headers.ControlMessageHeader))
                Bus.CurrentMessageContext.Headers.Add(Headers.ControlMessageHeader, true.ToString(CultureInfo.InvariantCulture));

            return message;
        }

        /// <summary>
        /// Register the IncomingReturnMessageMutator
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<IncomingReturnMessageMutator>(DependencyLifecycle.InstancePerCall);
            Log.Debug("Configured IncomingReturnMessageMutator");
        }

        private readonly static ILog Log = LogManager.GetLogger(typeof(IncomingSubscriptionMessageMutator));
    }
}
