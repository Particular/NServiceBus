namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Channels;
    using DataBus;
    using HeaderManagement;
    using Logging;
    using Notifications;
    using Persistence;
    using Sending;
    using Utils;

    [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public class IdempotentChannelReceiver : IReceiveMessagesFromSites
    {
        public IdempotentChannelReceiver(IChannelFactory channelFactory, IPersistMessages persister)
        {
            this.channelFactory = channelFactory;
            this.persister = persister;
        }

        public IDataBus DataBus { get; set; }
        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        public void Start(Channel channel, int numberOfWorkerThreads)
        {
            channelReceiver = channelFactory.GetReceiver(channel.Type);

            channelReceiver.DataReceived += DataReceivedOnChannel;
            channelReceiver.Start(channel.Address, numberOfWorkerThreads);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (channelReceiver != null)
            {
                channelReceiver.DataReceived -= DataReceivedOnChannel;
                channelReceiver.Dispose();
            }
        }

        void DataReceivedOnChannel(object sender, DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = ChannelReceiverHeaderReader.GetCallInfo(e);

                Hasher.Verify(callInfo.Data,callInfo.Md5);
                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);

                using (var scope = DefaultTransactionScope())
                {
                    DispatchReceivedCallInfo(callInfo);
                    scope.Complete();
                }
            }
        }

        internal void DispatchReceivedCallInfo(CallInfo callInfo)
        {
            switch (callInfo.Type)
            {
                case CallType.Submit:
                    HandleSubmit(callInfo);
                    break;
                case CallType.DatabusProperty:
                    HandleDatabusProperty(callInfo);
                    break;
                case CallType.Ack:
                    HandleAck(callInfo);
                    break;
            }
        }

        static TransactionScope DefaultTransactionScope()
        {
            return new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(30)
                });
        }

        void HandleSubmit(CallInfo callInfo)
        {
            persister.InsertMessage(callInfo.ClientId, DateTime.UtcNow, callInfo.Data, callInfo.Headers);

            if (callInfo.AutoAck)
            {
                HandleAck(callInfo);
            }
        }

        void HandleDatabusProperty(CallInfo callInfo)
        {
            if (DataBus == null)
            {
                throw new InvalidOperationException("Databus transmission received without a databus configured");
            }
            
            string newDatabusKey;

            using (callInfo.Data)
            {
                newDatabusKey = DataBus.Put(callInfo.Data, callInfo.TimeToBeReceived);
            }

            var specificDataBusHeaderToUpdate = callInfo.ReadDataBus();

            persister.UpdateHeader(callInfo.ClientId, specificDataBusHeaderToUpdate, newDatabusKey);
        }


        void HandleAck(CallInfo callInfo)
        {
            byte[] outMessage;
            IDictionary<string, string> outHeaders;

            if (!persister.AckMessage(callInfo.ClientId, out outMessage, out outHeaders))
            {
                Logger.InfoFormat("Message with id: {0} is already acked, dropping the request", callInfo.ClientId);
                return;
            }


            var msg = HeaderMapper.Map(outHeaders);

            msg.Body = outMessage;

            MessageReceived(this, new MessageReceivedOnChannelArgs {Message = msg});
        }

        static ILog Logger = LogManager.GetLogger(typeof(IdempotentChannelReceiver));

        IChannelFactory channelFactory;
        IPersistMessages persister;
        IChannelReceiver channelReceiver;
    }
}