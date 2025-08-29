#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class LearningTransportInfrastructure : TransportInfrastructure
{
    public LearningTransportInfrastructure(HostSettings settings, LearningTransport transport, ReceiveSettings[] receiverSettings)
    {
        this.settings = settings;
        this.transport = transport;

        if (string.IsNullOrWhiteSpace(storagePath = transport.StorageDirectory))
        {
            storagePath = FindStoragePath();
        }

        var maxPayloadSize = transport.RestrictPayloadSize ? 64 : int.MaxValue / 1024; //64 kB is the max size of the ASQ transport
        Dispatcher = new LearningTransportDispatcher(storagePath, maxPayloadSize);

        Receivers = receiverSettings
            .ToDictionary<ReceiveSettings, string, IMessageReceiver>(receiverSetting => receiverSetting.Id, CreateReceiver);
    }

    static string FindStoragePath()
    {
        var directory = AppDomain.CurrentDomain.BaseDirectory;

        while (true)
        {
            // Finding a solution file takes precedence
            if (Directory.EnumerateFiles(directory).Any(file => Path.GetExtension(file) is ".sln" or ".slnx"))
            {
                return Path.Combine(directory, DefaultLearningTransportDirectory);
            }

            // When no solution file was found try to find a learning transport directory
            var learningTransportDirectory = Path.Combine(directory, DefaultLearningTransportDirectory);
            if (Directory.Exists(learningTransportDirectory))
            {
                return learningTransportDirectory;
            }

            var parent = Directory.GetParent(directory) ?? throw new Exception($"Unable to determine the storage directory path for the learning transport due to the absence of a solution file. Either create a '{DefaultLearningTransportDirectory}' directory in one of this project’s parent directories, or specify the path explicitly using the 'EndpointConfiguration.UseTransport<LearningTransport>().StorageDirectory()' API.");

            directory = parent.FullName;
        }
    }

    LearningTransportMessagePump CreateReceiver(ReceiveSettings receiveSettings)
    {
        var errorQueueAddress = receiveSettings.ErrorQueue;
        PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");

        PathChecker.ThrowForBadPath(settings.Name, "endpoint name");

        var queueAddress = ToTransportAddress(receiveSettings.ReceiveAddress);

        ISubscriptionManager? subscriptionManager = null;
        if (receiveSettings.UsePublishSubscribe)
        {

            subscriptionManager = new LearningTransportSubscriptionManager(storagePath, settings.Name, queueAddress);
        }
        return new LearningTransportMessagePump(receiveSettings.Id, queueAddress, storagePath, settings.CriticalErrorAction, subscriptionManager, receiveSettings, transport.TransportTransactionMode);
    }

    readonly string storagePath;
    readonly HostSettings settings;
    readonly LearningTransport transport;

    const string DefaultLearningTransportDirectory = ".learningtransport";

    public override async Task Shutdown(CancellationToken cancellationToken = default) =>
        await Task.WhenAll(Receivers.Values.Select(r => r.StopReceive(cancellationToken)))
            .ConfigureAwait(false);

    public override string ToTransportAddress(QueueAddress queueAddress)
    {
        var address = queueAddress.BaseAddress;
        PathChecker.ThrowForBadPath(address, "endpoint name");

        var discriminator = queueAddress.Discriminator;

        if (!string.IsNullOrEmpty(discriminator))
        {
            PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

            address += $"-{discriminator}";
        }

        var qualifier = queueAddress.Qualifier;

        if (!string.IsNullOrEmpty(qualifier))
        {
            PathChecker.ThrowForBadPath(qualifier, "address qualifier");

            address += $"-{qualifier}";
        }

        return address;
    }

    public override IEnumerable<KeyValuePair<string, ManifestItem>> GetManifest() => throw new NotImplementedException();
}