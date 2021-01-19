namespace NServiceBus.Serializers.XML.Test.A
{
    using System;


    public class Command1 : ICommand
    {
        public Guid Id { get; set; }

        public Command1(Guid id)
        {
            Id = id;
        }
    }
}