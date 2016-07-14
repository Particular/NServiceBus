namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions
{
    using System.Linq;
    using MessageNameSpace;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using OtherMesagenameSpace;

    [TestFixture]
    public class PublishersTests
    {
        [Test]
        public void Should_return_empty_list_for_events_with_no_routes()
        {
            var publishers = new Publishers();

            var result = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.IsEmpty(result);
        }

        [Test]
        public void Should_return_all_routes_registered_for_type()
        {
            var publishers = new Publishers();
            publishers.Add(typeof(BaseMessage), "logicalEndpoint");
            publishers.Add(typeof(BaseMessage), "logicalEndpoint");
            publishers.Add(typeof(BaseMessage), "logicalEndpoint2");
            publishers.AddByAddress(typeof(BaseMessage), "address1");

            var result = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.AreEqual(4, result.Count());
        }

        [Test]
        public void Should_return_static_and_dynamic_routes_for_registered_type()
        {
            var publishers = new Publishers();
            publishers.Add(typeof(BaseMessage), "logicalEndpoint");
            publishers.AddDynamic(e => PublisherAddress.CreateFromEndpointName(e.ToString()));

            var result = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void Should_evaluate_dynamic_rules_on_each_call()
        {
            var c = 0;
            var publishers = new Publishers();
            publishers.AddDynamic(t => PublisherAddress.CreateFromEndpointName((++c).ToString()));

            publishers.GetPublisherFor(typeof(BaseMessage));
            publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.AreEqual(2, c);
        }

        [Test]
        public void Should_not_return_null_results_from_dynamic_rules()
        {
            var publishers = new Publishers();
            publishers.AddDynamic(t => null);

            var result = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.IsEmpty(result);
        }

        [Test]
        public void Should_not_return_rules_for_subclasses()
        {
            var publishers = new Publishers();
            publishers.Add(typeof(SubMessage), "address");

            var result = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.IsEmpty(result);
        }

        [Test]
        public void Should_not_return_rules_for_baseclasses()
        {
            var publishers = new Publishers();
            publishers.Add(typeof(BaseMessage), "address");

            var result = publishers.GetPublisherFor(typeof(SubMessage));

            Assert.IsEmpty(result);
        }

        [Test]
        public void Should_not_return_rules_for_implemented_interfaces()
        {
            var publishers = new Publishers();
            publishers.Add(typeof(IMessageInterface), "address");

            var result = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.IsEmpty(result);
        }

        [Test]
        public void Dynamic_rules_should_not_leak_into_static_rules()
        {
            var calledOnce = false;
            var publishers = new Publishers();
            publishers.Add(typeof(BaseMessage), "address");
            publishers.AddDynamic(e =>
            {
                if (calledOnce)
                {
                    return null;
                }

                calledOnce = true;
                return PublisherAddress.CreateFromEndpointName("x");
            });

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(BaseMessage));

            Assert.AreEqual(2, result1.Count());
            Assert.AreEqual(1, result2.Count());
        }
    }
}

namespace MessageNameSpace
{
    interface IMessageInterface
    {
    }

    class BaseMessage : IMessageInterface
    {
    }
}

namespace OtherMesagenameSpace
{
    using MessageNameSpace;

    class SubMessage : BaseMessage
    {
    }
}

class EventWithoutNamespace
{
}