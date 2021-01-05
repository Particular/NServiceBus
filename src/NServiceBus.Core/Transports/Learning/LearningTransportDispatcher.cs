using System.Threading;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Transport;

    class LearningTransportDispatcher : IMessageDispatcher
    {
        public LearningTransportDispatcher(string basePath, int maxMessageSizeKB)
        {
            if (maxMessageSizeKB > int.MaxValue / 1024)
            {
                throw new ArgumentException("The message size cannot be larger than int.MaxValue / 1024.", nameof(maxMessageSizeKB));
            }

            this.basePath = basePath;
            this.maxMessageSizeKB = maxMessageSizeKB;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(
                DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction),
                DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction));
        }

        async Task DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction)
        {
            var tasks = new List<Task>();

            foreach (var transportOperation in transportOperations)
            {
                var subscribers = await GetSubscribersFor(transportOperation.MessageType)
                    .ConfigureAwait(false);

                foreach (var subscriber in subscribers)
                {
                    tasks.Add(WriteMessage(subscriber, transportOperation, transaction));
                }
            }

            await Task.WhenAll(tasks)
                .ConfigureAwait(false);
        }

        Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction)
        {
            return Task.WhenAll(operations.Select(operation =>
            {
                PathChecker.ThrowForBadPath(operation.Destination, "message destination");

                return WriteMessage(operation.Destination, operation, transaction);
            }));
        }

        async Task WriteMessage(string destination, IOutgoingTransportOperation transportOperation, TransportTransaction transaction)
        {
            var message = transportOperation.Message;

            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyDir = Path.Combine(destinationPath, LearningTransportMessagePump.BodyDirName);

            Directory.CreateDirectory(bodyDir);

            var bodyPath = Path.Combine(bodyDir, nativeMessageId) + LearningTransportMessagePump.BodyFileSuffix;

            await AsyncFile.WriteBytes(bodyPath, message.Body)
                .ConfigureAwait(false);

            DateTimeOffset? timeToDeliver = null;

            if (transportOperation.Properties.DoNotDeliverBefore != null)
            {
                timeToDeliver = transportOperation.Properties.DoNotDeliverBefore.At.ToUniversalTime();
            }
            else if (transportOperation.Properties.DelayDeliveryWith != null)
            {
                timeToDeliver = DateTimeOffset.UtcNow + transportOperation.Properties.DelayDeliveryWith.Delay;
            }

            if (timeToDeliver.HasValue)
            {
                // we need to "ceil" the seconds to guarantee that we delay with at least the requested value
                // since the folder name has only second resolution.
                if (timeToDeliver.Value.Millisecond > 0)
                {
                    timeToDeliver += TimeSpan.FromSeconds(1);
                }

                destinationPath = Path.Combine(destinationPath, LearningTransportMessagePump.DelayedDirName, timeToDeliver.Value.ToString("yyyyMMddHHmmss"));

                Directory.CreateDirectory(destinationPath);
            }

            var timeToBeReceived = transportOperation.Properties.DiscardIfNotReceivedBefore;

            if (timeToBeReceived != null && timeToBeReceived.MaxTime < TimeSpan.MaxValue)
            {
                if (timeToDeliver.HasValue)
                {
                    throw new Exception($"Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of type '{message.Headers[Headers.EnclosedMessageTypes]}'.");
                }

                message.Headers[LearningTransportHeaders.TimeToBeReceived] = timeToBeReceived.MaxTime.ToString();
            }

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".metadata.txt";

            var headerPayload = HeaderSerializer.Serialize(message.Headers);
            var headerSize = Encoding.UTF8.GetByteCount(headerPayload);

            if (headerSize + message.Body.Length > maxMessageSizeKB * 1024)
            {
                throw new Exception($"The total size of the '{message.Headers[Headers.EnclosedMessageTypes]}' message body ({message.Body.Length} bytes) plus headers ({headerSize} bytes) is larger than {maxMessageSizeKB} KB and will not be supported on some production transports. Consider using the NServiceBus DataBus or the claim check pattern to avoid messages with a large payload. Use 'EndpointConfiguration.UseTransport<LearningTransport>().NoPayloadSizeRestriction()' to disable this check and proceed with the current message size.");
            }

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated && transaction.TryGet(out ILearningTransportTransaction directoryBasedTransaction))
            {
                await directoryBasedTransaction.Enlist(messagePath, headerPayload)
                    .ConfigureAwait(false);
            }
            else
            {
                // atomic avoids the file being locked when the receiver tries to process it
                await AsyncFile.WriteTextAtomic(messagePath, headerPayload)
                    .ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<string>> GetSubscribersFor(Type messageType)
        {
            var subscribers = new HashSet<string>();

            var allEventTypes = GetPotentialEventTypes(messageType);

            foreach (var eventType in allEventTypes)
            {
                var eventDir = Path.Combine(basePath, ".events", eventType.FullName);

                if (!Directory.Exists(eventDir))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(eventDir))
                {
                    var allText = await AsyncFile.ReadText(file)
                        .ConfigureAwait(false);

                    subscribers.Add(allText);
                }
            }

            return subscribers;
        }

        static IEnumerable<Type> GetPotentialEventTypes(Type messageType)
        {
            var allEventTypes = new HashSet<Type>();

            var currentType = messageType;

            while (currentType != null)
            {
                //do not include the marker interfaces
                if (IsCoreMarkerInterface(currentType))
                {
                    break;
                }

                allEventTypes.Add(currentType);

                currentType = currentType.BaseType;
            }

            foreach (var type in messageType.GetInterfaces().Where(i => !IsCoreMarkerInterface(i)))
            {
                allEventTypes.Add(type);
            }

            return allEventTypes;
        }

        static bool IsCoreMarkerInterface(Type type) => type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);

        int maxMessageSizeKB;
        string basePath;
    }
}