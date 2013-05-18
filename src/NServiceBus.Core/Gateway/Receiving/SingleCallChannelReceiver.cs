namespace NServiceBus.Gateway.Receiving
{
    using Channels;
    using Channels.Http;
    using DataBus;
    using Deduplication;
    using HeaderManagement;
    using log4net;
    using Notifications;
    using Sending;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Transactions;
    using Utils;

    public class SingleCallChannelReceiver : IReceiveMessagesFromSites
    {
        public SingleCallChannelReceiver(IChannelFactory channelFactory, IDeduplicateMessages deduplicator, DataBusHeaderManager headerManager, IdempotentChannelReceiver receiver)
        {
            this.channelFactory = channelFactory;
            this.deduplicator = deduplicator;
            this.headerManager = headerManager;
            this.receiver = receiver;
        }

        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        public IDataBus DataBus { get; set; }

        public void Start(Channel channel, int numWorkerThreads)
        {
            channelReceiver = channelFactory.GetReceiver(channel.Type);
            channelReceiver.DataReceived += DataReceivedOnChannel;
            receiver.MessageReceived += MessageReceivedOnOldChannel;
            channelReceiver.Start(channel.Address,numWorkerThreads);
        }

        void MessageReceivedOnOldChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            MessageReceived(sender, e);
        }

        void DataReceivedOnChannel(object sender, DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = GetCallInfo(e);

                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);

                using (var scope = DefaultTransactionScope())
                {
                    switch (callInfo.Type)
                    {
                        case CallType.SingleCallDatabusProperty: HandleDatabusProperty(callInfo); break;
                        case CallType.SingleCallSubmit: HandleSubmit(callInfo); break;
                        default: receiver.DispatchReceivedCallInfo(callInfo); break;
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

        CallInfo GetCallInfo(DataReceivedOnChannelArgs receivedData)
        {
            var headers = receivedData.Headers;
       
            string callType = headers[GatewayHeaders.CallTypeHeader];
            if (!Enum.IsDefined(typeof(CallType), callType))
                throw new ChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");

            var type = (CallType)Enum.Parse(typeof(CallType), callType);

            var clientId = headers[GatewayHeaders.ClientIdHeader];
            if (clientId == null)
                throw new ChannelException(400, "Required header '" + GatewayHeaders.ClientIdHeader + "' missing.");

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
            using (var stream = new MemoryStream())
            {
                callInfo.Data.CopyTo_net35(stream);
                stream.Position = 0;

                CheckHashOfGatewayStream(stream, callInfo.Headers[HttpHeaders.ContentMd5Key]);

                if (deduplicator.DeduplicateMessage(callInfo.ClientId, DateTime.UtcNow))
                {
                    var msg = HeaderMapper.Map(headerManager.Reassemble(callInfo.ClientId, callInfo.Headers));
                    msg.Body = new byte[stream.Length];
                    stream.Read(msg.Body, 0, msg.Body.Length);

                    MessageReceived(this, new MessageReceivedOnChannelArgs { Message = msg });
                }
                else
                    Logger.InfoFormat("Message with id: {0} is already on the bus, dropping the request", callInfo.ClientId);
            }
        }

        void HandleDatabusProperty(CallInfo callInfo)
        {
            if (DataBus == null)
                throw new InvalidOperationException("Databus transmission received without a configured databus");

            TimeSpan timeToBeReceived;
            if (!TimeSpan.TryParse(callInfo.Headers["NServiceBus.TimeToBeReceived"], out timeToBeReceived))
                timeToBeReceived = TimeSpan.FromHours(1);

            var newDatabusKey = DataBus.Put(callInfo.Data, timeToBeReceived);
            using(var databusStream = DataBus.Get(newDatabusKey))
                CheckHashOfGatewayStream(databusStream, callInfo.Headers[HttpHeaders.ContentMd5Key]);

            var specificDataBusHeaderToUpdate = callInfo.Headers[GatewayHeaders.DatabusKey];
            headerManager.InsertHeader(callInfo.ClientId, specificDataBusHeaderToUpdate, newDatabusKey);
        }

        void CheckHashOfGatewayStream(Stream input, string md5Hash)
        {
            if (md5Hash == null)
                throw new ChannelException(400, "Required header '" + HttpHeaders.ContentMd5Key + "' missing.");

            if (md5Hash != Hasher.Hash(input))
                throw new ChannelException(412, "MD5 hash received does not match hash calculated on server. Please resubmit.");
        }

        public void Dispose()
        {
            channelReceiver.DataReceived -= DataReceivedOnChannel;
            receiver.MessageReceived -= MessageReceivedOnOldChannel;
            channelReceiver.Dispose();
        }

        IChannelReceiver channelReceiver;
        readonly IChannelFactory channelFactory;
        readonly IDeduplicateMessages deduplicator;
        readonly DataBusHeaderManager headerManager;

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        readonly IdempotentChannelReceiver receiver;

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
