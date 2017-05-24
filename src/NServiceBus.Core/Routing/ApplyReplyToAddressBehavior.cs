namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ApplyReplyToAddressBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public enum RouteOption
        {
            None,
            ExplicitReplyDestination,
            RouteReplyToThisInstance,
            RouteReplyToAnyInstanceOfThisEndpoint
        }

        public ApplyReplyToAddressBehavior(string sharedQueue, string instanceSpecificQueue, string publicReturnAddress)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
            configuredReturnAddress = publicReturnAddress ?? sharedQueue;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();
            if (state.Option == RouteOption.RouteReplyToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route a reply to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }

            var replyTo = ApplyUserOverride(configuredReturnAddress, state);

            context.Headers[Headers.ReplyToAddress] = replyTo;

            return next(context);
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

        string instanceSpecificQueue;

        string sharedQueue;
        string configuredReturnAddress;

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