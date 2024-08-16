namespace NServiceBus.Core.Tests.DataBus;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.DataBus;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
class When_applying_the_databus_message_mutator_to_outgoing_messages
{
    [Test]
    public async Task Outgoing_databus_properties_should_be_dehydrated()
    {
        var context = new TestableOutgoingLogicalMessageContext
        {
            Message = new OutgoingLogicalMessage(typeof(MessageWithDataBusProperty), new MessageWithDataBusProperty
            {
                DataBusProperty = new DataBusProperty<string>("test")
            })
        };

        var fakeDatabus = new FakeDataBus();

        var sendBehavior = new DataBusSendBehavior(fakeDatabus, new SystemJsonDataBusSerializer(), new Conventions());

        await sendBehavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(fakeDatabus.TTBRUsed, Is.EqualTo(TimeSpan.MaxValue));
    }

    [Test]
    public async Task Serializer_header_should_be_set()
    {
        var context = new TestableOutgoingLogicalMessageContext
        {
            Message = new OutgoingLogicalMessage(typeof(MessageWithDataBusProperty), new MessageWithDataBusProperty
            {
                DataBusProperty = new DataBusProperty<string>("test")
            })
        };

        var fakeDatabus = new FakeDataBus();
        var serializer = new SystemJsonDataBusSerializer();

        var sendBehavior = new DataBusSendBehavior(fakeDatabus, serializer, new Conventions());

        await sendBehavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.DataBusConfigContentType], Is.EqualTo(serializer.ContentType));
    }

    [Test]
    public async Task Time_to_live_should_be_passed_on_the_databus()
    {
        var context = new TestableOutgoingLogicalMessageContext
        {
            Message = new OutgoingLogicalMessage(typeof(MessageWithExplicitTimeToLive), new MessageWithExplicitTimeToLive
            {
                DataBusProperty = new DataBusProperty<string>("test")
            })
        };

        context.Extensions.GetOrCreate<DispatchProperties>().DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(TimeSpan.FromMinutes(1));

        var fakeDatabus = new FakeDataBus();

        var sendBehavior = new DataBusSendBehavior(fakeDatabus, new SystemJsonDataBusSerializer(), new Conventions());

        await sendBehavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(fakeDatabus.TTBRUsed, Is.EqualTo(TimeSpan.FromMinutes(1)));
    }

    class FakeDataBus : IDataBus
    {
        public Task<Stream> Get(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> Put(Stream stream, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default)
        {
            TTBRUsed = timeToBeReceived;
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task Start(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public TimeSpan TTBRUsed;
    }
}