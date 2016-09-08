namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using DeliveryConstraints;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    public class When_using_custom_delayed_delivery : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_replace_the_default_behavior()
        {
            var delay = TimeSpan.FromMilliseconds(1);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);
                    options.RouteToThisEndpoint();

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Run();

            Assert.IsTrue(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.DisableFeature<TimeoutManager>();
                    config.EnableFeature<CustomTimeoutManager>();
                });
            }

            class CustomTimeoutManager : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("CustomTM");
                    var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

                    context.AddSatelliteReceiver("CustomTM", satelliteAddress, TransportTransactionMode.ReceiveOnly, PushRuntimeSettings.Default, RecoverabilityPolicy, (builder, messageContext) =>
                    {
                        var ctx = builder.Build<Context>();
                        ctx.WasCalled = true;
                        return Task.FromResult(0);
                    });

                    context.Pipeline.Replace("ThrowIfCannotDeferMessage", new RouteDeferredMessageToTimeoutManagerBehavior(satelliteAddress));
                }

                static RecoverabilityAction RecoverabilityPolicy(RecoverabilityConfig config, ErrorContext errorContext)
                {
                    return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
                }

                class RouteDeferredMessageToTimeoutManagerBehavior : Behavior<IRoutingContext>
                {
                    public RouteDeferredMessageToTimeoutManagerBehavior(string timeoutManagerAddress)
                    {
                        this.timeoutManagerAddress = timeoutManagerAddress;
                    }

                    public override Task Invoke(IRoutingContext context, Func<Task> next)
                    {
                        DateTime deliverAt;
                        if (!IsDeferred(context, out deliverAt))
                        {
                            return next();
                        }

                        DiscardIfNotReceivedBefore discardIfNotReceivedBefore;
                        if (context.Extensions.TryGetDeliveryConstraint(out discardIfNotReceivedBefore))
                        {
                            throw new Exception("Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of this type.");
                        }

                        var newRoutingStrategies = context.RoutingStrategies.Select(s => RerouteToTimeoutManager(s, context, deliverAt));
                        context.RoutingStrategies = newRoutingStrategies.ToArray();

                        return next();
                    }

                    RoutingStrategy RerouteToTimeoutManager(RoutingStrategy routingStrategy, IRoutingContext context, DateTime deliverAt)
                    {
                        var headers = new Dictionary<string, string>(context.Message.Headers);
                        var originalTag = routingStrategy.Apply(headers);
                        var unicastTag = originalTag as UnicastAddressTag;
                        if (unicastTag == null)
                        {
                            throw new Exception("Delayed delivery using the Timeout Manager is only supported for messages with unicast routing");
                        }
                        return new UnicastRoutingStrategy(timeoutManagerAddress);
                    }

                    static bool IsDeferred(IExtendable context, out DateTime deliverAt)
                    {
                        deliverAt = DateTime.MinValue;
                        DoNotDeliverBefore doNotDeliverBefore;
                        DelayDeliveryWith delayDeliveryWith;
                        if (context.Extensions.TryRemoveDeliveryConstraint(out doNotDeliverBefore))
                        {
                            deliverAt = doNotDeliverBefore.At;
                            return true;
                        }
                        if (context.Extensions.TryRemoveDeliveryConstraint(out delayDeliveryWith))
                        {
                            deliverAt = DateTime.UtcNow + delayDeliveryWith.Delay;
                            return true;
                        }
                        return false;
                    }

                    string timeoutManagerAddress;
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}