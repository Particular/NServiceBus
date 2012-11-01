﻿namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Channels;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using Unicast.Transport;
    using log4net;
    using Notifications;
    using Persistence;
    using Sending;
    using Utils;

    public class IdempotentChannelReceiver : IReceiveMessagesFromSites
    {
        public IdempotentChannelReceiver(IChannelFactory channelFactory, IPersistMessages persister)
        {
            this.channelFactory = channelFactory;
            this.persister = persister;
        }

        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        public IDataBus DataBus { get; set; }


        public void Start(Channel channel, int numWorkerThreads)
        {

            channelReceiver = channelFactory.GetReceiver(channel.Type);

            channelReceiver.DataReceived += DataReceivedOnChannel;
            channelReceiver.Start(channel.Address,numWorkerThreads);
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
                        case CallType.Submit: HandleSubmit(callInfo); break;
                        case CallType.DatabusProperty: HandleDatabusProperty(callInfo); break;
                        case CallType.Ack: HandleAck(callInfo); break;
                    }

                    scope.Complete();
                }

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
                Data = receivedData.Data,
                AutoAck = headers.ContainsKey(GatewayHeaders.AutoAck)
            };
        }

        void HandleSubmit(CallInfo callInfo)
        {
            persister.InsertMessage(callInfo.ClientId, DateTime.UtcNow, callInfo.Data, callInfo.Headers);

            if(callInfo.AutoAck)
                HandleAck(callInfo);
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
            
            var msg = new TransportMessage
                          {
                              Body = outMessage,
                              Headers = new Dictionary<string, string>(),
                              MessageIntent = MessageIntentEnum.Send,
                              Recoverable = true
                          };


            if (outHeaders.ContainsKey(GatewayHeaders.IsGatewayMessage))
                HeaderMapper.Map(outHeaders, msg);


            MessageReceived(this, new MessageReceivedOnChannelArgs { Message = msg });
        }

        public void Dispose()
        {
            channelReceiver.DataReceived -= DataReceivedOnChannel;
            channelReceiver.Dispose();
        }

        IChannelReceiver channelReceiver;
        readonly IChannelFactory channelFactory;
        readonly IPersistMessages persister;

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

    }
}
