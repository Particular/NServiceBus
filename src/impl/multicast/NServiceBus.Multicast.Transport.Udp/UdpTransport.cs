using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using NServiceBus.Unicast.Transport;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Utils;
using Common.Logging;

namespace NServiceBus.Multicast.Transport.Udp
{
    public class UdpTransport : IMulticastTransport
    {
        #region config info

        private IPAddress localAddress = IPAddress.Parse("127.0.0.1");
        public string LocalAddress
        {
            set { localAddress = IPAddress.Parse(value); }
        }

        private int port;
        public int Port
        {
            set { this.port = value; }
        }

        #endregion

        #region IMulticastTransport members

        public void Subscribe(string address)
        {
            this.receiver.JoinMulticastGroup(IPAddress.Parse(address), 10);
        }

        public void Unsubscribe(string address)
        {
            this.receiver.DropMulticastGroup(IPAddress.Parse(address));
        }

        public void Publish(TransportMessage message, string address)
        {
            MemoryStream ms = new MemoryStream();

            formatter.Serialize(ms, message);
            byte[] toSend = ms.GetBuffer();

            this.sender.Send(toSend, toSend.Length, address, this.port);
        }

        #endregion

        #region ITransport Members

        private int _numberOfWorkerThreads;
        public int NumberOfWorkerThreads
        {
            get
            {
                lock (this.workerThreads)
                    return this.workerThreads.Count;
            }
            set
            {
                _numberOfWorkerThreads = value;
            }
        }

        public IList<Type> MessageTypesToBeReceived
        {
            set { }
        }

        public string Address
        {
            get { return Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(); }
        }

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;

        public void Start()
        {
            this.sender = new UdpClient();
            this.sender.EnableBroadcast = true;

            this.receiver = new UdpClient();
            this.receiver.EnableBroadcast = true;
            this.receiver.Client.ExclusiveAddressUse = false;
            this.receiver.Client.MulticastLoopback = false;
            this.receiver.Client.Bind(new IPEndPoint(this.localAddress, this.port));

            for (int i = 0; i < this._numberOfWorkerThreads; i++)
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

        public void StopSendingReadyMessages()
        {
            this.canSendReadyMessages = false;
        }

        public void ContinueSendingReadyMessages()
        {
            this.canSendReadyMessages = true;
        }

        public void Send(TransportMessage m, string destination)
        {
            this.Send(m, destination, false, TimeSpan.MaxValue);
        }

        public void Send(TransportMessage m, string destination, bool recoverable, TimeSpan timeToBeReceived)
        {
            MemoryStream stream = new MemoryStream();
            this.formatter.Serialize(stream, m);

            byte[] toSend = stream.GetBuffer();

            this.sender.Send(toSend, toSend.Length, destination, this.port);
        }

        public void ReceiveMessageLater(TransportMessage m)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Dispose()
        {
            foreach(WorkerThread thread in this.workerThreads)
                thread.Stop();

            this.receiver.Close();
        }

        #endregion

        #region helper methods

        private void Receive()
        {
            IPEndPoint from = null;
            byte[] message = this.receiver.Receive(ref from);

            try
            {
                MemoryStream stream = new MemoryStream(message);
                TransportMessage result = this.formatter.Deserialize(stream) as TransportMessage;

                if (this.TransportMessageReceived != null)
                    this.TransportMessageReceived(this, new TransportMessageReceivedEventArgs(result));
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                unhandledMessages.Debug(message);
            }
        }

        private WorkerThread AddWorkerThread()
        {
            lock (this.workerThreads)
            {
                WorkerThread result = new WorkerThread(this.Receive);

                this.workerThreads.Add(result);

                result.Stopped += delegate(object sndr, EventArgs e)
                                      {
                                          WorkerThread wt = sndr as WorkerThread;
                                          lock (this.workerThreads)
                                              this.workerThreads.Remove(wt);
                                      };

                return result;
            }
        }

        #endregion

        #region fields

        private BinaryFormatter formatter = new BinaryFormatter();
        private IList<WorkerThread> workerThreads = new List<WorkerThread>();
        private UdpClient sender;
        private UdpClient receiver;

        /// <summary>
        /// Accessed by multiple threads.
        /// </summary>
        private volatile bool canSendReadyMessages = true;

        private static ILog logger = LogManager.GetLogger(typeof(UdpTransport).Name);
        private static ILog unhandledMessages = LogManager.GetLogger(typeof(UdpTransport).Name + ":Unhandled");

        #endregion
    }
}
