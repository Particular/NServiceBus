namespace NServiceBus.Core.Tests.DependencyInjection
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    class CommonObjectBuilderTests
    {
        [Test]
        public void Should_prevent_mutator_registrations()
        {
            var builder = new CommonObjectBuilder(null);

            Assert.Throws<Exception>(() => builder.ConfigureComponent(typeof(MyIncomingMessageMutator), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent<MyIncomingMessageMutator>(DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(() => new MyIncomingMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(b => new MyIncomingMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.RegisterSingleton(new MyIncomingMessageMutator()));
            Assert.Throws<Exception>(() => ((IConfigureComponents)builder).RegisterSingleton(typeof(MyIncomingMessageMutator), new MyIncomingMessageMutator()));

            Assert.Throws<Exception>(() => builder.ConfigureComponent(typeof(MyOutgoingMessageMutator), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent<MyOutgoingMessageMutator>(DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(() => new MyOutgoingMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(b => new MyOutgoingMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.RegisterSingleton(new MyOutgoingMessageMutator()));
            Assert.Throws<Exception>(() => ((IConfigureComponents)builder).RegisterSingleton(typeof(MyOutgoingMessageMutator), new MyOutgoingMessageMutator()));

            Assert.Throws<Exception>(() => builder.ConfigureComponent(typeof(MyIncomingTransportMessageMutator), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent<MyIncomingTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(() => new MyIncomingTransportMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(b => new MyIncomingTransportMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.RegisterSingleton(new MyIncomingTransportMessageMutator()));
            Assert.Throws<Exception>(() => ((IConfigureComponents)builder).RegisterSingleton(typeof(MyIncomingTransportMessageMutator), new MyIncomingTransportMessageMutator()));

            Assert.Throws<Exception>(() => builder.ConfigureComponent(typeof(MyOutgoingTransportMessageMutator), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent<MyOutgoingTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(() => new MyOutgoingTransportMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.ConfigureComponent(b => new MyOutgoingTransportMessageMutator(), DependencyLifecycle.InstancePerCall));
            Assert.Throws<Exception>(() => builder.RegisterSingleton(new MyOutgoingTransportMessageMutator()));
            Assert.Throws<Exception>(() => ((IConfigureComponents)builder).RegisterSingleton(typeof(MyOutgoingTransportMessageMutator), new MyOutgoingTransportMessageMutator()));
        }

        class MyIncomingMessageMutator : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class MyOutgoingMessageMutator : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class MyIncomingTransportMessageMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class MyOutgoingTransportMessageMutator : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}