namespace Netcode.Transports.AzureWebPubSub
{
    internal class ConnectionContext
    {
        public ulong UClientId { get; set; }
        public string SubChannel { get; set; }
        public string PubChannel { get; set; }
    }
}