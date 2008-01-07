using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NServiceBus.Grid.Messages;
using NServiceBus;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


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
                object machine = Environment.MachineName;
                object missing = Type.Missing;
                object formatName = FormatFull(endpoint.Queue);

                qMgmt.Init(ref machine, ref missing, ref formatName);
                endpoint.SetNumberOfMessages(qMgmt.MessageCount);
            }

            watch.Stop();

            long due = refreshInterval*1000 - watch.ElapsedMilliseconds;
            due = (due < 0 ? 0 : due);

            timer.Change(due, refreshInterval*1000);
        }

        private static string Format(string queue)
        {
            if (queue.Contains("\\"))
                return queue;

            return Environment.MachineName + "\\private$\\" + queue;
        }

        private static string FormatFull(string queue)
        {
            string s = Format(queue);

            return "DIRECT=OS:" + s;
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
            GetNumberOfWorkerThreadsMessage message = new GetNumberOfWorkerThreadsMessage();
            bus.Send(Format(queue), message);
        }

        public static void SetNumberOfWorkerThreads(string queue, int number)
        {
            ChangeNumberOfWorkerThreadsMessage message = new ChangeNumberOfWorkerThreadsMessage(number);
            bus.Send(Format(queue), message);

            RefreshNumberOfWorkerThreads(queue);
        }

    }
}
