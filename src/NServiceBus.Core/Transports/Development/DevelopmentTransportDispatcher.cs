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
    using Transport;

    class DevelopmentTransportDispatcher : IDispatchMessages
    {
        public DevelopmentTransportDispatcher(string basePath)
        {
            this.basePath = basePath;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction);
            DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction);

            return TaskEx.CompletedTask;
        }

        void DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction)
        {
            foreach (var transportOperation in transportOperations)
            {
                var subscribers = GetSubscribersFor(transportOperation.MessageType);

                foreach (var subscriber in subscribers)
                {
                    WriteMessage(subscriber, transportOperation, transaction);
                }
            }
        }


        void DispatchUnicast(IEnumerable<UnicastTransportOperation> transportOperations, TransportTransaction transaction)
        {
            foreach (var transportOperation in transportOperations)
            {
                WriteMessage(transportOperation.Destination, transportOperation, transaction);
            }
        }

        void WriteMessage(string destination, IOutgoingTransportOperation transportOperation, TransportTransaction transaction)
        {
            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyDir = Path.Combine(destinationPath, ".bodies");

            Directory.CreateDirectory(bodyDir);


            var bodyPath = Path.Combine(bodyDir, nativeMessageId) + ".xml"; //TODO: pick the correct ending based on the serialized type

            File.WriteAllBytes(bodyPath, transportOperation.Message.Body);

            var messageContents = new List<string>
            {
                bodyPath,
                HeaderSerializer.ToXml(transportOperation.Message.Headers)
            };

            DelayDeliveryWith delayDeliveryWith;

            if (transportOperation.DeliveryConstraints.TryGet(out delayDeliveryWith))
            {
                var timeToDeliver = DateTime.UtcNow + delayDeliveryWith.Delay;

                if (timeToDeliver.Millisecond > 0)
                {
                    timeToDeliver += TimeSpan.FromSeconds(1);
                }

                destinationPath = Path.Combine(destinationPath, ".delayed", timeToDeliver.ToString("yyyyMMddHHmmss"));

                Directory.CreateDirectory(destinationPath);
            }


            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".txt";

            DirectoryBasedTransaction directoryBasedTransaction;

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

        IEnumerable<string> GetSubscribersFor(Type messageType)
        {
            var subscribers = new List<string>();

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
                    subscribers.Add(File.ReadAllText(file));
                }
            }

            return subscribers.Distinct();
        }

        static List<Type> GetPotentialEventTypes(Type messageType)
        {
            var allEventTypes = new List<Type>();


            var currentType = messageType;

            while (currentType.BaseType != null)
            {
                //do not include the marker interfaces
                if (IsCoreMarkerInterface(currentType))
                {
                    break;
                }

                allEventTypes.AddRange(messageType.GetInterfaces().Where(i => !IsCoreMarkerInterface(i)));
                allEventTypes.Add(currentType);

                currentType = currentType.BaseType;
            }

            return allEventTypes.Distinct().ToList();
        }

        static bool IsCoreMarkerInterface(Type type)
        {
            return type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);
        }

        string basePath;
    }
}