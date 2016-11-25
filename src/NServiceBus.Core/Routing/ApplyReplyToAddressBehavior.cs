namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;
    using Transport;

    class ApplyReplyToAddressBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public enum RouteOption
        {
            None,
            ExplicitReplyDestination,
            RouteReplyToThisInstance,
            RouteReplyToAnyInstanceOfThisEndpoint
        }

        public ApplyReplyToAddressBehavior(string sharedQueue, string instanceSpecificQueue, string publicReturnAddress, string distributorAddress)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
            this.publicReturnAddress = publicReturnAddress;
            this.distributorAddress = distributorAddress;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();
            if (state.Option == RouteOption.RouteReplyToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route a reply to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }

            var effectiveSharedQUeue = ApplyDistributorLogic(context);

            var replyTo = ApplyUserOverride(publicReturnAddress ?? effectiveSharedQUeue, state);

            context.Headers[Headers.ReplyToAddress] = replyTo;
            
            return next(context);
        }

        string ApplyDistributorLogic(IExtendable context)
        {
            IncomingMessage incomingMessage;
            return context.Extensions.TryGet(out incomingMessage) && incomingMessage.Headers.ContainsKey(LegacyDistributorHeaders.WorkerSessionId) 
                ? distributorAddress 
                : sharedQueue;
        }


        string ApplyUserOverride(string replyTo, State state)
        {
            if (state.Option == RouteOption.RouteReplyToAnyInstanceOfThisEndpoint)
            {
                replyTo = sharedQueue;
            }
            else if (state.Option == RouteOption.RouteReplyToThisInstance)
            {
                replyTo = instanceSpecificQueue;
            }
            else if (state.Option == RouteOption.ExplicitReplyDestination)
            {
                replyTo = state.ExplicitDestination;
            }
            return replyTo;
        }

        string distributorAddress;
        string instanceSpecificQueue;
        string publicReturnAddress;

        string sharedQueue;

        public class State
        {
            public RouteOption Option
            {
                get { return option; }
                set
                {
                    if (option != RouteOption.None)
                    {
                        throw new Exception("Already specified reply routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            public string ExplicitDestination { get; set; }
            RouteOption option;
        }
    }
}