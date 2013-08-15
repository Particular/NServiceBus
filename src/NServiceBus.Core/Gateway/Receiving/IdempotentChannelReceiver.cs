namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Channels;
    using Channels.Http;
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

        public void Start(Channel channel, int numWorkerThreads)
        {
            channelReceiver = channelFactory.GetReceiver(channel.Type);

            channelReceiver.DataReceived += DataReceivedOnChannel;
            channelReceiver.Start(channel.Address, numWorkerThreads);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (channelReceiver != null)
            {
                channelReceiver.DataReceived -= DataReceivedOnChannel;
                channelReceiver.Dispose();
            }

            disposed = true;
        }

        void DataReceivedOnChannel(object sender, DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = GetCallInfo(e);

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

        CallInfo GetCallInfo(DataReceivedOnChannelArgs receivedData)
        {
            var headers = receivedData.Headers;

            var callType = headers[GatewayHeaders.CallTypeHeader];
            if (!Enum.IsDefined(typeof(CallType), callType))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");
            }

            var type = (CallType) Enum.Parse(typeof(CallType), callType);

            var clientId = headers[GatewayHeaders.ClientIdHeader];
            if (clientId == null)
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.ClientIdHeader + "' missing.");
            }

            var md5 = headers[HttpHeaders.ContentMd5Key];

            if (md5 == null)
            {
                throw new ChannelException(400, "Required header '" + HttpHeaders.ContentMd5Key + "' missing.");
            }

            var hash = Hasher.Hash(receivedData.Data);

            if (receivedData.Data.Length > 0 && hash != md5)
            {
                throw new ChannelException(412,
                    "MD5 hash received does not match hash calculated on server. Consider resubmitting.");
            }


            return new CallInfo
            {
                ClientId = clientId,
                Type = type,
                Headers = headers,
                Data = receivedData.Data,
                AutoAck = headers.ContainsKey(GatewayHeaders.AutoAck)
            };
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

            TimeSpan timeToBeReceived;

            if (!TimeSpan.TryParse(callInfo.Headers["NServiceBus.TimeToBeReceived"], out timeToBeReceived))
            {
                timeToBeReceived = TimeSpan.FromHours(1);
            }

            string newDatabusKey;

            using (callInfo.Data)
            {
                newDatabusKey = DataBus.Put(callInfo.Data, timeToBeReceived);
            }

            var specificDataBusHeaderToUpdate = callInfo.Headers[GatewayHeaders.DatabusKey];

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

        static readonly ILog Logger = LogManager.GetLogger(typeof(IdempotentChannelReceiver));

        readonly IChannelFactory channelFactory;
        readonly IPersistMessages persister;
        IChannelReceiver channelReceiver;
        bool disposed;
    }
}