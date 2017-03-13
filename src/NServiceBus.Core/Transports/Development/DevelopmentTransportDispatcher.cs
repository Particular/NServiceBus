namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class DevelopmentTransportDispatcher : IDispatchMessages
    {
        public DevelopmentTransportDispatcher()
        {
            // Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas")
            basePath = Path.Combine("c:\\bus");
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            DispatchUnicast(outgoingMessages.UnicastTransportOperations, context);
            DispatchMulticast(outgoingMessages.MulticastTransportOperations, context);

            return TaskEx.CompletedTask;
        }

        void DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, ContextBag context)
        {
            foreach (var transportOperation in transportOperations)
            {
                var subscribers = GetSubscribersFor(transportOperation.MessageType);

                foreach (var subscriber in subscribers)
                {
                    WriteMessage(subscriber, transportOperation, context);
                }
            }
        }


        void DispatchUnicast(IEnumerable<UnicastTransportOperation> transportOperations, ContextBag context)
        {
            foreach (var transportOperation in transportOperations)
            {
                WriteMessage(transportOperation.Destination, transportOperation, context);
            }
        }

        void WriteMessage(string destination, IOutgoingTransportOperation transportOperation, ContextBag context)
        {
            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyPath = Path.Combine(destinationPath, ".bodies", nativeMessageId) + ".xml"; //TODO: pick the correct ending based on the serialized type

            File.WriteAllBytes(bodyPath, transportOperation.Message.Body);

            var messageContents = new List<string>
            {
                bodyPath,
                HeaderSerializer.ToXml(transportOperation.Message.Headers)
            };

            DirectoryBasedTransaction transaction;

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".txt";

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated &&
                context.TryGet(out transaction))
            {
                transaction.Enlist(messagePath, messageContents);
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
            var allEventTypes = new List<Type>
            {
                messageType
            };

            allEventTypes.AddRange(messageType.GetInterfaces().Where(i => !IsCoreMarkerInterface(i)));

            var currentType = messageType;

            while (currentType.BaseType != null)
            {
                //do not include the marker interfaces
                if (IsCoreMarkerInterface(currentType))
                {
                    break;
                }

                currentType = currentType.BaseType;
            }
            return allEventTypes;
        }

        static bool IsCoreMarkerInterface(Type type)
        {
            return type.Assembly == typeof(IMessage).Assembly;
        }

        string basePath;
    }
}