namespace NServiceBus.Serializers.XML
{
    public interface IM1 : IMessage
    {
        float Age { get; set; }
        int Int { get; set; }
        string Name { get; set; }
        string Address { get; set; }
        Risk Risk { get; set; }
    }
}
