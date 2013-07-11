using NUnit.Framework;

namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using Receiving;

    [TestFixture]
    public class When_using_the_configuration_bases_channel_manager
    {
        IManageReceiveChannels config;
        IEnumerable<ReceiveChannel> activeChannels;
        Channel defaultChannel;

        [SetUp]
        public void SetUp()
        {
            config = new ConfigurationBasedChannelManager();
            activeChannels = config.GetReceiveChannels();
            defaultChannel = config.GetDefaultChannel();

        }

        [Test]
        public void Should_read_the_channels_from_the_configsource()
        {
            Assert.AreEqual(activeChannels.Count(), 3);
        }

        [Test]
        public void Should_default_the_number_of_worker_threads_to_1()
        {
            Assert.AreEqual(activeChannels.First().NumberOfWorkerThreads, 1);
        }

        [Test]
        public void Should_allow_number_of_worker_threads_to_be_specified()
        {
            Assert.AreEqual(activeChannels.Last().NumberOfWorkerThreads, 3);
        }

        [Test]
        public void Should_default_to_the_first_channel_if_no_default_is_set()
        {
            Assert.AreEqual(activeChannels.First(), defaultChannel);
        }
    }
}