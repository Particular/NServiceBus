namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Config;
    using EasyNetQ;
    using Logging.Loggers.NLogAdapter;
    using NLog;
    using NLog.Targets;
    using NUnit.Framework;
    using Support;
    using Unicast.Transport;

    public abstract class ClusteredTestContext
    {
        protected const string QueueName = "testreceiver";
        const string ErlangProcessName = "erl";
        protected static Logger Logger = LogManager.GetCurrentClassLogger();

        protected Dictionary<int, RabbitNode> RabbitNodes = new Dictionary<int, RabbitNode>
            {
                {1, new RabbitNode {Number = 1, Port = 5673, MgmtPort = 15673, ShouldBeRunning=true}},
                {2, new RabbitNode {Number = 2, Port = 5674, MgmtPort = 15674, ShouldBeRunning=true}},
                {3, new RabbitNode {Number = 3, Port = 5675, MgmtPort = 15675, ShouldBeRunning=true}}
            };

        readonly string rabbitMqCtl = "rabbitmqctl.bat";//make sure that you have the PATH environment variable setup
        readonly string rabbitMqServer = "rabbitmq-server.bat";//make sure that you have the PATH environment variable setup

        RabbitMqConnectionManager connectionManager;
        RabbitMqDequeueStrategy dequeueStrategy;
        protected int[] erlangProcessesRunningBeforeTheTest;
        BlockingCollection<TransportMessage> receivedMessages;
        RabbitMqMessageSender sender;
        RabbitMqUnitOfWork unitOfWork;

        protected class RabbitNode
        {
            public static readonly string LocalHostName = RuntimeEnvironment.MachineName;
            public int MgmtPort;
            public int Number;
            public int Port;
            public bool ShouldBeRunning = true;

            /// <summary>
            ///     The FQ node name (eg rabbit1@JUSTINT).
            /// </summary>
            public string Name {
                get { return string.Format("rabbit{0}@{1}", Number, LocalHostName); }
            }
        }

        protected Process[] GetExistingErlangProcesses() {
            return Process.GetProcessesByName(ErlangProcessName);
        }

        void StartRabbitMqServer(RabbitNode node) {
            Dictionary<string,string> envVars = new Dictionary<string,string>
                {
                    {"RABBITMQ_NODENAME", node.Name},
                    {"RABBITMQ_NODE_PORT", node.Port.ToString(CultureInfo.InvariantCulture)},
                    {"RABBITMQ_SERVER_START_ARGS", string.Format("-rabbitmq_management listener [{{port,{0}}}]", node.MgmtPort)},
                };

            InvokeExternalProgram(rabbitMqServer, "-detached", envVars);
        }

        protected void InvokeRabbitMqCtl(RabbitNode node, string cmd) {
            var args = (string.Format("-n {0} {1}", node.Name, cmd));
            InvokeExternalProgram(rabbitMqCtl, args);
        }

        static void InvokeExternalProgram(string program, string args, Dictionary<string,string> customEnvVars = null) {
            ProcessStartInfo startInfo = new ProcessStartInfo {UseShellExecute = false, RedirectStandardOutput = true, FileName = program, Arguments = args,CreateNoWindow = true,WindowStyle = ProcessWindowStyle.Hidden};
            var environmentVariables = startInfo.EnvironmentVariables;

            if (customEnvVars != null) {
                foreach (KeyValuePair<string, string> customEnvVar in customEnvVars) {
                    Logger.Debug("Setting env var {0} to '{1}'", customEnvVar.Key, customEnvVar.Value);
                    if (environmentVariables.ContainsKey(customEnvVar.Key)) {
                        environmentVariables[customEnvVar.Key] = customEnvVar.Value;
                    }
                    else {
                        environmentVariables.Add(customEnvVar.Key, customEnvVar.Value);
                    }
                }
            }

            string programName = Path.GetFileName(program);
            Logger.Debug("Running {0} with args: '{1}'", programName, args);
            Process p = Process.Start(startInfo);
            string output = p.StandardOutput.ReadToEnd();
            output = output.Replace("\n", "  "); // replace line breaks for more terse logging output
            p.WaitForExit();
            Logger.Debug("Result: {0}", output);
        }

        [TestFixtureSetUp]
        public void TestContextFixtureSetup() {
            SetupNLog(LogLevel.Trace);
            Logger.Trace("Running TestContextFixtureSetup");
            CaptureExistingErlangProcesses();
            StartUpRabbitNodes();
            ClusterRabbitNodes();
            SetHAPolicy();
            Logger.Fatal("RabbitMQ cluster setup complete");
        }

        [TestFixtureTearDown]
        public void TestContextFixtureTearDown() {
            Logger.Trace("Running TestContextFixtureTearDown");
            if (dequeueStrategy != null) {
                dequeueStrategy.Stop();
            }

            connectionManager.Dispose();

            var erlangProcessesToKill = GetExistingErlangProcesses().Select(p => p.Id).Except(erlangProcessesRunningBeforeTheTest).ToList();
            erlangProcessesToKill.ForEach(id => Process.GetProcessById(id).Kill());
        }

        void ClusterRabbitNodes() {
            ClusterRabbitNode(2, 1);
            ClusterRabbitNode(3, 1);
        }

        void ResetCluster() {
            StartNode(1);
            ClusterRabbitNode(2, 1, withReset: true);
            ClusterRabbitNode(3, 1, withReset: true);
        }

        void SetHAPolicy() {
            const string cmd = @"set_policy ha-all ""^(?!amq\.).*"" ""{""""ha-mode"""": """"all""""}""";
            InvokeRabbitMqCtl(RabbitNodes[1], cmd);
        }

        void CaptureExistingErlangProcesses() {
            erlangProcessesRunningBeforeTheTest = GetExistingErlangProcesses().Select(p => p.Id).ToArray();
        }

        void StartUpRabbitNodes() {
            foreach (var node in RabbitNodes.Values.Where(node => node.ShouldBeRunning)) {
                StartRabbitMqServer(node);
            }
        }

        void ClusterRabbitNode(int fromNodeNumber, int toNodeNumber, bool withReset = false) {
            var node = RabbitNodes[fromNodeNumber];
            var clusterToNode = RabbitNodes[toNodeNumber];
            InvokeRabbitMqCtl(node, "stop_app");
            if (withReset) {
                InvokeRabbitMqCtl(node, "reset");
            }
            InvokeRabbitMqCtl(node, string.Format("join_cluster {0}", clusterToNode.Name));
            InvokeRabbitMqCtl(node, "start_app");
        }

        protected TransportMessage SendAndReceiveAMessage() {
            TransportMessage message; 
            return SendAndReceiveAMessage(out message);
        }

        protected TransportMessage SendAndReceiveAMessage(out TransportMessage sentMessage) {
            Logger.Info("Sending a message");
            var message = new TransportMessage();
            sender.Send(message, Address.Parse(QueueName));
            sentMessage = message;
            var receivedMessage = WaitForMessage();
            return receivedMessage;
        }

        static ColoredConsoleTarget GetConsoleLoggingTarget() {
            return new ColoredConsoleTarget {Layout = "${longdate:universalTime=true} | ${logger:shortname=true} | ${message} | ${exception:format=tostring}"};
        }

        static NLogViewerTarget GetNLogViewerLoggingTarget() {
            var log4View = new NLogViewerTarget {Address = "udp://127.0.0.1:12345", IncludeCallSite = true, AppInfo = "IntegrationTest"};
            log4View.Parameters.Add(new NLogViewerParameterInfo {Layout = "${exception:format=tostring}", Name = "Exception"});
            log4View.Parameters.Add(new NLogViewerParameterInfo {Layout = "${stacktrace}", Name = "StackTrace"});
            return log4View;
        }

        protected void SetupNLog(LogLevel logLevel) {
            var nLogViewerTarget = GetNLogViewerLoggingTarget();
            var consoleTarget = GetConsoleLoggingTarget();
            NLogConfigurator.Configure(new object[] {nLogViewerTarget, consoleTarget}, logLevel.ToString());
        }

        protected void SetupQueueAndSenderAndListener(string connectionString) {
            connectionManager = SetupRabbitMqConnectionManager(connectionString);
            EnsureRabbitQueueExists(QueueName);
            SetupMessageSender();
            SetupQueueListener(QueueName);
        }

        void SetupQueueListener(string queueName) {
            receivedMessages = new BlockingCollection<TransportMessage>();
            dequeueStrategy = new RabbitMqDequeueStrategy {ConnectionManager = connectionManager, PurgeOnStartup = true};
            dequeueStrategy.Init(Address.Parse(queueName), TransactionSettings.Default, m =>
                {
                    receivedMessages.Add(m);
                    return true;
                }, (s, exception) =>
                    {
                    });

            dequeueStrategy.Start(1);
        }

        void EnsureRabbitQueueExists(string queueName) {
            using (var channel = connectionManager.GetAdministrationConnection().CreateModel()) {
                channel.QueueDeclare(queueName, true, false, false, null);
                channel.QueuePurge(queueName);
            }
        }

        void SetupMessageSender() {
            unitOfWork = new RabbitMqUnitOfWork {ConnectionManager = connectionManager};
            sender = new RabbitMqMessageSender {UnitOfWork = unitOfWork};
        }

        static RabbitMqConnectionManager SetupRabbitMqConnectionManager(string connectionString) {
            var config = new ConnectionStringParser().Parse(connectionString);
//            config.OverrideClientProperties();
            var selectionStrategy = new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(config, selectionStrategy);
            var newConnectionManager = new RabbitMqConnectionManager(connectionFactory, config);
            return newConnectionManager;
        }

        TransportMessage WaitForMessage() {
            var waitTime = TimeSpan.FromSeconds(1);

            if (Debugger.IsAttached) {
                waitTime = TimeSpan.FromMinutes(10);
            }

            TransportMessage transportMessage;
            receivedMessages.TryTake(out transportMessage, waitTime);

            return transportMessage;
        }

        protected string GetConnectionString() {
            var hosts = RabbitNodes.Values.OrderBy(n => n.Port).Select(n => string.Format("{0}:{1}", RabbitNode.LocalHostName, n.Port));
            string connectionString = string.Concat("host=" ,string.Join(",", hosts));
            Logger.Info("Connection string is: '{0}'", connectionString);
            return connectionString;
        }

        protected void StopNode(int nodeNumber) {
            Logger.Warn("Stopping node {0}",nodeNumber);
            InvokeRabbitMqCtl(RabbitNodes[nodeNumber], "stop_app");
        }

        protected void StartNode(int nodeNumber) {
            Logger.Info("Starting node {0}",nodeNumber);
            InvokeRabbitMqCtl(RabbitNodes[nodeNumber], "start_app");
        }
    }
}