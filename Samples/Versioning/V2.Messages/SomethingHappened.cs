namespace V2.Messages
{
    public interface SomethingHappened : V1.Messages.SomethingHappened
    {
        string MoreInfo { get; set; }
    }
}
