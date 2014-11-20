namespace NServiceBus.Core.Tests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class FileBasedRoundRobinRoutingTests
    {
        [Test]
        public void Test1()
        {
            var addresses = new[]
            {
                "address1",
                "address2",
                "address3",
                "address4"
            };
            var queueName = Path.GetRandomFileName();
            var path = Path.Combine(Path.GetTempPath(), queueName + ".txt");

            try
            {
                Console.Out.WriteLine("File written to '{0}'", path);
                File.WriteAllLines(path, addresses);

                var fileBasedRouting = new FileBasedRoundRobinRoutingProvider(Path.GetTempPath(), TimeSpan.FromMilliseconds(100));

                Parallel.For(0, 100, i =>
                {
                    string address;
                    if (fileBasedRouting.TryGetRouteAddress(queueName, out address))
                    {
                        Console.Out.WriteLine(address);
                    }
                    else
                    {
                        Console.Out.WriteLine("Not Available Yet");
                    }

                });
            }
            finally
            {
                Thread.Sleep(1000);
                File.Delete(path);
            }
        }


        [Test]
        public void Test2()
        {
            var addresses1 = new[]
            {
                "address1",
                "address2",
                "address3",
                "address4"
            };
            var queueName1 = Path.GetRandomFileName();
            var path1 = Path.Combine(Path.GetTempPath(), queueName1 + ".txt");

            var addresses2 = new[]
            {
                "address5",
                "address6",
                "address7",
                "address8"
            };
            var queueName2 = Path.GetRandomFileName();
            var path2 = Path.Combine(Path.GetTempPath(), queueName2 + ".txt");

            try
            {
                Console.Out.WriteLine("File written to '{0}'", path1);
                File.WriteAllLines(path1, addresses1);

                Console.Out.WriteLine("File written to '{0}'", path2);
                File.WriteAllLines(path2, addresses2);

                var fileBasedRouting = new FileBasedRoundRobinRoutingProvider(Path.GetTempPath(), TimeSpan.FromMilliseconds(100));

                var task1 = Task.Factory.StartNew(
                () =>
                {
                    Parallel.For(0, 100, i =>
                    {
                        string address;
                        if (fileBasedRouting.TryGetRouteAddress(queueName1, out address))
                        {
                            Console.Out.WriteLine("{0} = {1}", address, queueName1);
                        }
                        else
                        {
                            Console.Out.WriteLine("Using default = {0}", queueName1);
                        }
                    });
                });

                var task2 = Task.Factory.StartNew(
                () =>
                {
                    Parallel.For(0, 100, i =>
                    {
                        string address;
                        if (fileBasedRouting.TryGetRouteAddress(queueName2, out address))
                        {
                            Console.Out.WriteLine("{0} = {1}", address, queueName2);
                        }
                        else
                        {
                            Console.Out.WriteLine("Using default = {0}", queueName2);
                        }
                    });
                });

                Task.WaitAll(task1, task2);
            }
            finally
            {
                Thread.Sleep(1000);
                File.Delete(path1);
                File.Delete(path2);
            }
        }
    }
}