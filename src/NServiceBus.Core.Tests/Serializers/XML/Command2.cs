namespace NServiceBus.Serializers.XML.Test.B
{
    using System;


    public class Command2 : ICommand
    {
        public Guid Id { get; set; }
    }
}