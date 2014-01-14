namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.IO;
    using System.Transactions;
    using Channels;
    using DataBus;
    using Deduplication;
    using HeaderManagement;
    using Logging;
    using Notifications;
    using Sending;
    using Utils;

    public class SingleCallChannelReceiver : IReceiveMessagesFromSites
    {
        public SingleCallChannelReceiver(IChannelFactory channelFactory, IDeduplicateMessages deduplicator,
            DataBusHeaderManager headerManager, IdempotentChannelReceiver receiver)
        {
            this.channelFactory = channelFactory;
            this.deduplicator = deduplicator;
            this.headerManager = headerManager;
            this.receiver = receiver;
        }

        public IDataBus DataBus { get; set; }
        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        public void Start(Channel channel, int numberOfWorkerThreads)
        {
            channelReceiver = channelFactory.GetReceiver(channel.Type);
            channelReceiver.DataReceived += DataReceivedOnChannel;
            receiver.MessageReceived += MessageReceivedOnOldChannel;
            channelReceiver.Start(channel.Address, numberOfWorkerThreads);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (receiver != null)
            {
                receiver.MessageReceived -= MessageReceivedOnOldChannel;
                receiver.Dispose();
            }

            if (channelReceiver != null)
            {
                channelReceiver.DataReceived -= DataReceivedOnChannel;
                channelReceiver.Dispose();
            }
        }

        void MessageReceivedOnOldChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            MessageReceived(sender, e);
        }

        void DataReceivedOnChannel(object sender, DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = ChannelReceiverHeaderReader.GetCallInfo(e);

                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);

                using (var scope = DefaultTransactionScope())
                {
                    switch (callInfo.Type)
                    {
                        case CallType.SingleCallDatabusProperty:
                            HandleDatabusProperty(callInfo);
                            break;
                        case CallType.SingleCallSubmit:
                            HandleSubmit(callInfo);
                            break;
                        default:
                            receiver.DispatchReceivedCallInfo(callInfo);
                            break;
                    }
                    scope.Complete();
                }
            }
        }

        static TransactionScope DefaultTransactionScope()
        {
            return new TransactionScope(TransactionScopeOption.RequiresNew,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(30)
                });
        }


        void HandleSubmit(CallInfo callInfo)
        {
            using (var stream = new MemoryStream())
            {
                callInfo.Data.CopyTo(stream);
                stream.Position = 0;

                Hasher.Verify(stream, callInfo.Md5);

                var msg = HeaderMapper.Map(headerManager.Reassemble(callInfo.ClientId, callInfo.Headers));
                msg.Body = new byte[stream.Length];
                stream.Read(msg.Body, 0, msg.Body.Length);

                if (deduplicator.DeduplicateMessage(callInfo.ClientId, DateTime.UtcNow))
                {
                    MessageReceived(this, new MessageReceivedOnChannelArgs {Message = msg});
                }
                else
                {
                    Logger.InfoFormat("Message with id: {0} is already on the bus, dropping the request", callInfo.ClientId);
                }
            }
        }

        void HandleDatabusProperty(CallInfo callInfo)
        {
            if (DataBus == null)
            {
                throw new InvalidOperationException("Databus transmission received without a configured databus");
            }

            
            var newDatabusKey = DataBus.Put(callInfo.Data, callInfo.TimeToBeReceived);
            using (var databusStream = DataBus.Get(newDatabusKey))
            {
                Hasher.Verify(databusStream, callInfo.Md5);
            }

            var specificDataBusHeaderToUpdate = callInfo.ReadDataBus();
            headerManager.InsertHeader(callInfo.ClientId, specificDataBusHeaderToUpdate, newDatabusKey);
        }


        static ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

        IChannelFactory channelFactory;
        IDeduplicateMessages deduplicator;
        DataBusHeaderManager headerManager;

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")] 
        IdempotentChannelReceiver receiver;

        IChannelReceiver channelReceiver;
    }
}