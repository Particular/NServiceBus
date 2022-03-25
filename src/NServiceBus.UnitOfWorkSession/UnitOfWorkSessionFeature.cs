namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;
    using Persistence;
    using Transport;

    public class UnitOfWorkSessionFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // check if not send only
            // check for outbox

            factory = new UnitOfWorkMessageSessionFactory();

            context.Services.AddSingleton<IUnitOfWorkMessageSessionFactory>(factory);

            var localQueueAddress = context.LocalQueueAddress();

            context.RegisterStartupTask(serviceProvider => new RegisterSessionStartupTask(factory,
                localQueueAddress,
                serviceProvider.GetRequiredService<ITransportAddressResolver>(),
                serviceProvider.GetRequiredService<IMessageDispatcher>(),
                serviceProvider.GetService<IOutboxStorage>(),
                serviceProvider.GetService<ISynchronizedStorageAdapter>(),
                serviceProvider.GetService<ISynchronizedStorage>()));
        }

        UnitOfWorkMessageSessionFactory? factory;

        class RegisterSessionStartupTask : FeatureStartupTask
        {
            readonly UnitOfWorkMessageSessionFactory factory;
            readonly IMessageDispatcher messageDispatcher;
            readonly IOutboxStorage? outboxStorage;
            readonly ISynchronizedStorageAdapter? synchronizedStorageAdapter;
            readonly ISynchronizedStorage? synchronizedStorage;
            readonly ITransportAddressResolver transportAddressResolver;
            readonly QueueAddress localQueueAddress;

            public RegisterSessionStartupTask(UnitOfWorkMessageSessionFactory factory, QueueAddress localQueueAddress, ITransportAddressResolver transportAddressResolver,
                IMessageDispatcher messageDispatcher, IOutboxStorage? outboxStorage,
                ISynchronizedStorageAdapter? synchronizedStorageAdapter, ISynchronizedStorage? synchronizedStorage)
            {
                this.localQueueAddress = localQueueAddress;
                this.transportAddressResolver = transportAddressResolver;
                this.outboxStorage = outboxStorage;
                this.messageDispatcher = messageDispatcher;
                this.factory = factory;
                this.synchronizedStorageAdapter = synchronizedStorageAdapter;
                this.synchronizedStorage = synchronizedStorage;
            }

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                var queueAddress = transportAddressResolver.ToTransportAddress(localQueueAddress);
                factory.Initialize(queueAddress, session, messageDispatcher, outboxStorage, synchronizedStorageAdapter, synchronizedStorage);
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}