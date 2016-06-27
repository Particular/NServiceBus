namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class ApplyReplyToAddressBehavior : Behavior<IOutgoingLogicalMessageContext>
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

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();
            if (state.Option == RouteOption.RouteReplyToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route a reply to this specific instance because endpoint instance ID was not provided by either host, a plugin or user. You can specify it via EndpointConfiguration.AddUniquelyAddressableQueue(string discriminator).");
            }
            context.Headers[Headers.ReplyToAddress] = ApplyUserOverride(publicReturnAddress ?? sharedQueue, state);

            //Legacy distributor logic
            IncomingMessage incomingMessage;
            if (context.TryGetIncomingPhysicalMessage(out incomingMessage) && incomingMessage.Headers.ContainsKey(LegacyDistributorHeaders.WorkerSessionId))
            {
                context.Headers[Headers.ReplyToAddress] = distributorAddress;
            }
            return next();
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