namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Transport;

    class DevelopmentTransportDispatcher : IDispatchMessages
    {
        public DevelopmentTransportDispatcher(string basePath)
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
            return Task.WhenAll(operations.Select(operation => WriteMessage(operation.Destination, operation, transaction)));
        }

        async Task WriteMessage(string destination, IOutgoingTransportOperation transportOperation, TransportTransaction transaction)
        {
            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyDir = Path.Combine(destinationPath, ".bodies");

            Directory.CreateDirectory(bodyDir);

            var bodyPath = Path.Combine(bodyDir, nativeMessageId) + ".txt";

            await AsyncFile.WriteBytes(bodyPath, transportOperation.Message.Body).ConfigureAwait(false);

            var messageContents = new List<string>
            {
                bodyPath,
                HeaderSerializer.Serialize(transportOperation.Message.Headers)
            };

            DateTime? timeToDeliver = null;
            DelayDeliveryWith delayDeliveryWith;

            if (transportOperation.DeliveryConstraints.TryGet(out delayDeliveryWith))
            {
                timeToDeliver = DateTime.UtcNow + delayDeliveryWith.Delay;
            }

            DoNotDeliverBefore doNotDeliverBefore;

            if (transportOperation.DeliveryConstraints.TryGet(out doNotDeliverBefore))
            {
                timeToDeliver = doNotDeliverBefore.At;
            }


            if (timeToDeliver.HasValue)
            {
                if (timeToDeliver.Value.Millisecond > 0)
                {
                    timeToDeliver += TimeSpan.FromSeconds(1);
                }

                destinationPath = Path.Combine(destinationPath, ".delayed", timeToDeliver.Value.ToString("yyyyMMddHHmmss"));

                Directory.CreateDirectory(destinationPath);
            }

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".txt";

            IDevelopmentTransportTransaction directoryBasedTransaction;

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated &&
                transaction.TryGet(out directoryBasedTransaction))
            {
                directoryBasedTransaction.Enlist(messagePath, messageContents);
            }
            else
            {
                var tempFile = Path.GetTempFileName();

                //write to temp file first so we can do a atomic move
                //this avoids the file being locked when the receiver tries to process it
                File.WriteAllLines(tempFile, messageContents);
                File.Move(tempFile, messagePath);
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
                    var allText = await ReadTextAsync(file).ConfigureAwait(false);
                    subscribers.Add(allText);
                }
            }

            return subscribers;
        }

        //TODO: merge with dev persistence
        static async Task<string> ReadTextAsync(string filePath)
        {
            using (var sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                var builder = new StringBuilder();

                var buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, numRead));
                }

                return builder.ToString();
            }
        }
        static IEnumerable<Type> GetPotentialEventTypes(Type messageType)
        {
            var allEventTypes = new HashSet<Type>();

            var currentType = messageType;
            do
            {
                //do not include the marker interfaces
                if (IsCoreMarkerInterface(currentType))
                {
                    break;
                }

                foreach (var type in messageType.GetInterfaces().Where(i => !IsCoreMarkerInterface(i)))
                {
                    allEventTypes.Add(type);
                }
                allEventTypes.Add(currentType);

                currentType = currentType.BaseType;
            } while (currentType != null);

            return allEventTypes;
        }

        static bool IsCoreMarkerInterface(Type type)
        {
            return type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);
        }

        string basePath;
    }
}