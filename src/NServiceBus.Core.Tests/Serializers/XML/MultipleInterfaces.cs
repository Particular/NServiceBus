namespace NServiceBus.Serializers.XML.Test
{
    public interface IFirst : IMessage
    {
        string FirstName { get; set; }
    }

    public interface ISecond : IFirst
    {
    }

    public interface IThird : ISecond
    {
    }
}
