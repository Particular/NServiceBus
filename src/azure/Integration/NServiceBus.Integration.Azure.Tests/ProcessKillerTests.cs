
namespace NServiceBus.Integration.Azure.Tests
{
    using System.Diagnostics;
    using System.Linq;
    using Hosting;
    using NUnit.Framework;

    [TestFixture]
    public class ProcessKillerTests
    {
        [Test]
        public void Should_not_throw_when_process_doesnt_exist()
        {
            int processId;
            var startInfo = new ProcessStartInfo
                            {
                                FileName = "ping",
                                Arguments = "/?",
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
            using (var process = Process.Start(startInfo))
            {
                processId = process.Id;
                process.WaitForExit();
            }
            DynamicEndpointRunner.KillProcess(processId, 100);
        }

        [Test]
        public void Should_kill_process_when_process_does_exist()
        {
            var startInfo = new ProcessStartInfo
                            {
                                FileName = "ping",
                                Arguments = "localhost",
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
            var process = Process.Start(startInfo);
            {
                DynamicEndpointRunner.KillProcess(process.Id, 100);
                var processById = Process
                    .GetProcesses()
                    .FirstOrDefault(x => x.Id == process.Id);
                Assert.IsNull(processById);
            }
        }
    }
}