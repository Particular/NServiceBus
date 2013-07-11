namespace NServiceBus.Serializers.XML.Test.B
{
    using System;

    [Serializable]
    public class Command2 : ICommand
    {
        public Guid Id { get; set; }

        public Command2(Guid id)
        {
            Id = id;
        }
    }
}