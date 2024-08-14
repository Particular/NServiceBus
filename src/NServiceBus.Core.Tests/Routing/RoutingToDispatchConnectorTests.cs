namespace NServiceBus.Core.Tests.Routing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class RoutingToDispatchConnectorTests
{
    [Test]
    public async Task Should_preserve_message_state_for_one_routing_strategy_for_allocation_reasons()
    {
        var behavior = new RoutingToDispatchConnector();
        IEnumerable<TransportOperation> operations = null;
        var testableRoutingContext = new TestableRoutingContext
        {
            RoutingStrategies =
            [
                new DestinationRoutingStrategy("destination1", "HeaderKeyAddedByTheRoutingStrategy1", "HeaderValueAddedByTheRoutingStrategy1")
            ]
        };
        var originalDispatchProperties = new DispatchProperties
        {
            { "SomeKey", "SomeValue" }
        };
        testableRoutingContext.Extensions.Set(originalDispatchProperties);
        var originalHeaders = new Dictionary<string, string> { { "SomeHeaderKey", "SomeHeaderValue" } };
        testableRoutingContext.Message = new OutgoingMessage("ID", originalHeaders, Array.Empty<byte>());
        await behavior.Invoke(testableRoutingContext, context =>
        {
            operations = context.Operations;
            return Task.CompletedTask;
        });

        Assert.That(operations.ToList(), Has.Count.EqualTo(1));

        TransportOperation destination1Operation = operations.ElementAt(0);
        Assert.Multiple(() =>
        {
            Assert.That(destination1Operation.Message.MessageId, Is.EqualTo("ID"));
            Assert.That((destination1Operation.AddressTag as UnicastAddressTag)?.Destination, Is.EqualTo("destination1"));
        });
        Dictionary<string, string> destination1Headers = destination1Operation.Message.Headers;
        Assert.That(destination1Headers, Contains.Item(new KeyValuePair<string, string>("SomeHeaderKey", "SomeHeaderValue")));
        Assert.That(destination1Headers, Contains.Item(new KeyValuePair<string, string>("HeaderKeyAddedByTheRoutingStrategy1", "HeaderValueAddedByTheRoutingStrategy1")));
        Assert.That(destination1Headers, Is.SameAs(originalHeaders));
        DispatchProperties destination1DispatchProperties = destination1Operation.Properties;
        Assert.That(destination1DispatchProperties, Contains.Item(new KeyValuePair<string, string>("SomeKey", "SomeValue")));
        Assert.That(destination1DispatchProperties, Is.SameAs(originalDispatchProperties));
    }

    [Test]
    public async Task Should_copy_message_state_for_multiple_routing_strategies()
    {
        var behavior = new RoutingToDispatchConnector();
        IEnumerable<TransportOperation> operations = null;
        var testableRoutingContext = new TestableRoutingContext
        {
            RoutingStrategies =
            [
                new DestinationRoutingStrategy("destination1", "HeaderKeyAddedByTheRoutingStrategy1", "HeaderValueAddedByTheRoutingStrategy1"),
                new DestinationRoutingStrategy("destination2", "HeaderKeyAddedByTheRoutingStrategy2", "HeaderValueAddedByTheRoutingStrategy2")
            ]
        };
        var originalDispatchProperties = new DispatchProperties
        {
            { "SomeKey", "SomeValue" }
        };
        testableRoutingContext.Extensions.Set(originalDispatchProperties);
        var originalHeaders = new Dictionary<string, string> { { "SomeHeaderKey", "SomeHeaderValue" } };
        testableRoutingContext.Message = new OutgoingMessage("ID", originalHeaders, Array.Empty<byte>());
        await behavior.Invoke(testableRoutingContext, context =>
        {
            operations = context.Operations;
            return Task.CompletedTask;
        });

        Assert.That(operations.ToList(), Has.Count.EqualTo(2));

        TransportOperation destination1Operation = operations.ElementAt(0);
        Assert.Multiple(() =>
        {
            Assert.That(destination1Operation.Message.MessageId, Is.EqualTo("ID"));
            Assert.That((destination1Operation.AddressTag as UnicastAddressTag)?.Destination, Is.EqualTo("destination1"));
        });
        Dictionary<string, string> destination1Headers = destination1Operation.Message.Headers;
        Assert.That(destination1Headers, Contains.Item(new KeyValuePair<string, string>("SomeHeaderKey", "SomeHeaderValue")));
        Assert.That(destination1Headers, Contains.Item(new KeyValuePair<string, string>("HeaderKeyAddedByTheRoutingStrategy1", "HeaderValueAddedByTheRoutingStrategy1")));
        Assert.That(destination1Headers, Does.Not.Contain(new KeyValuePair<string, string>("HeaderKeyAddedByTheRoutingStrategy2", "HeaderValueAddedByTheRoutingStrategy2")));
        Assert.That(destination1Headers, Is.Not.SameAs(originalHeaders));
        DispatchProperties destination1DispatchProperties = destination1Operation.Properties;
        Assert.That(destination1DispatchProperties, Contains.Item(new KeyValuePair<string, string>("SomeKey", "SomeValue")));
        Assert.That(destination1DispatchProperties, Is.Not.SameAs(originalDispatchProperties));

        TransportOperation destination2Operation = operations.ElementAt(1);
        Assert.Multiple(() =>
        {
            Assert.That(destination2Operation.Message.MessageId, Is.EqualTo("ID"));
            Assert.That((destination2Operation.AddressTag as UnicastAddressTag)?.Destination, Is.EqualTo("destination2"));
        });
        Dictionary<string, string> destination2Headers = destination2Operation.Message.Headers;
        Assert.That(destination2Headers, Contains.Item(new KeyValuePair<string, string>("SomeHeaderKey", "SomeHeaderValue")));
        Assert.That(destination2Headers, Contains.Item(new KeyValuePair<string, string>("HeaderKeyAddedByTheRoutingStrategy2", "HeaderValueAddedByTheRoutingStrategy2")));
        Assert.That(destination2Headers, Does.Not.Contain(new KeyValuePair<string, string>("HeaderKeyAddedByTheRoutingStrategy1", "HeaderValueAddedByTheRoutingStrategy1")));
        Assert.That(destination2Headers, Is.Not.SameAs(originalHeaders));
        DispatchProperties destination2DispatchProperties = destination2Operation.Properties;
        Assert.That(destination2DispatchProperties, Is.Not.SameAs(originalDispatchProperties));
        Assert.Multiple(() =>
        {
            Assert.That(destination2DispatchProperties, Contains.Item(new KeyValuePair<string, string>("SomeKey", "SomeValue")));

            Assert.That(destination1Headers, Is.Not.SameAs(destination2Headers));
            Assert.That(destination1DispatchProperties, Is.Not.SameAs(destination2DispatchProperties));
        });
    }

    [Test]
    public async Task Should_preserve_headers_generated_by_custom_routing_strategy()
    {
        var behavior = new RoutingToDispatchConnector();
        Dictionary<string, string> headers = null;
        await behavior.Invoke(new TestableRoutingContext { RoutingStrategies = [new HeaderModifyingRoutingStrategy()] }, context =>
            {
                headers = context.Operations.First().Message.Headers;
                return Task.CompletedTask;
            });

        Assert.That(headers.ContainsKey("CustomHeader"), Is.True);
    }

    [Test]
    public async Task Should_dispatch_immediately_if_user_requested()
    {
        var options = new SendOptions();
        options.RequireImmediateDispatch();

        var dispatched = false;
        var behavior = new RoutingToDispatchConnector();
        var message = new OutgoingMessage("ID", [], Array.Empty<byte>());

        await behavior.Invoke(new RoutingContext(message,
            new UnicastRoutingStrategy("Destination"), CreateContext(options, true)), c =>
            {
                dispatched = true;
                return Task.CompletedTask;
            });

        Assert.That(dispatched, Is.True);
    }

    [Test]
    public async Task Should_dispatch_immediately_if_not_sending_from_a_handler()
    {
        var dispatched = false;
        var behavior = new RoutingToDispatchConnector();
        var message = new OutgoingMessage("ID", [], Array.Empty<byte>());

        await behavior.Invoke(new RoutingContext(message,
            new UnicastRoutingStrategy("Destination"), CreateContext(new SendOptions(), false)), c =>
            {
                dispatched = true;
                return Task.CompletedTask;
            });

        Assert.That(dispatched, Is.True);
    }

    [Test]
    public async Task Should_not_dispatch_by_default()
    {
        var dispatched = false;
        var behavior = new RoutingToDispatchConnector();
        var message = new OutgoingMessage("ID", [], Array.Empty<byte>());

        await behavior.Invoke(new RoutingContext(message,
            new UnicastRoutingStrategy("Destination"), CreateContext(new SendOptions(), true)), c =>
            {
                dispatched = true;
                return Task.CompletedTask;
            });

        Assert.That(dispatched, Is.False);
    }

    [Test]
    public async Task Should_promote_message_headers_to_pipeline_activity()
    {
        var behavior = new RoutingToDispatchConnector();
        var routingContext = new TestableRoutingContext();
        routingContext.Message.Headers[Headers.ContentType] = "test content type"; // one of the headers that will be mapped to tags

        using var pipelineActivity = new Activity("pipeline activity");
        routingContext.Extensions.SetOutgoingPipelineActitvity(pipelineActivity);
        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.Start();

        await behavior.Invoke(routingContext, _ => Task.CompletedTask);

        var contentTypeTag = pipelineActivity.GetTagItem(ActivityTags.ContentType);
        Assert.Multiple(() =>
        {
            Assert.That(contentTypeTag, Is.EqualTo("test content type"), "should set tags on activity from pipeline context");

            Assert.That(ambientActivity.GetTagItem(ActivityTags.ContentType), Is.Null, "should not set tags on Activity.Current");
        });
    }

    static OutgoingSendContext CreateContext(SendOptions options, bool fromHandler)
    {
        var message = new MyMessage();
        var context = new OutgoingSendContext(new OutgoingLogicalMessage(message.GetType(), message), options.UserDefinedMessageId ?? Guid.NewGuid().ToString(), options.OutgoingHeaders, options.Context, new FakeRootContext());
        if (fromHandler)
        {
            context.Extensions.Set(new PendingTransportOperations());
        }
        return context;
    }

    class HeaderModifyingRoutingStrategy : RoutingStrategy
    {
        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            headers["CustomHeader"] = "CustomValue";
            return new UnicastAddressTag("destination");
        }
    }

    class DestinationRoutingStrategy(string destination, string headerKey, string headerValue) : RoutingStrategy
    {
        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            headers[headerKey] = headerValue;
            return new UnicastAddressTag(destination);
        }
    }

    class MyMessage : IMessage
    {
    }
}