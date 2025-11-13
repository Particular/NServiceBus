namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Logging;
using NUnit.Framework;
using Routing;
using Transport;

public abstract class NServiceBusTransportTest
{
    static NServiceBusTransportTest()
    {
        LogFactory = new TransportTestLoggerFactory();
        LogManager.UseFactory(LogFactory);
    }

    [SetUp]
    public void SetUp()
    {
        testId = Guid.NewGuid().ToString();

        LogFactory.LogItems.Clear();

        //when using [TestCase] NUnit will reuse the same test instance so we need to make sure that the message pump is a fresh one
        transportInfrastructure = null;
        configurer = null;
        testCancellationTokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TestTimeout);
        receiver = null;
        registrations = [];
        CustomizeTransportDefinition = _ => { };
    }

    [TearDown]
    public async Task TearDown()
    {
        await StopPump();
        await (transportInfrastructure != null ? transportInfrastructure.Shutdown() : Task.CompletedTask);
        await (configurer != null ? configurer.Cleanup() : Task.CompletedTask);
        foreach (var registration in registrations)
        {
            registration.Dispose();
        }

        testCancellationTokenSource.Dispose();
    }

    protected async Task StartPump(OnMessage onMessage, OnError onError, TransportTransactionMode transactionMode,
        Action<string, Exception, CancellationToken> onCriticalError = null,
        PushRuntimeSettings pushRuntimeSettings = null,
        CancellationToken cancellationToken = default)
    {
        await Initialize(onMessage, onError, transactionMode, onCriticalError, pushRuntimeSettings,
            cancellationToken);

        await receiver.StartReceive(cancellationToken);
    }

    protected async Task Initialize(OnMessage onMessage, OnError onError, TransportTransactionMode transactionMode,
        Action<string, Exception, CancellationToken> onCriticalError = null,
        PushRuntimeSettings pushRuntimeSettings = null,
        CancellationToken cancellationToken = default)
    {
        onMessage = onMessage ?? throw new ArgumentNullException(nameof(onMessage));
        onError = onError ?? throw new ArgumentNullException(nameof(onError));

        configurer = TransportTestSuiteConstraints.Current.CreateTransportConfiguration();

        var testName = GetTestName();

        InputQueueName = configurer.GetInputQueueName(testName, transactionMode);
        ErrorQueueName = configurer.GetErrorQueueName(testName, transactionMode);

        var hostSettings = new HostSettings(
            InputQueueName,
            string.Empty,
            new StartupDiagnosticEntries(),
            (message, ex, token) =>
            {
                if (onCriticalError == null)
                {
                    testCancellationTokenSource.Cancel();
                    Assert.Fail($"{message}{Environment.NewLine}{ex}");
                }

                onCriticalError(message, ex, token);
            },
            true);

        var transportDefinition = configurer.CreateTransportDefinition();

        CustomizeTransportDefinition(transportDefinition);
        IgnoreUnsupportedTransactionModes(transportDefinition, transactionMode);

        if (OperatingSystem.IsWindows() && transactionMode == TransportTransactionMode.TransactionScope)
        {
            TransactionManager.ImplicitDistributedTransactions = true;
        }

        transportDefinition.TransportTransactionMode = transactionMode;

        transportInfrastructure = await configurer.Configure(transportDefinition, hostSettings, new QueueAddress(InputQueueName), ErrorQueueName, cancellationToken);

        receiver = transportInfrastructure.Receivers.Single().Value;

        await receiver.Initialize(
            pushRuntimeSettings ?? new PushRuntimeSettings(8),
            (context, token) =>
                context.Headers.Contains(TestIdHeaderName, testId) ? onMessage(context, token) : Task.CompletedTask,
            (context, token) =>
                context.Message.Headers.Contains(TestIdHeaderName, testId) ? onError(context, token) : Task.FromResult(ErrorHandleResult.Handled),
            cancellationToken);
    }

    protected async Task StopPump(CancellationToken cancellationToken = default)
    {
        if (receiver == null)
        {
            return;
        }

        await receiver.StopReceive(cancellationToken);

        receiver = null;
    }

    static void IgnoreUnsupportedTransactionModes(TransportDefinition transportDefinition, TransportTransactionMode requestedTransactionMode)
    {
        if (!transportDefinition.GetSupportedTransactionModes().Contains(requestedTransactionMode))
        {
            Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
        }
    }

    protected Task SendMessage(
        string address,
        Dictionary<string, string> headers = null,
        TransportTransaction transportTransaction = null,
        DispatchProperties dispatchProperties = null,
        DispatchConsistency dispatchConsistency = DispatchConsistency.Default,
        byte[] body = null,
        CancellationToken cancellationToken = default) =>
        SendMessage(new UnicastAddressTag(address), headers, transportTransaction, dispatchProperties, dispatchConsistency, body, cancellationToken);

    protected Task SendMessage(
        AddressTag addressTag,
        Dictionary<string, string> headers = null,
        TransportTransaction transportTransaction = null,
        DispatchProperties dispatchProperties = null,
        DispatchConsistency dispatchConsistency = DispatchConsistency.Default,
        byte[] body = null,
        CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();
        var message = new OutgoingMessage(messageId, headers ?? [], body ?? []);

        message.Headers.TryAdd(TestIdHeaderName, testId);

        transportTransaction ??= new TransportTransaction();

        var transportOperation = new TransportOperation(message, addressTag, dispatchProperties, dispatchConsistency);

        return transportInfrastructure.Dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, cancellationToken);
    }
    protected void OnTestTimeout(Action onTimeoutAction)
        => registrations.Add(testCancellationTokenSource.Token.Register(onTimeoutAction));

    protected TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>()
    {
        var source = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!Debugger.IsAttached)
        {
            var tokenRegistration = testCancellationTokenSource.Token
                .Register(state => ((TaskCompletionSource<TResult>)state).TrySetException(new Exception("The test timed out.")), source);
            registrations.Add(tokenRegistration);
        }

        return source;
    }

    protected TaskCompletionSource CreateTaskCompletionSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!Debugger.IsAttached)
        {
            var tokenRegistration = testCancellationTokenSource.Token
                .Register(state => ((TaskCompletionSource)state).TrySetException(new Exception("The test timed out.")), source);
            registrations.Add(tokenRegistration);
        }

        return source;
    }

    protected static string GetTestName()
    {
        var index = 1;
        var frame = new StackFrame(index);
        Type type;

        while (true)
        {
            type = frame.GetMethod().DeclaringType;

            if (type != null && !type.IsAbstract && typeof(NServiceBusTransportTest).IsAssignableFrom(type))
            {
                break;
            }

            frame = new StackFrame(++index);
        }

        var classCallingUs = type.FullName.Split('.').Last();

        var testName = classCallingUs.Split('+').First();

        testName = testName.Replace("When_", "");

        testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

        testName = testName.Replace("_", "");

        return testName;
    }

    public CancellationToken TestTimeoutCancellationToken => testCancellationTokenSource.Token;

    protected string InputQueueName;
    protected string ErrorQueueName;
    protected static readonly TransportTestLoggerFactory LogFactory;
    protected static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);
    protected Action<TransportDefinition> CustomizeTransportDefinition;
    protected IMessageReceiver receiver;

    string testId;

    CancellationTokenSource testCancellationTokenSource;
    IConfigureTransportInfrastructure configurer;
    List<CancellationTokenRegistration> registrations;
    TransportInfrastructure transportInfrastructure;

    const string TestIdHeaderName = "TransportTest.TestId";
}