namespace NServiceBus.Serializers.XML.Test
{
    public class IFirst : IMessage
    {
        public string FirstName { get; set; }
    }

    public class ISecond : IFirst
    {
    }

    public class IThird : ISecond
    {
    }
}
