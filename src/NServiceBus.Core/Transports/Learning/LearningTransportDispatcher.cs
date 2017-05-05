namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Performance.TimeToBeReceived;
    using Transport;

    class LearningTransportDispatcher : IDispatchMessages
    {
        public LearningTransportDispatcher(string basePath)
        {
            this.basePath = basePath;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
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
            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyDir = Path.Combine(destinationPath, LearningTransportMessagePump.BodyDirName);

            Directory.CreateDirectory(bodyDir);

            var bodyPath = Path.Combine(bodyDir, nativeMessageId) + LearningTransportMessagePump.BodyFileSuffix;

            await AsyncFile.WriteBytes(bodyPath, transportOperation.Message.Body)
                .ConfigureAwait(false);

            DateTime? timeToDeliver = null;

            if (transportOperation.DeliveryConstraints.TryGet(out DoNotDeliverBefore doNotDeliverBefore))
            {
                timeToDeliver = doNotDeliverBefore.At;
            }
            else if (transportOperation.DeliveryConstraints.TryGet(out DelayDeliveryWith delayDeliveryWith))
            {
                timeToDeliver = DateTime.UtcNow + delayDeliveryWith.Delay;
            }

            if (timeToDeliver.HasValue)
            {
                if (transportOperation.DeliveryConstraints.TryGet(out DiscardIfNotReceivedBefore timeToBeReceived) && timeToBeReceived.MaxTime < TimeSpan.MaxValue)
                {
                    throw new Exception("Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of this type.");
                }

                // we need to "ceil" the seconds to guarantee that we delay with at least the requested value
                // since the folder name has only second resolution.
                if (timeToDeliver.Value.Millisecond > 0)
                {
                    timeToDeliver += TimeSpan.FromSeconds(1);
                }

                destinationPath = Path.Combine(destinationPath, ".delayed", timeToDeliver.Value.ToString("yyyyMMddHHmmss"));

                Directory.CreateDirectory(destinationPath);
            }

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".metadata.txt";

            var messageContents = HeaderSerializer.Serialize(transportOperation.Message.Headers);

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated && transaction.TryGet(out ILearningTransportTransaction directoryBasedTransaction))
            {
                await directoryBasedTransaction.Enlist(messagePath, messageContents)
                    .ConfigureAwait(false);
            }
            else
            {
                // atomic avoids the file being locked when the receiver tries to process it
                await AsyncFile.WriteTextAtomic(messagePath, messageContents)
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

        string basePath;
    }
}
