namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Transactions;
    using Channels;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using log4net;
    using Notifications;
    using ObjectBuilder;
    using Persistence;
    using Sending;
    using Unicast.Transport;
    using Utils;

    public class IdempotentReceiver : IReceiveMessagesFromSites
    {
        public IdempotentReceiver(IBuilder builder, IPersistMessages persister)
        {
            this.builder = builder;
            this.persister = persister;
        }

        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        public IDataBus DataBus { get; set; }


        public void Start(Channel channel)
        {

            channelReceiver = (IChannelReceiver)builder.Build(channel.Receiver);

            channelReceiver.DataReceived += DataReceivedOnChannel;
            channelReceiver.Start(channel.ReceiveAddress, channel.NumWorkerThreads);
        }

        void DataReceivedOnChannel(object sender, DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = GetCallInfo(e);

                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);


                //todo this is a msmq specific validation and should be moved to the layer above that is sending the message onto the main transport
                //if (callInfo.Type == CallType.Submit && e.Data.Length > 4 * 1024 * 1024)
                //    throw new Exception("Cannot accept messages larger than 4MB.");

                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew,
                        new TransactionOptions
                        {
                            IsolationLevel = IsolationLevel.ReadCommitted,
                            Timeout = TimeSpan.FromSeconds(30)
                        }))
                {
                    switch (callInfo.Type)
                    {
                        case CallType.Submit: HandleSubmit(callInfo); break;
                        case CallType.DatabusProperty: HandleDatabusProperty(callInfo); break;
                        case CallType.Ack: HandleAck(callInfo); break;
                    }

                    scope.Complete();
                }

            }
        }

        CallInfo GetCallInfo(DataReceivedOnChannelArgs receivedData)
        {
            var headers = receivedData.Headers;

            string callType = headers[GatewayHeaders.CallTypeHeader];
            if (!Enum.IsDefined(typeof(CallType), callType))
                throw new HttpChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");

            var type = (CallType)Enum.Parse(typeof(CallType), callType);

            var clientId = headers[GatewayHeaders.ClientIdHeader];
            if (clientId == null)
                throw new HttpChannelException(400, "Required header '" + GatewayHeaders.ClientIdHeader + "' missing.");

            var md5 = headers[HttpHeaders.ContentMd5Key];

            if (md5 == null)
                throw new HttpChannelException(400, "Required header '" + HttpHeaders.ContentMd5Key + "' missing.");

            var hash = Hasher.Hash(receivedData.Data);

            if (receivedData.Data.Length > 0 && hash != md5)
                throw new HttpChannelException(412, "MD5 hash received does not match hash calculated on server. Consider resubmitting.");


            return new CallInfo
            {
                ClientId = clientId,
                Type = type,
                Headers = headers,
                Data = receivedData.Data
            };
        }



        void HandleSubmit(CallInfo callInfo)
        {
            persister.InsertMessage(callInfo.ClientId, DateTime.UtcNow, callInfo.Data, callInfo.Headers);
        }

        void HandleDatabusProperty(CallInfo callInfo)
        {
            if (DataBus == null)
                throw new InvalidOperationException("Databus transmission received without a databus configured");

            TimeSpan timeToBeReceived;

            if (!TimeSpan.TryParse(callInfo.Headers["NServiceBus.TimeToBeReceived"], out timeToBeReceived))
                timeToBeReceived = TimeSpan.FromHours(1);

            string newDatabusKey;

            using (callInfo.Data)
                newDatabusKey = DataBus.Put(callInfo.Data, timeToBeReceived);

            persister.UpdateHeader(callInfo.ClientId, callInfo.Headers[GatewayHeaders.DatabusKey], newDatabusKey);
        }

        void HandleAck(CallInfo callInfo)
        {
            byte[] outMessage;
            IDictionary<string, string> outHeaders;

            if (!persister.AckMessage(callInfo.ClientId, out outMessage, out outHeaders))
                return;

            var msg = new TransportMessage
                          {
                              Body = outMessage,
                              Headers = new Dictionary<string, string>(),
                              MessageIntent = MessageIntentEnum.Send,
                              Recoverable = true
                          };


            if (outHeaders[GatewayHeaders.IsGatewayMessage] != null)
                HeaderMapper.Map(outHeaders, msg);


            MessageReceived(this, new MessageReceivedOnChannelArgs { Message = msg });
        }

        public void Dispose()
        {
            channelReceiver.DataReceived -= DataReceivedOnChannel;
            channelReceiver.Dispose();
        }

        IChannelReceiver channelReceiver;
        readonly IBuilder builder;
        readonly IPersistMessages persister;

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

    }
}