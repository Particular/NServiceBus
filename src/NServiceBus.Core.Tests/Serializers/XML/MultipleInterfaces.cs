namespace NServiceBus.Serializers.XML.Test
{
    using System;

    public interface IFirst : IMessage
    {
        String FirstName { get; set; }
    }
    
    public interface ISecond : IFirst
    {
    }
    
    public interface IThird : ISecond
    {
    }
}
