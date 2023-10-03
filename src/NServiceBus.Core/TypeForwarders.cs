using System.Runtime.CompilerServices;
using NServiceBus;

[assembly: TypeForwardedTo(typeof(ICommand))]
[assembly: TypeForwardedTo(typeof(IEvent))]
[assembly: TypeForwardedTo(typeof(IMessage))]
