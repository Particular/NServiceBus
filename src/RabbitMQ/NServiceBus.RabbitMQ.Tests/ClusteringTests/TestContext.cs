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
    using EasyNetQ.ConnectionString;
    using Logging.Loggers.NLogAdapter;
    using NLog;
    using NLog.Config;
    using NLog.Targets;
    using NUnit.Framework;
    using Unicast.Transport;

    public abstract class TestContext
    {
        const string RabbitMqDir = @"C:\Program Files (x86)\RabbitMQ Server\rabbitmq_server-3.0.2\sbin";
        protected const string QueueName = "testreceiver";
        const string ErlangProcessName = "erl";
        protected static Logger Logger = LogManager.GetCurrentClassLogger();

        protected Dictionary<int, RabbitNode> RabbitNodes = new Dictionary<int, RabbitNode>
            {
                {1, new RabbitNode {Number = 1, Port = 5673, MgmtPort = 15673, ShouldBeRunning=true}},
                {2, new RabbitNode {Number = 2, Port = 5674, MgmtPort = 15674, ShouldBeRunning=true}},
                {3, new RabbitNode {Number = 3, Port = 5675, MgmtPort = 15675, ShouldBeRunning=true}}
            };

        readonly string rabbitMqCtl = Path.Combine(RabbitMqDir, "rabbitmqctl.bat");
        readonly string rabbitMqServer = Path.Combine(RabbitMqDir, "rabbitmq-server.bat");
        protected RabbitMqConnectionManager connectionManager;
        protected RabbitMqDequeueStrategy dequeueStrategy;
        protected int[] erlangProcessesRunningBeforeTheTest;
        protected TransportMessage message;
        BlockingCollection<TransportMessage> receivedMessages;
        protected TransportMessage roundTrippedMessage;
        protected RabbitMqMessageSender sender;
        protected RabbitMqUnitOfWork unitOfWork;

        protected class RabbitNode
        {
            public static readonly string LocalHostName = Environment.MachineName;
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
            ProcessStartInfo startInfo = new ProcessStartInfo {UseShellExecute = false, RedirectStandardOutput = true, FileName = program, Arguments = args};
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
            SetupTestDebugLogger();
            Logger.Trace("Running TestContextFixtureSetup");
            SetupNServiceBusLogger(LogLevel.Debug);
            CaptureExistingErlangProcesses();
            StartUpRabbitNodes();
            ClusterRabbitNodes();
            SetHAPolicy();
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
            ClusterRabbitNode(RabbitNodes[2], RabbitNodes[1]);
            ClusterRabbitNode(RabbitNodes[3], RabbitNodes[1]);
        }

        void ResetCluster() {
            InvokeRabbitMqCtl(RabbitNodes[1], "start_app");
            ClusterRabbitNode(RabbitNodes[2], RabbitNodes[1], withReset: true);
            ClusterRabbitNode(RabbitNodes[3], RabbitNodes[1], withReset: true);
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

        void ClusterRabbitNode(RabbitNode node, RabbitNode clusterToNode, bool withReset = false) {
            InvokeRabbitMqCtl(node, "stop_app");
            if (withReset) {
                InvokeRabbitMqCtl(node, "reset");
            }
            InvokeRabbitMqCtl(node, string.Format("join_cluster {0}", clusterToNode.Name));
            InvokeRabbitMqCtl(node, "start_app");
        }

        protected TransportMessage SendAndReceiveAMessage() {
            Logger.Info("Sending a message");
            roundTrippedMessage = null;
            message = new TransportMessage();
            sender.Send(message, Address.Parse(QueueName));
            var receivedMessage = WaitForMessage();
            return receivedMessage;
        }

        protected void SetupTestDebugLogger() {
            SimpleConfigurator.ConfigureForTargetLogging(GetNLogViewerLoggingTarget(),LogLevel.Trace);
            SimpleConfigurator.ConfigureForTargetLogging(GetConsoleLoggingTarget(),LogLevel.Trace);
        }

        static ColoredConsoleTarget GetConsoleLoggingTarget() {
            return new ColoredConsoleTarget {Layout = "${longdate:universalTime=true} | ${logger:shortname=true:padding=-50} | ${message} | ${exception:format=tostring}"};
        }

        static NLogViewerTarget GetNLogViewerLoggingTarget() {
            var log4View = new NLogViewerTarget {Address = "udp://127.0.0.1:12345", IncludeCallSite = true, AppInfo = "IntegrationTest"};
            log4View.Parameters.Add(new NLogViewerParameterInfo {Layout = "${exception:format=tostring}", Name = "Exception"});
            log4View.Parameters.Add(new NLogViewerParameterInfo {Layout = "${stacktrace}", Name = "StackTrace"});
            return log4View;
        }

        protected void SetupNServiceBusLogger(LogLevel logLevel) {
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
            using (var channel = connectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel()) {
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
            var selectionStrategy = new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(config, selectionStrategy);
            var newConnectionManager = new RabbitMqConnectionManager(connectionFactory, new ConnectionRetrySettings());
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
    }
}