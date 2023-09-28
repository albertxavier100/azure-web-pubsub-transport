namespace Netcode.Transports.AzureWebPubSub
{
    public class NegotiateException : AzureRealtimeTransportException
    {
        public NegotiateException(string message) : base(message)
        { }
    }
}