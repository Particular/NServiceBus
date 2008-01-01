using System;
using System.Collections.Generic;
using System.Text;
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

        private int numberOfWorkerThreads;
        public int NumberOfWorkerThreads
        {
            set { numberOfWorkerThreads = value; }
        }

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

        public void Publish(Msg message, string address)
        {
            MemoryStream ms = new MemoryStream();

            formatter.Serialize(ms, message);
            byte[] toSend = ms.GetBuffer();

            this.sender.Send(toSend, toSend.Length, address, this.port);
        }

        #endregion

        #region ITransport Members

        public void Start()
        {
            this.sender = new UdpClient();
            this.sender.EnableBroadcast = true;

            this.receiver = new UdpClient();
            this.receiver.EnableBroadcast = true;
            this.receiver.Client.ExclusiveAddressUse = false;
            this.receiver.Client.MulticastLoopback = false;
            this.receiver.Client.Bind(new IPEndPoint(this.localAddress, this.port));

            this.threads = new List<WorkerThread>(this.numberOfWorkerThreads);
            for (int i = 0; i < this.numberOfWorkerThreads; i++)
                threads.Add(new WorkerThread(ReceiveMessage));

            foreach (WorkerThread thread in this.threads)
                thread.Start();
        }

        public IList<Type> MessageTypesToBeReceived
        {
            set {  }
        }

        public void Send(Msg m, string destination)
        {
            this.Send(m, destination, false, TimeSpan.MaxValue);
        }

        public void Send(Msg m, string destination, bool recoverable, TimeSpan timeToBeReceived)
        {
            MemoryStream stream = new MemoryStream();
            this.formatter.Serialize(stream, m);

            byte[] toSend = stream.GetBuffer();

            this.sender.Send(toSend, toSend.Length, destination, this.port);
        }

        public void ReceiveMessageLater(Msg m)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string Address
        {
            get { return Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(); }
        }

        public event EventHandler<MsgReceivedEventArgs> MsgReceived;

        public void Dispose()
        {
            foreach(WorkerThread thread in this.threads)
                thread.Stop();

            this.receiver.Close();
        }

        #endregion

        #region helper methods

        private void ReceiveMessage()
        {
            IPEndPoint from = null;
            byte[] message = this.receiver.Receive(ref from);

            try
            {
                MemoryStream stream = new MemoryStream(message);
                Msg result = this.formatter.Deserialize(stream) as Msg;

                if (this.MsgReceived != null)
                    this.MsgReceived(this, new MsgReceivedEventArgs(result));
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                unhandledMessages.Debug(message);
            }
        }

        #endregion

        #region fields

        private BinaryFormatter formatter = new BinaryFormatter();
        private IList<WorkerThread> threads;
        private UdpClient sender;
        private UdpClient receiver;

        private static ILog logger = LogManager.GetLogger(typeof(UdpTransport).Name);
        private static ILog unhandledMessages = LogManager.GetLogger(typeof(UdpTransport).Name + ":Unhandled");

        #endregion
    }
}
