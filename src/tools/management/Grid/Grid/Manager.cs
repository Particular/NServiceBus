using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NServiceBus.Grid.Messages;
using NServiceBus;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Messaging;


namespace Grid
{
    public static class Manager
    {
        private static readonly string path = "storage.txt";

        private static readonly int refreshInterval = 5;
        public static int RefreshInterval
        {
            get
            {
                lock (typeof(Manager))
                    return refreshInterval;
            }
        }

        private static IBus bus;

        private static List<ManagedEndpoint> endpoints = new List<ManagedEndpoint>();

        private static readonly Timer timer = new Timer(CheckNumberOfMessages);

        static Manager()
        {
            if (File.Exists(path))
            {
                try
                {
                    Stream stream = File.OpenRead(path);
                    BinaryFormatter formatter = new BinaryFormatter();

                    object result = formatter.Deserialize(stream);
                    stream.Close();

                    endpoints = result as List<ManagedEndpoint>;
                }
                catch(Exception)
                {
                    // intentionally swallow exception
                }
            }

            timer.Change(0, refreshInterval*1000);
        }

        public static void SetBus(IBus b)
        {
            bus = b;
        }

        private static void CheckNumberOfMessages(object state)
        {
            timer.Change(int.MaxValue, int.MaxValue);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            List<ManagedEndpoint> myList;

            lock(typeof(Manager))
                myList = new List<ManagedEndpoint>(endpoints);

            foreach(ManagedEndpoint endpoint in myList)
            {
                MSMQ.MSMQManagementClass qMgmt = new MSMQ.MSMQManagementClass();
                object machine = Type.Missing;
                object missing = Type.Missing;
                object formatName = "DIRECT=OS:" + Environment.MachineName + "\\private$\\" + endpoint.Queue;

                try
                {
                    qMgmt.Init(ref machine, ref missing, ref formatName);
                    endpoint.SetNumberOfMessages(qMgmt.MessageCount);

                    MessageQueue q = new MessageQueue("FormatName:" + formatName as string);

                    MessagePropertyFilter mpf = new MessagePropertyFilter();
                    mpf.SetAll();

                    q.MessageReadPropertyFilter = mpf;

                    Message m = q.Peek();
                    if (m != null)
                        endpoint.AgeOfOldestMessage = DateTime.Now - m.SentTime;
                }
                catch
                {
                    //intentionally swallow bad endpoints
                }
            }

            watch.Stop();

            long due = refreshInterval*1000 - watch.ElapsedMilliseconds;
            due = (due < 0 ? 0 : due);

            timer.Change(due, refreshInterval*1000);
        }

        public static void Save()
        {
            if (!File.Exists(path))
                File.CreateText(path).Close();

            Stream stream = File.OpenWrite(path);
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, endpoints);

            stream.Close();
        }

        public static List<ManagedEndpoint> GetManagedEndpoints()
        {
            lock (typeof(Manager))
                return new List<ManagedEndpoint>(endpoints);
        }

        public static void StoreManagedEndpoints(List<ManagedEndpoint> points)
        {
            lock(typeof(Manager))
                endpoints = points;
        }

        internal static void UpdateNumberOfWorkerThreads(string queue, int number)
        {
            List<ManagedEndpoint> myList;

            lock(typeof(Manager))
                myList = new List<ManagedEndpoint>(endpoints);

            foreach (ManagedEndpoint endpoint in myList)
                foreach (Worker w in endpoint.Workers)
                {
                    string[] aList = w.Queue.Split('\\');
                    string a = aList[aList.Length - 1].ToLower();

                    string[] bList = queue.Split('\\');
                    string b = bList[bList.Length - 1].ToLower();

                    if (a == b)
                        w.SetNumberOfWorkerThreads(number);
                }
        }

        public static void RefreshNumberOfWorkerThreads(string queue)
        {
            var message = new GetNumberOfWorkerThreadsMessage();
            bus.Send(queue, message).Register(
                aResult =>
                    {
                        var result =
                            aResult.AsyncState as CompletionResult;
                        if (result == null)
                            return;
                        if (result.Messages == null)
                            return;
                        if (result.Messages.Length != 1)
                            return;
                        var response = result.Messages[0] as GotNumberOfWorkerThreadsMessage;
                        if (response == null)
                            return;
                        var q = result.State as string;

                        UpdateNumberOfWorkerThreads(q, response.NumberOfWorkerThreads);
                    }, queue);
        }

        public static void SetNumberOfWorkerThreads(string queue, int number)
        {
            var message = new ChangeNumberOfWorkerThreadsMessage { NumberOfWorkerThreads = number };
            bus.Send(queue, message);

            RefreshNumberOfWorkerThreads(queue);
        }

    }
}
