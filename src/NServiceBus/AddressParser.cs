namespace NServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface AddressParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        Address Parse(string destination);
    }
}