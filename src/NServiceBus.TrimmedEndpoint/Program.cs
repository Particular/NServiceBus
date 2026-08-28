using System.Text.Json;
using System.Text.Json.Serialization;
using NServiceBus;

var configuration = new EndpointConfiguration("TrimmedEndpoint");
configuration.AssemblyScanner().Disable = true;
configuration.UseSerialization<SystemJsonSerializer>().Options(new JsonSerializerOptions
{
    TypeInfoResolver = TrimmedEndpointJsonContext.Default
});
configuration.UseTransport<LearningTransport>().StorageDirectory(Path.Combine(Path.GetTempPath(), "nservicebus-learning-trimmed"));
configuration.AddMessageType<MyCommand>();
configuration.AddHandler<MyHandler>();

var endpoint = await Endpoint.Start(configuration);
IMessageSession session = endpoint;
await session.SendLocal<MyCommand>(new MyCommand { SomeValue = "hello" });

for (var i = 0; i < 50 && !MyHandler.Invoked; i++)
{
    await Task.Delay(100);
}

await endpoint.Stop();

if (!MyHandler.Invoked)
{
    Console.Error.WriteLine("Handler was not invoked.");
    return 1;
}

Console.WriteLine("TRIM-VALIDATION-SUCCESS");
return 0;

[Handler]
public class MyHandler : IHandleMessages<MyCommand>
{
    public static bool Invoked;

    public Task Handle(MyCommand message, IMessageHandlerContext context)
    {
        Invoked = true;
        return Task.CompletedTask;
    }
}

public class MyCommand : ICommand
{
    public string SomeValue { get; set; } = string.Empty;
}

[JsonSerializable(typeof(MyCommand))]
public partial class TrimmedEndpointJsonContext : JsonSerializerContext
{
}
