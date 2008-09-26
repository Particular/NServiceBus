using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus.Unicast.Transport;
using System.Net;
using NServiceBus.Serialization;
using nsoftware.IPWorks;
using Utils;

namespace NServiceBus.Unicast.Transport.Http
{
    /// <summary>
    /// Implements the <see cref="ITransport"/> interface using HTTP.
    /// Does not support the methods <see cref="GetNumberOfPendingMessages"/> or <see cref="AbortHandlingCurrentMessage"/>.
    /// </summary>
    public class Transport : ITransport
    {
        #region members

        private readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();
        private readonly IList<WorkerThread> senderThreads = new List<WorkerThread>();
        private readonly BlockingQueue queue = new BlockingQueue();
        private string address;
        private HttpListener listener;

        private IMessageSerializer messageSerializer;
        private int numberOfWorkerThreads;
        private int numberOfSenderThreads;
        private int timeoutInSeconds=10;


        #endregion

        #region additional config

        public virtual int TimeoutInSeconds
        {
            set { this.timeoutInSeconds = value; }
        }

        public virtual int NumberOfSenderThreads
        {
            get { return this.numberOfSenderThreads; }
            set { this.numberOfSenderThreads = value; }
        }
        
        public virtual IMessageSerializer MessageSerializer
        {
            set { this.messageSerializer = value; }
        }

        #endregion

        #region ITransport config

        public IList<Type> MessageTypesToBeReceived
        {
            set { this.messageSerializer.Initialize(GetExtraTypes(value)); }
        }

        public virtual int NumberOfWorkerThreads
        {
            get { return this.numberOfWorkerThreads; }
            set { this.numberOfWorkerThreads = value; }
        }

        public virtual string Address
        {
            get { return address; }
            set { this.address = value; }
        }

        #endregion

        #region ITransport events

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
        public event EventHandler StartedMessageProcessing;
        public event EventHandler FinishedMessageProcessing;

        #endregion

        #region ITransport methods

        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(this.address);
            
            listener.Start();

            for (int i = 0; i < numberOfSenderThreads; i++)
                this.AddSenderThread().Start();

            for (int i = 0; i < numberOfWorkerThreads; i++)
                this.AddWorkerThread().Start();
        }

        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
            lock (this.workerThreads)
            {
                int current = this.workerThreads.Count;

                if (targetNumberOfWorkerThreads == current)
                    return;

                if (targetNumberOfWorkerThreads < current)
                {
                    for (int i = targetNumberOfWorkerThreads; i < current; i++)
                        this.workerThreads[i].Stop();

                    return;
                }

                if (targetNumberOfWorkerThreads > current)
                {
                    for (int i = current; i < targetNumberOfWorkerThreads; i++)
                        this.AddWorkerThread().Start();

                    return;
                }
            }
        }

        public void Send(TransportMessage m, string destination)
        {
            m.Id = Guid.NewGuid().ToString("N");

            this.queue.Enqueue(new SendStateObject(m, destination));
        }

        public void ReceiveMessageLater(TransportMessage m)
        {
            this.Send(m, this.address);
        }

        public int GetNumberOfPendingMessages()
        {
            throw new NotImplementedException();
        }

        public void AbortHandlingCurrentMessage()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.listener.Close();
        }

        #endregion

        #region helper methods

        private void Receive()
        {
            HttpListenerContext context = listener.GetContext();

            TransportMessage transportMessage = GetTransportMessage(context, this.messageSerializer);

            context.Response.Close();

            this.OnStartedMessageProcessing();

            if (this.TransportMessageReceived != null)
                this.TransportMessageReceived(this, new TransportMessageReceivedEventArgs(transportMessage));

            this.OnFinishedMessageProcessing();
        }

        private void SendFromQueue()
        {
            SendStateObject state = this.queue.Dequeue() as SendStateObject;

            string destination = state.Destination;
            TransportMessage m = state.Message;

            nsoftware.IPWorks.Http request = new nsoftware.IPWorks.Http();
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = this.timeoutInSeconds;

            request.OtherHeaders = GetHeadersFrom(m);

            MemoryStream stream = new MemoryStream();
            this.messageSerializer.Serialize(m.Body, stream);

            request.PostDataB = stream.GetBuffer();

            try
            {
                request.Post(destination);
            }
            catch (IPWorksHttpException ex)
            {
                if (ex.Code != 201) //Timeout
                    throw;
            }
        }

        private WorkerThread AddSenderThread()
        {
            lock (this.senderThreads)
            {
                WorkerThread result = new WorkerThread(this.SendFromQueue);

                this.senderThreads.Add(result);

                result.Stopped += delegate(object sender, EventArgs e)
                                      {
                                          WorkerThread wt = sender as WorkerThread;
                                          lock (this.senderThreads)
                                              this.senderThreads.Remove(wt);
                                      };

                return result;
            }
        }

        private WorkerThread AddWorkerThread()
        {
            lock (this.workerThreads)
            {
                WorkerThread result = new WorkerThread(this.Receive);

                this.workerThreads.Add(result);

                result.Stopped += delegate(object sender, EventArgs e)
                                      {
                                          WorkerThread wt = sender as WorkerThread;
                                          lock (this.workerThreads)
                                              this.workerThreads.Remove(wt);
                                      };

                return result;
            }
        }

        private static string GetHeadersFrom(TransportMessage m)
        {
            return
                string.Format(
                    "CorrelationId:{0}\r\nId:{1}\r\nIdForCorrelation:{2}\r\nReturnAddress:{3}\r\nWindowsIdentityName:{4}",
                    m.CorrelationId, m.Id, m.IdForCorrelation, m.ReturnAddress, m.WindowsIdentityName);
        }

        /// <summary>
        /// Get a list of serializable types from the list of types provided.
        /// </summary>
        /// <param name="value">A list of types process.</param>
        /// <returns>A list of serializable types.</returns>
        private static Type[] GetExtraTypes(IEnumerable<Type> value)
        {
            List<Type> types = new List<Type>(value);

            if (!types.Contains(typeof(List<object>)))
                types.Add(typeof(List<object>));

            return types.ToArray();
        }

        private static TransportMessage GetTransportMessage(HttpListenerContext context, IMessageSerializer serializer)
        {
            TransportMessage transportMessage = new TransportMessage();
            transportMessage.CorrelationId = context.Request.Headers["CorrelationId"];
            transportMessage.Id = context.Request.Headers["Id"];
            transportMessage.IdForCorrelation = context.Request.Headers["IdForCorrelation"];
            transportMessage.ReturnAddress = context.Request.Headers["ReturnAddress"];
            transportMessage.WindowsIdentityName = context.Request.Headers["WindowsIdentityName"];

            if (transportMessage.IdForCorrelation == null || transportMessage.IdForCorrelation == string.Empty)
                transportMessage.IdForCorrelation = transportMessage.Id;

            transportMessage.Body = serializer.Deserialize(context.Request.InputStream);
            return transportMessage;
        }

        private void OnStartedMessageProcessing()
        {
            if (this.StartedMessageProcessing != null)
                this.StartedMessageProcessing(this, null);
        }

        private void OnFinishedMessageProcessing()
        {
            if (this.FinishedMessageProcessing != null)
                this.FinishedMessageProcessing(this, null);
        }

        #endregion
    }
}
