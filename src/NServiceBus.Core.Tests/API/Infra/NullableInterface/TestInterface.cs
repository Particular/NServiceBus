namespace NServiceBus.Core.Tests.API.Infra.NullableInterface
{
    using System;

    class TestInterface : ITestInterface
    {
        public string ReturnNullableMessage(string message)
        {
            Console.WriteLine(message);
            return message;
        }

        public string WriteAMessage(string message)
        {
            Console.WriteLine(message);
            return message;
        }
        public string WriteNullableMessage(string message)
        {
            Console.WriteLine(message);
            return message;
        }
    }
}
