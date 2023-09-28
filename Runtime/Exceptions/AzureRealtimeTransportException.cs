using System;

namespace Netcode.Transports.AzureWebPubSub
{
    public class AzureRealtimeTransportException : Exception
    {
        public AzureRealtimeTransportException(string message) : base(message)
        { }
    }
}